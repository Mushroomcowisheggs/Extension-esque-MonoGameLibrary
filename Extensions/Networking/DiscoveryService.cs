using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The default <see cref="IDiscoveryService"/>. It keeps one datagram socket on the discovery port,
    /// answers probes for the room it publishes, and collects the adverts of every other room.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The socket is bound to the wildcard address so that a broadcast reaches it on whichever interface it
    /// arrives through, and it is opened only while browsing or announcing. Adverts are sent to every usable
    /// broadcast address rather than to a single one, because a machine with several adapters does not
    /// reliably emit a limited broadcast on all of them.
    /// </para>
    /// <para>
    /// Entries are matched by the room identifier the host publishes, never by address, so a room that is
    /// reachable through two interfaces stays one entry instead of two.
    /// </para>
    /// </remarks>
    public sealed class DiscoveryService : IDiscoveryService {
        /// <summary>The cancellation operation key this service registers with <see cref="ICancellationService"/>.</summary>
        private const string OperationDiscovery = "Network.Discovery";
        
        /// <summary>The time a shutdown waits for a background loop to observe its cancellation.</summary>
        private const int ShutdownWaitMilliseconds = 500;
        
        private readonly NetworkOptions _optionsNetwork;
        private readonly DiscoveryOptions _optionsDiscovery;
        private readonly INetworkSerializer _serializerNetwork;
        private readonly IThreadPool _poolThread;
        private readonly ICancellationService _serviceCancellation;
        private readonly ILogger _logger;
        private readonly Optional<IProfiler> _profiler;
        
        private readonly object _lock = new object();
        private readonly ConcurrentQueue<DiscoveryNotification> _queueNotifications = new ConcurrentQueue<DiscoveryNotification>();
        private readonly Dictionary<Guid, DiscoveryRoomRecord> _rooms = new Dictionary<Guid, DiscoveryRoomRecord>();
        
        private Socket _socket;
        private CancellationTokenSource _sourceSocket;
        private CancellationTokenSource _sourceBrowsing;
        private CancellationTokenSource _sourceAnnouncing;
        private Task _taskReceive;
        private Task _taskProbe;
        private Task _taskAnnounce;
        private NetworkRoomInfo _infoPublished;
        private Guid _identifierRoom = Guid.Empty;
        private Guid _identifierLocal = Guid.Empty;
        private int _portPublished;
        private long _sequenceRoom;
        private bool _flagBrowsing;
        private bool _flagAnnouncing;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a new service.
        /// </summary>
        /// <param name="optionsNetwork">The session settings, used for the application identity and the protocol version.</param>
        /// <param name="optionsDiscovery">The discovery settings.</param>
        /// <param name="serializerNetwork">The serializer that encodes and decodes adverts. It must know the discovery messages.</param>
        /// <param name="poolThread">The thread pool that hosts the background loops.</param>
        /// <param name="serviceCancellation">The cancellation service that lets the application stop discovery as a whole.</param>
        /// <param name="logger">The logger for discovery diagnostics.</param>
        /// <param name="profiler">An optional profiler used to measure dispatch work.</param>
        /// <exception cref="ArgumentNullException">Thrown if a required dependency is null.</exception>
        public DiscoveryService(
            NetworkOptions optionsNetwork,
            DiscoveryOptions optionsDiscovery,
            INetworkSerializer serializerNetwork,
            IThreadPool poolThread,
            ICancellationService serviceCancellation,
            ILogger logger,
            Optional<IProfiler> profiler = default
        ) {
            if (optionsNetwork == null) {
                throw new ArgumentNullException(nameof(optionsNetwork));
            }
            if (optionsDiscovery == null) {
                throw new ArgumentNullException(nameof(optionsDiscovery));
            }
            if (serializerNetwork == null) {
                throw new ArgumentNullException(nameof(serializerNetwork));
            }
            if (poolThread == null) {
                throw new ArgumentNullException(nameof(poolThread));
            }
            if (serviceCancellation == null) {
                throw new ArgumentNullException(nameof(serviceCancellation));
            }
            if (logger == null) {
                throw new ArgumentNullException(nameof(logger));
            }
            optionsDiscovery.Validate();
            _optionsNetwork = optionsNetwork;
            _optionsDiscovery = optionsDiscovery;
            _serializerNetwork = serializerNetwork;
            _poolThread = poolThread;
            _serviceCancellation = serviceCancellation;
            _logger = logger;
            _profiler = profiler;
            _identifierLocal = Guid.NewGuid();
        }
        
        /// <inheritdoc />
        public event EventHandler<DiscoveredRoomEventArgs> RoomDiscovered;
        
        /// <inheritdoc />
        public event EventHandler<DiscoveredRoomEventArgs> RoomUpdated;
        
        /// <inheritdoc />
        public event EventHandler<DiscoveredRoomEventArgs> RoomLost;
        
        /// <inheritdoc />
        public bool IsBrowsing {
            get {
                lock (_lock) {
                    return _flagBrowsing;
                }
            }
        }
        
        /// <inheritdoc />
        public bool IsAnnouncing {
            get {
                lock (_lock) {
                    return _flagAnnouncing;
                }
            }
        }
        
        /// <inheritdoc />
        public IReadOnlyList<DiscoveredRoom> Rooms {
            get {
                lock (_lock) {
                    List<DiscoveryRoomRecord> records = new List<DiscoveryRoomRecord>(_rooms.Values);
                    records.Sort(delegate(DiscoveryRoomRecord left, DiscoveryRoomRecord right) {
                        return left.Sequence.CompareTo(right.Sequence);
                    });
                    List<DiscoveredRoom> rooms = new List<DiscoveredRoom>(records.Count);
                    for (int index = 0; index < records.Count; index += 1) {
                        rooms.Add(CreateSnapshot(records[index]));
                    }
                    return rooms;
                }
            }
        }
        
        /// <inheritdoc />
        public void StartBrowsing() {
            EnsureNotDisposed();
            CancellationTokenSource sourceBrowsing;
            lock (_lock) {
                if (_flagBrowsing) {
                    return;
                }
                _flagBrowsing = true;
                sourceBrowsing = CancellationTokenSource.CreateLinkedTokenSource(_serviceCancellation.GetTokenForOperation(OperationDiscovery));
                _sourceBrowsing = sourceBrowsing;
            }
            EnsureSocket();
            _taskProbe = _poolThread.RunAsync(delegate() { return RunProbeLoopAsync(sourceBrowsing.Token); }, "Discovery.Probe");
            _logger.Info($"Browsing for rooms on UDP port {_optionsDiscovery.DiscoveryPort}.");
        }
        
        /// <inheritdoc />
        public void StopBrowsing() {
            CancellationTokenSource sourceBrowsing;
            lock (_lock) {
                if (!_flagBrowsing) {
                    return;
                }
                _flagBrowsing = false;
                sourceBrowsing = _sourceBrowsing;
                _sourceBrowsing = null;
            }
            CancelScope(sourceBrowsing);
            CloseSocketIfIdle();
        }
        
        /// <inheritdoc />
        public void ClearRooms() {
            lock (_lock) {
                _rooms.Clear();
            }
            DiscoveryNotification notificationDiscarded;
            while (_queueNotifications.TryDequeue(out notificationDiscarded)) {
                // Pending notifications describe rooms that no longer exist.
            }
        }
        
        /// <inheritdoc />
        public void StartAnnouncing(NetworkRoomInfo room) {
            if (room == null) {
                throw new ArgumentNullException(nameof(room));
            }
            EnsureNotDisposed();
            room.Validate();
            
            NetworkRoomInfo roomPublished = room.Clone();
            roomPublished.ProtocolVersion = _optionsNetwork.ProtocolVersion;
            roomPublished.ApplicationName = _optionsNetwork.ApplicationName;
            roomPublished.ApplicationVersion = _optionsNetwork.ApplicationVersion;
            
            CancellationTokenSource sourceAnnouncing;
            lock (_lock) {
                _infoPublished = roomPublished;
                _identifierRoom = Guid.NewGuid();
                _portPublished = room.Port;
                if (_flagAnnouncing) {
                    return;
                }
                _flagAnnouncing = true;
                sourceAnnouncing = CancellationTokenSource.CreateLinkedTokenSource(_serviceCancellation.GetTokenForOperation(OperationDiscovery));
                _sourceAnnouncing = sourceAnnouncing;
            }
            EnsureSocket();
            _taskAnnounce = _poolThread.RunAsync(delegate() { return RunAnnounceLoopAsync(sourceAnnouncing.Token); }, "Discovery.Announce");
            _logger.Info($"Announcing the room '{roomPublished.RoomName}' on UDP port {_optionsDiscovery.DiscoveryPort}.");
        }
        
        /// <inheritdoc />
        public void UpdateRoomInfo(NetworkRoomInfo room) {
            if (room == null) {
                throw new ArgumentNullException(nameof(room));
            }
            room.Validate();
            lock (_lock) {
                if (!_flagAnnouncing || _infoPublished == null) {
                    throw new InvalidOperationException("No room is being announced. Call StartAnnouncing before updating it.");
                }
                _infoPublished = room.Clone();
                _infoPublished.ProtocolVersion = _optionsNetwork.ProtocolVersion;
                _infoPublished.ApplicationName = _optionsNetwork.ApplicationName;
                _infoPublished.ApplicationVersion = _optionsNetwork.ApplicationVersion;
                _portPublished = room.Port;
            }
        }
        
        /// <inheritdoc />
        public void StopAnnouncing() {
            CancellationTokenSource sourceAnnouncing;
            lock (_lock) {
                if (!_flagAnnouncing) {
                    return;
                }
                _flagAnnouncing = false;
                _infoPublished = null;
                _identifierRoom = Guid.Empty;
                sourceAnnouncing = _sourceAnnouncing;
                _sourceAnnouncing = null;
            }
            CancelScope(sourceAnnouncing);
            CloseSocketIfIdle();
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            if (_profiler.HasValue) {
                using (IDisposable measure = _profiler.Value.BeginMeasure("Discovery.Update")) {
                    ExpireRooms();
                    DispatchPending();
                }
            } else {
                ExpireRooms();
                DispatchPending();
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            try {
                StopBrowsing();
                StopAnnouncing();
                CloseSocketIfIdle();
                WaitForTask(_taskReceive, ShutdownWaitMilliseconds);
                _taskReceive = null;
            } finally {
                GC.SuppressFinalize(this);
            }
        }
        /// <summary>
        /// Creates the datagram socket if it is not open yet.
        /// </summary>
        /// <exception cref="NetworkProtocolException">Thrown if the discovery port cannot be bound.</exception>
        private void EnsureSocket() {
            CancellationTokenSource sourceSocket;
            CancellationToken tokenSocket;
            lock (_lock) {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(nameof(DiscoveryService));
                }
                if (_socket != null) {
                    return;
                }
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.EnableBroadcast = true;
                IPAddress addressBind = _optionsDiscovery.BindAddress;
                if (addressBind == null) {
                    addressBind = IPAddress.Any;
                }
                try {
                    socket.Bind(new IPEndPoint(addressBind, _optionsDiscovery.DiscoveryPort));
                } catch (SocketException exception) {
                    try {
                        socket.Dispose();
                    } catch (ObjectDisposedException exceptionReleased) {
                        _logger.Debug("The resource was already released: " + exceptionReleased.Message);
                    }
                    throw new NetworkProtocolException($"The discovery port {_optionsDiscovery.DiscoveryPort} cannot be used: {exception.Message}", exception);
                }
                sourceSocket = CancellationTokenSource.CreateLinkedTokenSource(_serviceCancellation.GetTokenForOperation(OperationDiscovery));
                tokenSocket = sourceSocket.Token;
                _socket = socket;
                _sourceSocket = sourceSocket;
                _taskReceive = _poolThread.RunAsync(delegate() { return RunReceiveLoopAsync(socket, tokenSocket); }, "Discovery.Receive");
            }
        }
        
        /// <summary>
        /// Closes the socket once neither browsing nor announcing needs it.
        /// </summary>
        private void CloseSocketIfIdle() {
            Socket socket;
            CancellationTokenSource sourceSocket;
            Task taskReceive;
            lock (_lock) {
                if (_flagBrowsing || _flagAnnouncing || _socket == null) {
                    return;
                }
                socket = _socket;
                sourceSocket = _sourceSocket;
                taskReceive = _taskReceive;
                _socket = null;
                _sourceSocket = null;
                _taskReceive = null;
            }
            CancelScope(sourceSocket);
            try {
                socket.Close();
            } catch (ObjectDisposedException exceptionReleased) {
                // The socket was already released.
                _logger.Debug("The resource was already released: " + exceptionReleased.Message);
            }
            // The receive loop stops on its own once the socket is closed; the caller is never made to wait,
            // because it is usually a frame of the game.
            GC.KeepAlive(taskReceive);
        }
        
        /// <summary>
        /// Cancels and releases a cancellation scope.
        /// </summary>
        /// <param name="source">The scope to release, or null.</param>
        private void CancelScope(CancellationTokenSource source) {
            if (source == null) {
                return;
            }
            try {
                source.Cancel();
            } catch (ObjectDisposedException exception) {
                // The scope was already released by a concurrent shutdown.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
            
                // The scope was already released by a concurrent shutdown.
            } catch (AggregateException exception) {
                // A callback of the scope threw; cancellation itself still completed.
                _logger.Warning("A cancellation callback failed: " + exception.Message);
            }
            source.Dispose();
        }
        
        /// <summary>
        /// Receives datagrams until the socket closes and hands each one to the dispatcher.
        /// </summary>
        /// <param name="socket">The datagram socket.</param>
        /// <param name="token">The socket token.</param>
        private async Task RunReceiveLoopAsync(Socket socket, CancellationToken token) {
            byte[] buffer = new byte[_optionsDiscovery.MaxPacketBytes];
            try {
                while (!token.IsCancellationRequested) {
                    EndPoint endpointRemote = new IPEndPoint(IPAddress.Any, 0);
                    SocketReceiveFromResult result = await socket.ReceiveFromAsync(
                        new ArraySegment<byte>(buffer),
                        SocketFlags.None,
                        endpointRemote,
                        token
                    ).ConfigureAwait(false);
                    if (result.ReceivedBytes <= 0) {
                        continue;
                    }
                    byte[] payload = new byte[result.ReceivedBytes];
                    Buffer.BlockCopy(buffer, 0, payload, 0, result.ReceivedBytes);
                    await HandleDatagramAsync(payload, result.RemoteEndPoint as IPEndPoint, socket, token).ConfigureAwait(false);
                }
            } catch (OperationCanceledException exception) {
                // The socket was closed.
                _logger.Debug("The operation was cancelled, so it stops here.");
                return;
                
                // The socket was closed.
            } catch (ObjectDisposedException exception) {
                // The socket was closed while a receive was pending.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
                
                // The socket was closed while a receive was pending.
            } catch (SocketException exception) {
                if (!token.IsCancellationRequested) {
                    _logger.Warning($"Room discovery stopped listening: {exception.Message}");
                }
            }
        }
        
        /// <summary>
        /// Decodes one datagram and reacts to it.
        /// </summary>
        /// <param name="payload">The datagram payload.</param>
        /// <param name="endpointRemote">The endpoint the datagram came from.</param>
        /// <param name="socket">The datagram socket.</param>
        /// <param name="token">The socket token.</param>
        private async Task HandleDatagramAsync(byte[] payload, IPEndPoint endpointRemote, Socket socket, CancellationToken token) {
            string kindRead;
            INetworkMessage message;
            if (!_serializerNetwork.TryDeserialize(payload, out kindRead, out message)) {
                return;
            }
            
            RoomAdvertisement advertisement = message as RoomAdvertisement;
            if (advertisement != null) {
                HandleAdvertisement(advertisement, endpointRemote);
                return;
            }
            RoomProbeRequest probe = message as RoomProbeRequest;
            if (probe != null) {
                await HandleProbeAsync(probe, endpointRemote, socket, token).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Records an advert, ignoring the echo of the local room and the adverts of other applications.
        /// </summary>
        /// <param name="advertisement">The advert that arrived.</param>
        /// <param name="endpointRemote">The endpoint the advert came from.</param>
        private void HandleAdvertisement(RoomAdvertisement advertisement, IPEndPoint endpointRemote) {
            if (advertisement == null || advertisement.Info == null) {
                return;
            }
            Guid identifierRoom = advertisement.RoomIdentifier;
            if (identifierRoom == Guid.Empty) {
                return;
            }
            lock (_lock) {
                if (_flagAnnouncing && identifierRoom == _identifierRoom) {
                    // The room's own advert was delivered back to the socket it left from.
                    return;
                }
            }
            if (_optionsDiscovery.EnableForeignApplicationFilter && advertisement.Info.ApplicationName != _optionsNetwork.ApplicationName) {
                return;
            }
            
            string addressHost;
            if (endpointRemote == null) {
                addressHost = advertisement.HostAddress;
            } else {
                addressHost = endpointRemote.Address.ToString();
            }
            if (string.IsNullOrEmpty(addressHost)) {
                return;
            }
            
            int latency = -1;
            if (advertisement.IsResponse && advertisement.TimestampTicks != 0) {
                long elapsed = Environment.TickCount64 - advertisement.TimestampTicks;
                if (elapsed >= 0 && elapsed <= int.MaxValue) {
                    latency = (int)elapsed;
                }
            }
            bool flagIsCompatible = advertisement.Info.ProtocolVersion == _optionsNetwork.ProtocolVersion;
            
            bool flagIsNew = false;
            bool flagChanged = false;
            lock (_lock) {
                DiscoveryRoomRecord record;
                if (!_rooms.TryGetValue(identifierRoom, out record)) {
                    if (_rooms.Count >= _optionsDiscovery.MaxRooms) {
                        return;
                    }
                    record = new DiscoveryRoomRecord();
                    record.Sequence = ++_sequenceRoom;
                    _rooms.Add(identifierRoom, record);
                    flagIsNew = true;
                } else {
                    flagChanged = HasDescriptionChanged(record.Advertisement, advertisement);
                }
                
                record.Advertisement = advertisement;
                record.HostAddress = addressHost;
                record.Port = advertisement.Port;
                if (latency >= 0) {
                    record.LatencyMilliseconds = latency;
                }
                record.SeenTimestamp = Environment.TickCount64;
                record.TimestampSeenUtcTicks = DateTime.UtcNow.Ticks;
                record.FlagIsCompatible = flagIsCompatible;
                
                if (flagIsNew || flagChanged) {
                    DiscoveryNotification notification = new DiscoveryNotification();
                    if (flagIsNew) {
                        notification.Kind = DiscoveryNotificationKind.Discovered;
                    } else {
                        notification.Kind = DiscoveryNotificationKind.Updated;
                    }
                    notification.Room = CreateSnapshot(record);
                    _queueNotifications.Enqueue(notification);
                }
            }
            if (flagIsNew) {
                _logger.Info($"Found the room '{advertisement.Info.RoomName}' at {addressHost}:{advertisement.Port}.");
            }
        }
        
        /// <summary>
        /// Answers a probe when the local peer publishes a room. The answer is sent straight back to the
        /// probing endpoint, which is what makes discovery work on a network that drops broadcasts one way.
        /// </summary>
        /// <param name="probe">The probe that arrived.</param>
        /// <param name="endpointRemote">The endpoint the probe came from.</param>
        /// <param name="socket">The datagram socket.</param>
        /// <param name="token">The socket token.</param>
        private async Task HandleProbeAsync(RoomProbeRequest probe, IPEndPoint endpointRemote, Socket socket, CancellationToken token) {
            if (endpointRemote == null) {
                return;
            }
            NetworkRoomInfo roomPublished;
            int portPublished;
            Guid identifierRoom;
            Guid identifierLocal;
            lock (_lock) {
                if (!_flagAnnouncing || _infoPublished == null) {
                    return;
                }
                roomPublished = _infoPublished;
                portPublished = _portPublished;
                identifierRoom = _identifierRoom;
                identifierLocal = _identifierLocal;
            }
            
            RoomAdvertisement advertisement = new RoomAdvertisement();
            advertisement.RoomIdentifier = identifierRoom;
            advertisement.HostIdentifier = identifierLocal;
            advertisement.HostAddress = endpointRemote.Address.ToString();
            advertisement.Port = portPublished;
            advertisement.Info = roomPublished;
            advertisement.TimestampTicks = probe.TimestampTicks;
            advertisement.IsResponse = true;
            await SendAdvertisementAsync(advertisement, endpointRemote, socket, token).ConfigureAwait(false);
            _logger.Debug($"Answered a room probe from '{endpointRemote}'.");
        }
        
        /// <summary>
        /// Repeats the advert of the published room until announcing stops.
        /// </summary>
        /// <param name="token">The announcing token.</param>
        private async Task RunAnnounceLoopAsync(CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    NetworkRoomInfo roomPublished;
                    int portPublished;
                    Guid identifierRoom;
                    Guid identifierLocal;
                    Socket socket = _socket;
                    lock (_lock) {
                        roomPublished = _infoPublished;
                        portPublished = _portPublished;
                        identifierRoom = _identifierRoom;
                        identifierLocal = _identifierLocal;
                    }
                    if (roomPublished != null && socket != null) {
                        RoomAdvertisement advertisement = new RoomAdvertisement();
                        advertisement.RoomIdentifier = identifierRoom;
                        advertisement.HostIdentifier = identifierLocal;
                        advertisement.Port = portPublished;
                        advertisement.Info = roomPublished;
                        advertisement.IsResponse = false;
                        advertisement.TimestampTicks = 0;
                        if (!await TrySendAdvertisementAsync(advertisement, null, socket, token).ConfigureAwait(false)) {
                            return;
                        }
                    }
                    if (!await TryDelayAsync(_optionsDiscovery.AnnouncementIntervalMilliseconds, token).ConfigureAwait(false)) {
                        return;
                    }
                }
            } catch (Exception exception) {
                _logger.Error("The room announcement loop stopped unexpectedly.", exception);
            }
        }
        
        /// <summary>
        /// Sends one advertisement and reports a transport failure without ending the loop that drives it. An
        /// interface that cannot carry the datagram is a normal condition, not a reason to stop announcing.
        /// </summary>
        /// <param name="advertisement">The advert to send.</param>
        /// <param name="endpointSingle">The single destination, or null to broadcast.</param>
        /// <param name="socket">The datagram socket.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns><c>false</c> when the loop must stop; otherwise <c>true</c>.</returns>
        private async Task<bool> TrySendAdvertisementAsync(RoomAdvertisement advertisement, IPEndPoint endpointSingle, Socket socket, CancellationToken token) {
            try {
                await SendAdvertisementAsync(advertisement, endpointSingle, socket, token).ConfigureAwait(false);
                return true;
            } catch (OperationCanceledException) {
                return false;
            } catch (SocketException exception) {
                _logger.Warning($"A room advert could not be sent: {exception.Message}");
                return true;
            } catch (ObjectDisposedException) {
                // The socket was closed by StopAnnouncing or StopBrowsing.
                return false;
            }
        }
        
        /// <summary>
        /// Asks every room on the local network to identify itself until browsing stops.
        /// </summary>
        /// <param name="token">The browsing token.</param>
        private async Task RunProbeLoopAsync(CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    Socket socket = _socket;
                    if (socket != null && !await TryProbeAsync(socket, token).ConfigureAwait(false)) {
                        return;
                    }
                    if (!await TryDelayAsync(_optionsDiscovery.ProbeIntervalMilliseconds, token).ConfigureAwait(false)) {
                        return;
                    }
                }
            } catch (Exception exception) {
                _logger.Error("The room probe loop stopped unexpectedly.", exception);
            }
        }
        
        /// <summary>
        /// Broadcasts one probe. A single interface that refuses the datagram does not prevent the others from
        /// carrying it, and a transport failure never ends the browsing session.
        /// </summary>
        /// <param name="socket">The datagram socket.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns><c>false</c> when the loop must stop; otherwise <c>true</c>.</returns>
        private async Task<bool> TryProbeAsync(Socket socket, CancellationToken token) {
            try {
                RoomProbeRequest probe = new RoomProbeRequest();
                probe.RequesterIdentifier = _identifierLocal;
                probe.ProtocolVersion = _optionsNetwork.ProtocolVersion;
                probe.TimestampTicks = Environment.TickCount64;
                byte[] payload = _serializerNetwork.Serialize(probe);
                List<IPEndPoint> endpoints = NetworkInterfaceBroadcast.GetEndpoints(
                    _optionsDiscovery.DiscoveryPort,
                    _optionsDiscovery.EnableLimitedBroadcast,
                    _optionsDiscovery.EnableSubnetBroadcast,
                    _optionsDiscovery.EnableLoopbackInterface
                );
                for (int index = 0; index < endpoints.Count; index += 1) {
                    try {
                        await socket.SendToAsync(new ArraySegment<byte>(payload), SocketFlags.None, endpoints[index], token).ConfigureAwait(false);
                    } catch (SocketException exception) {
                        _logger.Debug($"A room probe could not use '{endpoints[index]}': {exception.Message}");
                    }
                }
                return true;
            } catch (OperationCanceledException) {
                return false;
            } catch (SocketException exception) {
                _logger.Warning($"A room probe could not be sent: {exception.Message}");
                return true;
            } catch (ObjectDisposedException) {
                // The socket was closed by StopAnnouncing or StopBrowsing.
                return false;
            }
        }
        
        /// <summary>
        /// Waits for the next round of a discovery loop.
        /// </summary>
        /// <param name="milliseconds">The interval to wait.</param>
        /// <param name="token">The loop token.</param>
        /// <returns><c>false</c> when the loop must stop; otherwise <c>true</c>.</returns>
        private static async Task<bool> TryDelayAsync(int milliseconds, CancellationToken token) {
            try {
                await Task.Delay(milliseconds, token).ConfigureAwait(false);
                return true;
            } catch (OperationCanceledException) {
                return false;
            }
        }
        /// <summary>
        /// Sends one advert, either to a single endpoint or to every broadcast address.
        /// </summary>
        /// <param name="advertisement">The advert to send.</param>
        /// <param name="endpointSingle">The single destination, or null to broadcast.</param>
        /// <param name="socket">The datagram socket.</param>
        /// <param name="token">The cancellation token.</param>
        private async Task SendAdvertisementAsync(RoomAdvertisement advertisement, IPEndPoint endpointSingle, Socket socket, CancellationToken token) {
            byte[] payload = _serializerNetwork.Serialize(advertisement);
            if (endpointSingle != null) {
                await socket.SendToAsync(new ArraySegment<byte>(payload), SocketFlags.None, endpointSingle, token).ConfigureAwait(false);
                return;
            }
            List<IPEndPoint> endpoints = NetworkInterfaceBroadcast.GetEndpoints(
                _optionsDiscovery.DiscoveryPort,
                _optionsDiscovery.EnableLimitedBroadcast,
                _optionsDiscovery.EnableSubnetBroadcast,
                _optionsDiscovery.EnableLoopbackInterface
            );
            for (int index = 0; index < endpoints.Count; index += 1) {
                await socket.SendToAsync(new ArraySegment<byte>(payload), SocketFlags.None, endpoints[index], token).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Drops the rooms that stopped announcing themselves and reports each departure.
        /// </summary>
        private void ExpireRooms() {
            List<DiscoveredRoom> listLost = new List<DiscoveredRoom>();
            lock (_lock) {
                List<Guid> listExpired = new List<Guid>();
                foreach (KeyValuePair<Guid, DiscoveryRoomRecord> pair in _rooms) {
                    if (Environment.TickCount64 - pair.Value.SeenTimestamp > _optionsDiscovery.RoomExpiryMilliseconds) {
                        listExpired.Add(pair.Key);
                    }
                }
                for (int index = 0; index < listExpired.Count; index += 1) {
                    DiscoveryRoomRecord record = _rooms[listExpired[index]];
                    listLost.Add(CreateSnapshot(record));
                    _rooms.Remove(listExpired[index]);
                }
            }
            for (int index = 0; index < listLost.Count; index += 1) {
                _logger.Debug($"The room '{listLost[index].Info.RoomName}' at '{listLost[index].Address}' stopped announcing itself.");
                DiscoveryNotification notification = new DiscoveryNotification();
                notification.Kind = DiscoveryNotificationKind.Lost;
                notification.Room = listLost[index];
                _queueNotifications.Enqueue(notification);
            }
        }
        
        /// <summary>
        /// Raises the events that match the queued changes.
        /// </summary>
        private void DispatchPending() {
            DiscoveryNotification notification;
            int countDispatched = 0;
            while (countDispatched < _optionsDiscovery.MaxDispatchPerUpdate && _queueNotifications.TryDequeue(out notification)) {
                countDispatched += 1;
                if (notification.Kind == DiscoveryNotificationKind.Discovered) {
                    EventHandler<DiscoveredRoomEventArgs> handler = RoomDiscovered;
                    if (handler != null) {
                        handler(this, new DiscoveredRoomEventArgs(notification.Room));
                    }
                    continue;
                }
                if (notification.Kind == DiscoveryNotificationKind.Updated) {
                    EventHandler<DiscoveredRoomEventArgs> handler = RoomUpdated;
                    if (handler != null) {
                        handler(this, new DiscoveredRoomEventArgs(notification.Room));
                    }
                    continue;
                }
                EventHandler<DiscoveredRoomEventArgs> handlerLost = RoomLost;
                if (handlerLost != null) {
                    handlerLost(this, new DiscoveredRoomEventArgs(notification.Room));
                }
            }
        }
        
        /// <summary>
        /// Builds the immutable snapshot a game sees for one room.
        /// </summary>
        /// <param name="record">The browser-side record.</param>
        /// <returns>The snapshot.</returns>
        private static DiscoveredRoom CreateSnapshot(DiscoveryRoomRecord record) {
            NetworkRoomInfo room = record.Advertisement.Info;
            return new DiscoveredRoom(record.Advertisement.RoomIdentifier, record.HostAddress, record.Port, room, record.LatencyMilliseconds, record.TimestampSeenUtcTicks, record.FlagIsCompatible);
        }
        
        /// <summary>
        /// Determines whether the parts of an advert a player can see have changed.
        /// </summary>
        /// <param name="advertisementPrevious">The advert that was stored, or null.</param>
        /// <param name="advertisementCurrent">The advert that just arrived.</param>
        /// <returns><c>true</c> when the description changed.</returns>
        private static bool HasDescriptionChanged(RoomAdvertisement advertisementPrevious, RoomAdvertisement advertisementCurrent) {
            if (advertisementPrevious == null || advertisementPrevious.Info == null) {
                return true;
            }
            if (advertisementPrevious.Port != advertisementCurrent.Port) {
                return true;
            }
            NetworkRoomInfo roomPrevious = advertisementPrevious.Info;
            NetworkRoomInfo roomCurrent = advertisementCurrent.Info;
            if (roomPrevious.RoomName != roomCurrent.RoomName) {
                return true;
            }
            if (roomPrevious.HostName != roomCurrent.HostName) {
                return true;
            }
            if (roomPrevious.Description != roomCurrent.Description) {
                return true;
            }
            if (roomPrevious.PlayerCount != roomCurrent.PlayerCount) {
                return true;
            }
            if (roomPrevious.MaxPlayers != roomCurrent.MaxPlayers) {
                return true;
            }
            if (roomPrevious.ProtocolVersion != roomCurrent.ProtocolVersion) {
                return true;
            }
            if (roomPrevious.ApplicationVersion != roomCurrent.ApplicationVersion) {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Rejects use of a disposed service.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
        private void EnsureNotDisposed() {
            lock (_lock) {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(nameof(DiscoveryService));
                }
            }
        }
        
        /// <summary>
        /// Waits for a background task without letting a fault or a hang escape.
        /// </summary>
        /// <param name="task">The task to wait for, or null.</param>
        /// <param name="milliseconds">The time budget.</param>
        private void WaitForTask(Task task, int milliseconds) {
            if (task == null) {
                return;
            }
            try {
                task.Wait(milliseconds);
            } catch (AggregateException exception) {
                _logger.Warning($"A background discovery loop ended with an error: {exception.Message}");
            } catch (ObjectDisposedException exceptionReleased) {
                // The task was already released.
                _logger.Debug("The resource was already released: " + exceptionReleased.Message);
            }
        }
    }
}
