using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The default <see cref="INetworkService"/>. It owns the sockets, runs every read, write and timeout on
    /// background loops, and hands the results to the game from <see cref="Update"/> on the calling thread.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing in this class performs a blocking socket call. Connecting, accepting, reading and writing all
    /// await the asynchronous socket APIs, and the periodic work is driven by a watchdog task that sleeps
    /// between passes. A game therefore pays only for dispatching the messages it actually receives.
    /// </para>
    /// <para>
    /// The instance takes ownership of the transport passed to the constructor and disposes it together with
    /// the session, because a transport holds the sockets of the session.
    /// </para>
    /// </remarks>
    public sealed class NetworkService : INetworkService, INetworkProbe {
        /// <summary>The period of the watchdog that drives keep-alive, idle detection, and reconnection.</summary>
        private const int WatchdogIntervalMilliseconds = 250;
        
        /// <summary>The cancellation operation key this service registers with <see cref="ICancellationService"/>.</summary>
        private const string OperationSession = "Network.Session";
        
        private readonly NetworkOptions _optionsNetwork;
        private readonly INetworkSerializer _serializerNetwork;
        private readonly INetworkTransport _transportNetwork;
        private readonly IThreadPool _poolThread;
        private readonly ICancellationService _serviceCancellation;
        private readonly ILogger _logger;
        private readonly Optional<IProfiler> _profiler;
        
        private readonly object _lock = new object();
        private readonly ConcurrentQueue<NetworkInboundEvent> _queueInbound = new ConcurrentQueue<NetworkInboundEvent>();
        private readonly ConcurrentDictionary<Guid, NetworkConnection> _connections = new ConcurrentDictionary<Guid, NetworkConnection>();
        private readonly ConcurrentDictionary<Guid, NetworkPeerRecord> _peersKnown = new ConcurrentDictionary<Guid, NetworkPeerRecord>();
        
        private NetworkStatus _status = NetworkStatus.Idle;
        private NetworkRole _role = NetworkRole.None;
        private NetworkRoomInfo _infoCurrent;
        private Guid _identifierLocal = Guid.Empty;
        private Guid _identifierHost = Guid.Empty;
        private string _nameLocal = "";
        private string _addressTarget = "";
        private int _port;
        private int _portTarget;
        private long _sequencePeer;
        private volatile bool _flagDisconnectRequested;
        private bool _flagSessionOpen;
        private bool _flagDisposed;
        
        private INetworkListener _listener;
        private CancellationTokenSource _sourceSession;
        private Task _taskSession;
        private Task _taskWatchdog;
        
        /// <summary>
        /// Initializes a new service.
        /// </summary>
        /// <param name="optionsNetwork">The session settings. The instance is used as supplied and must not be mutated afterwards.</param>
        /// <param name="serializerNetwork">The serializer shared by both directions.</param>
        /// <param name="transportNetwork">The transport to open channels with. The service disposes it.</param>
        /// <param name="poolThread">The thread pool that hosts the background loops.</param>
        /// <param name="serviceCancellation">The cancellation service that lets the application stop networking as a whole.</param>
        /// <param name="logger">The logger for session diagnostics.</param>
        /// <param name="profiler">An optional profiler used to measure dispatch work.</param>
        /// <exception cref="ArgumentNullException">Thrown if a required dependency is null.</exception>
        public NetworkService(
            NetworkOptions optionsNetwork,
            INetworkSerializer serializerNetwork,
            INetworkTransport transportNetwork,
            IThreadPool poolThread,
            ICancellationService serviceCancellation,
            ILogger logger,
            Optional<IProfiler> profiler = default
        ) {
            if (optionsNetwork == null) {
                throw new ArgumentNullException(nameof(optionsNetwork));
            }
            if (serializerNetwork == null) {
                throw new ArgumentNullException(nameof(serializerNetwork));
            }
            if (transportNetwork == null) {
                throw new ArgumentNullException(nameof(transportNetwork));
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
            optionsNetwork.Validate();
            _optionsNetwork = optionsNetwork;
            _serializerNetwork = serializerNetwork;
            _transportNetwork = transportNetwork;
            _poolThread = poolThread;
            _serviceCancellation = serviceCancellation;
            _logger = logger;
            _profiler = profiler;
        }
        
        /// <inheritdoc />
        public event EventHandler<NetworkMessageEventArgs> MessageReceived;
        
        /// <inheritdoc />
        public event EventHandler<NetworkPeerEventArgs> PeerJoined;
        
        /// <inheritdoc />
        public event EventHandler<NetworkPeerEventArgs> PeerLeft;
        
        /// <inheritdoc />
        public event EventHandler<NetworkStatusChangedEventArgs> StatusChanged;
        
        /// <inheritdoc />
        public NetworkStatus Status {
            get {
                lock (_lock) {
                    return _status;
                }
            }
        }
        
        /// <inheritdoc />
        public NetworkRole Role {
            get {
                lock (_lock) {
                    return _role;
                }
            }
        }
        
        /// <inheritdoc />
        public int Port {
            get {
                lock (_lock) {
                    return _port;
                }
            }
        }
        
        /// <inheritdoc />
        public Guid LocalIdentifier {
            get {
                lock (_lock) {
                    return _identifierLocal;
                }
            }
        }
        
        /// <inheritdoc />
        public string LocalDisplayName {
            get {
                lock (_lock) {
                    return _nameLocal;
                }
            }
        }
        
        /// <inheritdoc />
        public NetworkRoomInfo Info {
            get {
                lock (_lock) {
                    return _infoCurrent;
                }
            }
        }
        
        /// <inheritdoc />
        public IReadOnlyList<NetworkPeer> Peers {
            get {
                List<NetworkPeerRecord> records = new List<NetworkPeerRecord>(_peersKnown.Values);
                records.Sort(delegate(NetworkPeerRecord left, NetworkPeerRecord right) {
                    return left.Sequence.CompareTo(right.Sequence);
                });
                List<NetworkPeer> peers = new List<NetworkPeer>(records.Count);
                for (int index = 0; index < records.Count; index += 1) {
                    peers.Add(records[index].Peer);
                }
                return peers;
            }
        }
        
        /// <inheritdoc />
        public void Host(int port, NetworkRoomInfo room, string displayName) {
            if (room == null) {
                throw new ArgumentNullException(nameof(room));
            }
            if (string.IsNullOrWhiteSpace(displayName)) {
                throw new ArgumentException("A display name is required.", nameof(displayName));
            }
            if (port < 0 || port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(port), "A port must be between 0 and 65535.");
            }
            EnsureNoSession();
            
            NetworkRoomInfo roomHosted = room.Clone();
            roomHosted.HostName = displayName;
            roomHosted.ProtocolVersion = _optionsNetwork.ProtocolVersion;
            roomHosted.ApplicationName = _optionsNetwork.ApplicationName;
            roomHosted.ApplicationVersion = _optionsNetwork.ApplicationVersion;
            roomHosted.Port = port;
            roomHosted.PlayerCount = 1;
            roomHosted.Validate();
            
            lock (_lock) {
                _role = NetworkRole.Host;
                _infoCurrent = roomHosted;
                _nameLocal = displayName;
                _identifierLocal = Guid.NewGuid();
                _identifierHost = _identifierLocal;
                _port = 0;
                _flagDisconnectRequested = false;
            }
            ResetSession();
            AddLocalPeer();
            TransitionStatus(NetworkStatus.Connecting, "Opening the room.");
            CancellationToken tokenSession = _sourceSession.Token;
            _taskWatchdog = _poolThread.RunAsync(delegate() { return RunWatchdogAsync(tokenSession); }, "Network.Watchdog");
            _taskSession = _poolThread.RunAsync(delegate() { return RunHostAsync(port, tokenSession); }, "Network.Host");
        }
        
        /// <inheritdoc />
        public void Connect(string addressHost, int port, string displayName) {
            if (addressHost == null) {
                throw new ArgumentNullException(nameof(addressHost));
            }
            if (string.IsNullOrWhiteSpace(addressHost)) {
                throw new ArgumentException("A host name or address is required.", nameof(addressHost));
            }
            if (string.IsNullOrWhiteSpace(displayName)) {
                throw new ArgumentException("A display name is required.", nameof(displayName));
            }
            if (port < 1 || port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(port), "A port must be between 1 and 65535.");
            }
            EnsureNoSession();
            
            lock (_lock) {
                _role = NetworkRole.Client;
                _infoCurrent = null;
                _nameLocal = displayName;
                _identifierLocal = Guid.NewGuid();
                _identifierHost = Guid.Empty;
                _port = port;
                _addressTarget = addressHost.Trim();
                _portTarget = port;
                _flagDisconnectRequested = false;
            }
            ResetSession();
            AddLocalPeer();
            TransitionStatus(NetworkStatus.Connecting, "Connecting to '" + addressHost + ":" + port + "'.");
            CancellationToken tokenSession = _sourceSession.Token;
            _taskWatchdog = _poolThread.RunAsync(delegate() { return RunWatchdogAsync(tokenSession); }, "Network.Watchdog");
            _taskSession = _poolThread.RunAsync(delegate() { return RunClientSessionAsync(tokenSession); }, "Network.Connect");
        }
        
        /// <inheritdoc />
        public void Disconnect(string reason) {
            string reasonReported = reason;
            if (reasonReported == null) {
                reasonReported = "";
            }
            
            bool flagActive;
            lock (_lock) {
                flagActive = _status == NetworkStatus.Connecting || _status == NetworkStatus.Hosting || _status == NetworkStatus.Connected;
                _flagDisconnectRequested = true;
            }
            if (!flagActive) {
                return;
            }
            
            TransitionStatus(NetworkStatus.Disconnecting, reasonReported);
            NetworkDisconnectMessage message = new NetworkDisconnectMessage();
            message.Reason = reasonReported;
            BroadcastFrame(message, true);
            // The farewell is flushed by a short-lived task; the caller is never blocked on the network.
            Task taskClosing = _poolThread.RunAsync(delegate() { return CloseSessionAfterFlushAsync(reasonReported); }, "Network.Disconnect");
        }
        
        /// <inheritdoc />
        public void Send(INetworkMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            EnsureSessionActive();
            BroadcastFrame(message, false);
        }
        
        /// <inheritdoc />
        public void SendTo(Guid identifierPeer, INetworkMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            EnsureSessionActive();
            
            byte[] frame = SerializeToFrame(message);
            NetworkConnection connection;
            if (!_connections.TryGetValue(identifierPeer, out connection)) {
                _logger.Warning($"The message for peer '{identifierPeer}' was dropped because that peer is no longer connected.");
                return;
            }
            connection.EnqueueFrame(frame);
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            if (_profiler.HasValue) {
                using (IDisposable measure = _profiler.Value.BeginMeasure("Network.Update")) {
                    DispatchPending();
                }
            } else {
                DispatchPending();
            }
        }
        
        /// <inheritdoc />
        public async Task<NetworkProbeResult> ProbeAsync(string addressHost, int port, CancellationToken token) {
            if (addressHost == null) {
                throw new ArgumentNullException(nameof(addressHost));
            }
            if (string.IsNullOrWhiteSpace(addressHost)) {
                throw new ArgumentException("A host name or address is required.", nameof(addressHost));
            }
            if (port < 1 || port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(port), "A port must be between 1 and 65535.");
            }
            
            string addressReported = addressHost.Trim() + ":" + port;
            INetworkChannel channel = null;
            Stopwatch watch = Stopwatch.StartNew();
            try {
                using (CancellationTokenSource sourceProbe = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                    sourceProbe.CancelAfter(_optionsNetwork.ConnectTimeoutMilliseconds + _optionsNetwork.HandshakeTimeoutMilliseconds);
                    channel = await _transportNetwork.ConnectAsync(addressHost.Trim(), port, sourceProbe.Token).ConfigureAwait(false);
                    if (channel == null) {
                        return new NetworkProbeResult(addressReported, false, false, -1, null, "The endpoint did not accept a connection.");
                    }
                    
                    NetworkHelloMessage hello = new NetworkHelloMessage();
                    hello.ProtocolVersion = _optionsNetwork.ProtocolVersion;
                    hello.ApplicationName = _optionsNetwork.ApplicationName;
                    hello.ApplicationVersion = _optionsNetwork.ApplicationVersion;
                    hello.DisplayName = "probe";
                    hello.Identifier = Guid.Empty;
                    hello.IsProbe = true;
                    await SendMessageAsync(channel, hello, sourceProbe.Token).ConfigureAwait(false);
                    
                    byte[] payload = await ReadFrameAsync(channel, _optionsNetwork.MaxMessageBytes, sourceProbe.Token).ConfigureAwait(false);
                    int latency = (int)watch.ElapsedMilliseconds;
                    if (payload == null) {
                        return new NetworkProbeResult(addressReported, false, false, -1, null, "The endpoint closed the connection during the handshake.");
                    }
                    
                    string kindRead;
                    INetworkMessage message;
                    if (!_serializerNetwork.TryDeserialize(payload, out kindRead, out message)) {
                        return new NetworkProbeResult(addressReported, false, false, -1, null, "The endpoint answered with an unreadable payload.");
                    }
                    
                    NetworkWelcomeMessage welcome = message as NetworkWelcomeMessage;
                    if (welcome != null) {
                        bool flagIsCompatible = welcome.ProtocolVersion == _optionsNetwork.ProtocolVersion;
                        string errorCompatible = flagIsCompatible ? "" : $"The room speaks protocol version {welcome.ProtocolVersion} while this peer speaks {_optionsNetwork.ProtocolVersion}.";
                        return new NetworkProbeResult(addressReported, true, flagIsCompatible, latency, welcome.Info, errorCompatible);
                    }
                    
                    NetworkRefusedMessage refused = message as NetworkRefusedMessage;
                    if (refused != null) {
                        return new NetworkProbeResult(addressReported, true, false, latency, null, refused.Reason);
                    }
                    
                    return new NetworkProbeResult(addressReported, false, false, -1, null, "The endpoint answered with an unexpected handshake.");
                }
            } catch (OperationCanceledException) {
                return new NetworkProbeResult(addressReported, false, false, -1, null, "The endpoint did not answer in time.");
            } catch (NetworkProtocolException exception) {
                return new NetworkProbeResult(addressReported, false, false, -1, null, exception.Message);
            } catch (IOException exception) {
                return new NetworkProbeResult(addressReported, false, false, -1, null, exception.Message);
            } finally {
                if (channel != null) {
                    channel.Close();
                }
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            try {
                CloseSession("The network service was disposed.", NetworkStatus.Idle);
                _transportNetwork.Dispose();
            } finally {
                GC.SuppressFinalize(this);
            }
        }
        
        /// <summary>
        /// The time a farewell message is given to reach its peers before the sockets close.
        /// </summary>
        private const int DisconnectFlushMilliseconds = 150;
        
        /// <summary>
        /// The time a shutdown waits for a background loop to observe its cancellation.
        /// </summary>
        private const int ShutdownWaitMilliseconds = 500;
        
        /// <summary>
        /// The longest display name the service accepts from a remote peer.
        /// </summary>
        private const int DisplayNameLimit = 32;
        
        /// <summary>
        /// Drains the notification queue, up to the limit configured in the options.
        /// </summary>
        private void DispatchPending() {
            int countDispatched = 0;
            int countLimit = _optionsNetwork.MaxDispatchPerUpdate;
            while (countDispatched < countLimit) {
                NetworkInboundEvent notification;
                if (!_queueInbound.TryDequeue(out notification)) {
                    return;
                }
                countDispatched += 1;
                DispatchNotification(notification);
            }
        }
        
        /// <summary>
        /// Raises the event that matches one notification. Handlers run on the calling thread, which is the
        /// thread the game drives <see cref="Update"/> from.
        /// </summary>
        /// <param name="notification">The notification to report.</param>
        private void DispatchNotification(NetworkInboundEvent notification) {
            if (notification.Kind == NetworkInboundKind.StatusChanged) {
                EventHandler<NetworkStatusChangedEventArgs> handlerStatus = StatusChanged;
                if (handlerStatus != null) {
                    handlerStatus(this, new NetworkStatusChangedEventArgs(notification.StatusPrevious, notification.CurrentStatus, notification.Reason));
                }
                return;
            }
            if (notification.Kind == NetworkInboundKind.PeerJoined) {
                EventHandler<NetworkPeerEventArgs> handlerJoined = PeerJoined;
                if (handlerJoined != null) {
                    handlerJoined(this, new NetworkPeerEventArgs(notification.Peer));
                }
                return;
            }
            if (notification.Kind == NetworkInboundKind.PeerLeft) {
                EventHandler<NetworkPeerEventArgs> handlerLeft = PeerLeft;
                if (handlerLeft != null) {
                    handlerLeft(this, new NetworkPeerEventArgs(notification.Peer));
                }
                return;
            }
            EventHandler<NetworkMessageEventArgs> handlerMessage = MessageReceived;
            if (handlerMessage != null) {
                handlerMessage(this, new NetworkMessageEventArgs(notification.Peer, notification.Message));
            }
        }
        
        /// <summary>
        /// Binds the listener and serves incoming players until the session ends.
        /// </summary>
        /// <param name="port">The requested port, or zero for an ephemeral one.</param>
        /// <param name="token">The session token.</param>
        private async Task RunHostAsync(int port, CancellationToken token) {
            INetworkListener listener = null;
            try {
                listener = await _transportNetwork.ListenAsync(port, token).ConfigureAwait(false);
                lock (_lock) {
                    _listener = listener;
                    _port = listener.Port;
                    if (_infoCurrent != null) {
                        _infoCurrent.Port = listener.Port;
                    }
                }
                TransitionStatus(NetworkStatus.Hosting, "The room is open on port " + listener.Port + ".");
                _logger.Info($"The room '{RoomNameForLog()}' is open on port {listener.Port}.");
                
                while (!token.IsCancellationRequested) {
                    INetworkChannel channel = await listener.AcceptAsync(token).ConfigureAwait(false);
                    if (channel == null) {
                        break;
                    }
                    // AcceptPeerAsync reports its own failures, so the returned task cannot fault unobserved.
                    Task taskAccepted = AcceptPeerAsync(channel, token);
                }
            } catch (OperationCanceledException exception) {
                // The session was closed.
                _logger.Debug("The operation was cancelled, so it stops here.");
                return;
                
                // The session was closed.
            } catch (NetworkProtocolException exception) {
                ReportFault("The room could not be opened.", exception);
            } catch (Exception exception) {
                ReportFault("The listener stopped unexpectedly.", exception);
            } finally {
                if (listener != null) {
                    listener.Close();
                }
            }
        }
        
        /// <summary>
        /// Serves one accepted socket: performs the handshake, registers the player, pumps the link and keeps
        /// serving until the link ends.
        /// </summary>
        /// <param name="channel">The accepted channel.</param>
        /// <param name="token">The session token.</param>
        private async Task AcceptPeerAsync(INetworkChannel channel, CancellationToken token) {
            NetworkConnection connection = null;
            try {
                connection = await RunAcceptedPeerAsync(channel, token).ConfigureAwait(false);
            } catch (OperationCanceledException exception) {
                // The session was closed while the player was joining.
                _logger.Debug("The operation was cancelled, so it stops here.");
                return;
                
                // The session was closed while the player was joining.
            } catch (NetworkProtocolException exception) {
                _logger.Warning($"A joining player was rejected: {exception.Message}");
            } catch (IOException exception) {
                _logger.Warning($"A joining player lost the connection: {exception.Message}");
            } catch (Exception exception) {
                _logger.Error("A joining player could not be served.", exception);
            } finally {
                if (connection != null) {
                    TeardownConnection(connection, "The player left.");
                }
                channel.Close();
            }
        }
        
        /// <summary>
        /// Runs the host side of the handshake and returns the established link, which the caller owns until
        /// the link ends.
        /// </summary>
        /// <param name="channel">The accepted channel.</param>
        /// <param name="token">The session token.</param>
        /// <returns>The established link, or null when the player was refused.</returns>
        private async Task<NetworkConnection> RunAcceptedPeerAsync(INetworkChannel channel, CancellationToken token) {
            byte[] payload;
            using (CancellationTokenSource sourceHandshake = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                sourceHandshake.CancelAfter(_optionsNetwork.HandshakeTimeoutMilliseconds);
                payload = await ReadFrameAsync(channel, _optionsNetwork.MaxMessageBytes, sourceHandshake.Token).ConfigureAwait(false);
            }
            
            NetworkHelloMessage hello = null;
            if (payload != null) {
                string kindRead;
                INetworkMessage message;
                if (_serializerNetwork.TryDeserialize(payload, out kindRead, out message)) {
                    hello = message as NetworkHelloMessage;
                }
            }
            if (hello == null) {
                await RefuseAsync(channel, "The room expected a handshake first.", token).ConfigureAwait(false);
                return null;
            }
            if (hello.ProtocolVersion != _optionsNetwork.ProtocolVersion) {
                string labelVersion = hello.ProtocolVersion < _optionsNetwork.ProtocolVersion ? "Outdated Client" : "Outdated Server";
                string reasonVersion = $"{labelVersion}. The room speaks protocol version {_optionsNetwork.ProtocolVersion} while this player speaks {hello.ProtocolVersion}.";
                await RefuseAsync(channel, reasonVersion, token).ConfigureAwait(false);
                return null;
            }
            
            Guid identifierLocal = LocalIdentifier;
            if (hello.IsProbe) {
                await AnswerProbeAsync(channel, token).ConfigureAwait(false);
                return null;
            }
            Guid identifierPeer = hello.Identifier;
            if (identifierPeer == Guid.Empty || identifierPeer == identifierLocal) {
                identifierPeer = Guid.NewGuid();
            }
            
            NetworkConnection connectionPrevious;
            if (_connections.TryGetValue(identifierPeer, out connectionPrevious)) {
                TeardownConnection(connectionPrevious, "The same player connected again.");
            }
            if (Status != NetworkStatus.Hosting) {
                await RefuseAsync(channel, "The room is closing.", token).ConfigureAwait(false);
                return null;
            }
            if (_connections.Count >= _optionsNetwork.MaxPeers) {
                await RefuseAsync(channel, "The room is full.", token).ConfigureAwait(false);
                return null;
            }
            
            NetworkConnection connection = new NetworkConnection(new Optional<ILogger>(_logger));
            connection.Channel = channel;
            connection.Identifier = identifierPeer;
            connection.DisplayName = SanitizeDisplayName(hello.DisplayName);
            connection.Address = channel.RemoteAddress;
            connection.HostFlag = false;
            if (!_connections.TryAdd(identifierPeer, connection)) {
                await RefuseAsync(channel, "The room already holds that player.", token).ConfigureAwait(false);
                return null;
            }
            
            AddOrUpdatePeer(connection.Identifier, connection.DisplayName, connection.Address, connection.MillisecondsLatency, false, false, true);
            UpdateRoomPlayerCount();
            
            NetworkWelcomeMessage welcome = new NetworkWelcomeMessage();
            welcome.ProtocolVersion = _optionsNetwork.ProtocolVersion;
            welcome.PeerIdentifier = connection.Identifier;
            welcome.HostIdentifier = identifierLocal;
            lock (_lock) {
                if (_infoCurrent != null) {
                    welcome.Info = _infoCurrent.Clone();
                } else {
                    welcome.Info = new NetworkRoomInfo();
                }
            }
            welcome.Peers = BuildRoster();
            connection.EnqueueFrame(SerializeToFrame(welcome));
            BroadcastRoster(connection.Identifier);
            _logger.Info($"Player '{connection.DisplayName}' joined from '{connection.Address}'.");
            
            StartLinkPumps(connection, token);
            await connection.ReceiveLoopTask.ConfigureAwait(false);
            return connection;
        }
        
        /// <summary>
        /// Answers a status probe with the room description and closes the link. The probing peer is never
        /// added to the roster, so it costs neither a player slot nor a join event.
        /// </summary>
        /// <param name="channel">The channel of the probing peer.</param>
        /// <param name="token">The session token.</param>
        private async Task AnswerProbeAsync(INetworkChannel channel, CancellationToken token) {
            NetworkWelcomeMessage welcome = new NetworkWelcomeMessage();
            welcome.ProtocolVersion = _optionsNetwork.ProtocolVersion;
            welcome.PeerIdentifier = Guid.Empty;
            welcome.HostIdentifier = LocalIdentifier;
            lock (_lock) {
                if (_infoCurrent != null) {
                    welcome.Info = _infoCurrent.Clone();
                } else {
                    welcome.Info = new NetworkRoomInfo();
                }
            }
            welcome.Peers = BuildRoster();
            await SendMessageAsync(channel, welcome, token).ConfigureAwait(false);
            _logger.Debug($"Answered a status probe from '{channel.RemoteAddress}'.");
            channel.Close();
        }
        
        /// <summary>
        /// Tells a joining player why the room will not accept it, then closes the socket.
        /// </summary>
        /// <param name="channel">The channel of the joining player.</param>
        /// <param name="reason">The human readable reason.</param>
        /// <param name="token">The session token.</param>
        private async Task RefuseAsync(INetworkChannel channel, string reason, CancellationToken token) {
            NetworkRefusedMessage refused = new NetworkRefusedMessage();
            refused.Reason = reason;
            await SendMessageAsync(channel, refused, token).ConfigureAwait(false);
            _logger.Info($"A joining player was refused: {reason}");
            channel.Close();
        }
        
        /// <summary>
        /// Joins the configured room, and reconnects while the options allow it.
        /// </summary>
        /// <param name="token">The session token.</param>
        private async Task RunClientSessionAsync(CancellationToken token) {
            int countAttempts = 0;
            while (!token.IsCancellationRequested) {
                try {
                    await RunClientLinkAsync(token).ConfigureAwait(false);
                } catch (OperationCanceledException) {
                    return;
                } catch (NetworkProtocolException exception) {
                    ReportFault("The connection failed.", exception);
                } catch (IOException exception) {
                    ReportFault("The connection failed.", exception);
                } catch (Exception exception) {
                    ReportFault("The connection ended unexpectedly.", exception);
                }
                
                if (token.IsCancellationRequested || _flagDisconnectRequested) {
                    return;
                }
                if (!_optionsNetwork.EnableAutoReconnect) {
                    return;
                }
                countAttempts += 1;
                if (countAttempts > _optionsNetwork.MaxReconnectAttempts) {
                    TransitionStatus(NetworkStatus.Failed, "The reconnection attempts were exhausted.");
                    return;
                }
                TransitionStatus(NetworkStatus.Connecting, $"Reconnecting to the room (attempt {countAttempts}).");
                await Task.Delay(_optionsNetwork.ReconnectDelayMilliseconds, token).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Opens one link to the room and keeps it until it ends.
        /// </summary>
        /// <param name="token">The session token.</param>
        /// <returns><c>true</c> when a link was established and then ended; <c>false</c> when it never opened.</returns>
        private async Task<bool> RunClientLinkAsync(CancellationToken token) {
            string addressTarget;
            int portTarget;
            string nameLocal;
            Guid identifierLocal;
            lock (_lock) {
                addressTarget = _addressTarget;
                portTarget = _portTarget;
                nameLocal = _nameLocal;
                identifierLocal = _identifierLocal;
            }
            
            INetworkChannel channel = null;
            try {
                Stopwatch watchHandshake = Stopwatch.StartNew();
                using (CancellationTokenSource sourceConnect = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                    sourceConnect.CancelAfter(_optionsNetwork.ConnectTimeoutMilliseconds);
                    channel = await _transportNetwork.ConnectAsync(addressTarget, portTarget, sourceConnect.Token).ConfigureAwait(false);
                }
                if (channel == null) {
                    TransitionStatus(NetworkStatus.Failed, "The room did not accept a connection.");
                    return false;
                }
                
                NetworkWelcomeMessage welcome;
                using (CancellationTokenSource sourceHandshake = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                    sourceHandshake.CancelAfter(_optionsNetwork.HandshakeTimeoutMilliseconds);
                    NetworkHelloMessage hello = new NetworkHelloMessage();
                    hello.ProtocolVersion = _optionsNetwork.ProtocolVersion;
                    hello.ApplicationName = _optionsNetwork.ApplicationName;
                    hello.ApplicationVersion = _optionsNetwork.ApplicationVersion;
                    hello.DisplayName = nameLocal;
                    hello.Identifier = identifierLocal;
                    await SendMessageAsync(channel, hello, sourceHandshake.Token).ConfigureAwait(false);
                    
                    byte[] payload = await ReadFrameAsync(channel, _optionsNetwork.MaxMessageBytes, sourceHandshake.Token).ConfigureAwait(false);
                    if (payload == null) {
                        TransitionStatus(NetworkStatus.Failed, "The room closed the connection during the handshake.");
                        return false;
                    }
                    string kindRead;
                    INetworkMessage message;
                    if (!_serializerNetwork.TryDeserialize(payload, out kindRead, out message)) {
                        TransitionStatus(NetworkStatus.Failed, "The room answered with an unreadable handshake.");
                        return false;
                    }
                    NetworkRefusedMessage refused = message as NetworkRefusedMessage;
                    if (refused != null) {
                        TransitionStatus(NetworkStatus.Failed, refused.Reason);
                        return false;
                    }
                    welcome = message as NetworkWelcomeMessage;
                    if (welcome == null) {
                        TransitionStatus(NetworkStatus.Failed, "The room answered with an unexpected handshake.");
                        return false;
                    }
                    if (welcome.ProtocolVersion != _optionsNetwork.ProtocolVersion) {
                        string labelVersion = welcome.ProtocolVersion > _optionsNetwork.ProtocolVersion ? "Outdated Client" : "Outdated Server";
                        TransitionStatus(NetworkStatus.Failed, $"{labelVersion}. The room speaks protocol version {welcome.ProtocolVersion} while this peer speaks {_optionsNetwork.ProtocolVersion}.");
                        return false;
                    }
                }
                
                NetworkConnection connection = new NetworkConnection(new Optional<ILogger>(_logger));
                connection.Channel = channel;
                connection.Identifier = welcome.HostIdentifier;
                connection.DisplayName = welcome.Info == null ? "" : welcome.Info.HostName;
                connection.Address = channel.RemoteAddress;
                connection.HostFlag = true;
                if (watchHandshake.ElapsedMilliseconds > int.MaxValue) {
                    connection.MillisecondsLatency = -1;
                } else {
                    connection.MillisecondsLatency = (int)watchHandshake.ElapsedMilliseconds;
                }
                if (!_connections.TryAdd(connection.Identifier, connection)) {
                    TransitionStatus(NetworkStatus.Failed, "The room identified itself twice.");
                    return false;
                }
                lock (_lock) {
                    _identifierHost = connection.Identifier;
                    if (welcome.Info != null) {
                        _infoCurrent = welcome.Info.Clone();
                        _infoCurrent.PlayerCount = welcome.Peers == null ? 1 : welcome.Peers.Count;
                    }
                }
                
                ApplyRoster(welcome.Peers, connection);
                UpdatePeerLatency(connection.Identifier, connection.MillisecondsLatency);
                TransitionStatus(NetworkStatus.Connected, "Joined the room at '" + addressTarget + ":" + portTarget + "'.");
                _logger.Info($"Joined the room of '{connection.DisplayName}' at '{connection.Address}'.");
                
                StartLinkPumps(connection, token);
                await connection.ReceiveLoopTask.ConfigureAwait(false);
                
                TeardownConnection(connection, "The link to the room ended.");
                // An orderly shutdown already reported Disconnected; it must not be rewritten as a failure,
                // and it must not look like a lost link to the reconnection logic.
                if (!_flagDisconnectRequested && !token.IsCancellationRequested && Status != NetworkStatus.Disconnected && Status != NetworkStatus.Idle) {
                    TransitionStatus(NetworkStatus.Failed, "The connection to the room was lost.");
                }
                return true;
            } finally {
                if (channel != null) {
                    channel.Close();
                }
            }
        }
        
        /// <summary>
        /// Keeps every link alive, measures latency, notices silent peers and drives reconnection.
        /// </summary>
        /// <param name="token">The session token.</param>
        private async Task RunWatchdogAsync(CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    await Task.Delay(WatchdogIntervalMilliseconds, token).ConfigureAwait(false);
                    foreach (NetworkConnection connection in _connections.Values) {
                        if (connection.QueueOverflowedFlag) {
                            _logger.Warning($"The peer at '{connection.Address}' cannot keep up with the traffic.");
                            HandleLinkLost(connection, "The remote peer could not keep up with the traffic.");
                            continue;
                        }
                        if (connection.IdleMilliseconds > _optionsNetwork.IdleTimeoutMilliseconds) {
                            _logger.Warning($"The peer at '{connection.Address}' stopped answering.");
                            HandleLinkLost(connection, "The remote peer stopped answering.");
                            continue;
                        }
                        long timestampPayload;
                        if (connection.TryBeginPing(_optionsNetwork.KeepAliveIntervalMilliseconds, out timestampPayload)) {
                            NetworkPingMessage ping = new NetworkPingMessage();
                            ping.TimestampTicks = timestampPayload;
                            connection.EnqueueFrame(SerializeToFrame(ping));
                        }
                    }
                }
            } catch (OperationCanceledException exception) {
                // The session was closed.
                _logger.Debug("The operation was cancelled, so it stops here.");
                return;
                
                // The session was closed.
            } catch (Exception exception) {
                ReportFault("The network watchdog stopped unexpectedly.", exception);
            }
        }
        
        /// <summary>
        /// Reads framed payloads from one link and turns them into session notifications.
        /// </summary>
        /// <param name="connection">The link to read from.</param>
        /// <param name="token">The link token.</param>
        private async Task RunReceiveLoopAsync(NetworkConnection connection, CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    INetworkChannel channel = connection.Channel;
                    if (channel == null) {
                        break;
                    }
                    byte[] payload = await ReadFrameAsync(channel, _optionsNetwork.MaxMessageBytes, token).ConfigureAwait(false);
                    if (payload == null) {
                        break;
                    }
                    connection.MarkReceived();
                    string kindRead;
                    INetworkMessage message;
                    if (!_serializerNetwork.TryDeserialize(payload, out kindRead, out message)) {
                        _logger.Warning($"A payload of kind '{kindRead}' from '{connection.Address}' was dropped because it could not be read.");
                        continue;
                    }
                    HandleMessage(connection, kindRead, message);
                }
            } catch (OperationCanceledException exception) {
                // The link was closed.
                _logger.Debug("The operation was cancelled, so it stops here.");
                return;
                
                // The link was closed.
            } catch (NetworkProtocolException exception) {
                _logger.Warning($"The link to '{connection.Address}' violated the protocol: {exception.Message}");
            } catch (IOException exception) {
                _logger.Warning($"The link to '{connection.Address}' ended: {exception.Message}");
            } catch (ObjectDisposedException exception) {
                // The link was closed while a read was pending.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
                
                // The link was closed while a read was pending.
            } finally {
                connection.Dispose();
            }
        }
        
        /// <summary>
        /// Writes queued frames to one link, one at a time, so that a burst cannot interleave.
        /// </summary>
        /// <param name="connection">The link to write to.</param>
        /// <param name="token">The link token.</param>
        private async Task RunSendLoopAsync(NetworkConnection connection, CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    await connection.OutboundSignal.WaitAsync(token).ConfigureAwait(false);
                    byte[] frame;
                    while (connection.OutboundQueue.TryDequeue(out frame)) {
                        INetworkChannel channel = connection.Channel;
                        if (channel == null) {
                            return;
                        }
                        await channel.SendAsync(frame, 0, frame.Length, token).ConfigureAwait(false);
                    }
                    connection.MarkQueueDrained();
                }
            } catch (OperationCanceledException exception) {
                // The link was closed.
                _logger.Debug("The operation was cancelled, so it stops here.");
                return;
                
                // The link was closed.
            } catch (ObjectDisposedException exception) {
                // The link was closed while a write was pending.
                _logger.Debug("A recoverable condition was handled: " + exception.Message);
                
                // The link was closed while a write was pending.
            } catch (NetworkProtocolException exception) {
                _logger.Warning($"Writing to '{connection.Address}' failed: {exception.Message}");
            } catch (IOException exception) {
                _logger.Warning($"Writing to '{connection.Address}' failed: {exception.Message}");
            }
        }
        
        /// <summary>
        /// Handles one message that arrived on a link. This runs on the receive loop, so it only touches
        /// thread-safe state and never the game.
        /// </summary>
        /// <param name="connection">The link the message arrived on.</param>
        /// <param name="kindReceived">The kind identifier of the message.</param>
        /// <param name="message">The message.</param>
        private void HandleMessage(NetworkConnection connection, string kindReceived, INetworkMessage message) {
            NetworkPingMessage ping = message as NetworkPingMessage;
            if (ping != null) {
                NetworkPongMessage pong = new NetworkPongMessage();
                pong.TimestampTicks = ping.TimestampTicks;
                connection.EnqueueFrame(SerializeToFrame(pong));
                return;
            }
            NetworkPongMessage pongReceived = message as NetworkPongMessage;
            if (pongReceived != null) {
                if (connection.TryCompletePing(pongReceived.TimestampTicks)) {
                    UpdatePeerLatency(connection.Identifier, connection.MillisecondsLatency);
                }
                return;
            }
            NetworkRosterMessage roster = message as NetworkRosterMessage;
            if (roster != null) {
                ApplyRoster(roster.Peers, connection);
                return;
            }
            NetworkDisconnectMessage farewell = message as NetworkDisconnectMessage;
            if (farewell != null) {
                _logger.Info($"The peer at '{connection.Address}' disconnected: {farewell.Reason}");
                TeardownConnection(connection, farewell.Reason);
                if (connection.HostFlag) {
                    TransitionStatus(NetworkStatus.Disconnected, farewell.Reason);
                }
                return;
            }
            if (NetworkSessionMessages.IsControlKind(kindReceived)) {
                _logger.Warning($"The peer at '{connection.Address}' sent the unsupported control message '{kindReceived}'.");
                return;
            }
            
            NetworkPeer peer = EnsurePeerSnapshot(connection);
            NetworkInboundEvent notification = new NetworkInboundEvent();
            notification.Kind = NetworkInboundKind.Message;
            notification.Peer = peer;
            notification.Message = message;
            _queueInbound.Enqueue(notification);
        }
        
        /// <summary>
        /// Replaces the peer table with the authoritative roster the host sent, reporting the difference.
        /// </summary>
        /// <param name="entries">The roster entries.</param>
        /// <param name="connection">The link the roster arrived on.</param>
        private void ApplyRoster(List<NetworkPeerEntry> entries, NetworkConnection connection) {
            if (entries == null) {
                return;
            }
            Guid identifierLocal = LocalIdentifier;
            HashSet<Guid> setIncoming = new HashSet<Guid>();
            for (int index = 0; index < entries.Count; index += 1) {
                NetworkPeerEntry entry = entries[index];
                if (entry == null || entry.Identifier == Guid.Empty) {
                    continue;
                }
                if (!setIncoming.Add(entry.Identifier)) {
                    continue;
                }
                bool flagLocal = entry.Identifier == identifierLocal;
                bool flagHost = entry.IsHost;
                if (flagLocal) {
                    flagHost = Role == NetworkRole.Host;
                }
                string addressEntry = flagLocal ? "" : connection.Address;
                AddOrUpdatePeer(entry.Identifier, entry.DisplayName, addressEntry, entry.LatencyMilliseconds, flagHost, flagLocal, true);
            }
            
            List<Guid> listRemoved = new List<Guid>();
            foreach (KeyValuePair<Guid, NetworkPeerRecord> pair in _peersKnown) {
                if (pair.Key == identifierLocal) {
                    continue;
                }
                if (!setIncoming.Contains(pair.Key)) {
                    listRemoved.Add(pair.Key);
                }
            }
            for (int index = 0; index < listRemoved.Count; index += 1) {
                RemovePeer(listRemoved[index], true);
            }
        }
        
        /// <summary>
        /// Creates the local participant so that a player list always shows the local peer first.
        /// </summary>
        private void AddLocalPeer() {
            Guid identifier = LocalIdentifier;
            string name = LocalDisplayName;
            bool flagHost = Role == NetworkRole.Host;
            AddOrUpdatePeer(identifier, name, "", -1, flagHost, true, false);
        }
        
        /// <summary>
        /// Adds a participant or refreshes the snapshot of one that is already known.
        /// </summary>
        /// <param name="identifier">The identifier of the peer.</param>
        /// <param name="name">The display name of the peer.</param>
        /// <param name="address">The remote endpoint, or an empty string for the local peer.</param>
        /// <param name="latencyMilliseconds">The measured latency, or -1 when unknown.</param>
        /// <param name="flagHost">True when the peer hosts the session.</param>
        /// <param name="flagLocal">True when the peer is the local process.</param>
        /// <param name="flagNotify">True to report a newly discovered peer to the game.</param>
        private void AddOrUpdatePeer(Guid identifier, string name, string address, int latencyMilliseconds, bool flagHost, bool flagLocal, bool flagNotify) {
            if (identifier == Guid.Empty) {
                return;
            }
            string nameEffective;
            if (string.IsNullOrWhiteSpace(name)) {
                nameEffective = "Player";
            } else {
                nameEffective = name.Trim();
            }
            string addressEffective = address;
            if (addressEffective == null) {
                addressEffective = "";
            }
            
            NetworkPeerRecord record;
            if (_peersKnown.TryGetValue(identifier, out record)) {
                int latencyEffective = latencyMilliseconds;
                if (identifier == HostLinkIdentifier && record.Peer.LatencyMilliseconds >= 0) {
                    latencyEffective = record.Peer.LatencyMilliseconds;
                }
                record.Peer = new NetworkPeer(identifier, nameEffective, addressEffective, latencyEffective, flagHost, flagLocal);
                return;
            }
            
            NetworkPeerRecord recordNew = new NetworkPeerRecord();
            recordNew.Sequence = Interlocked.Increment(ref _sequencePeer);
            recordNew.Peer = new NetworkPeer(identifier, nameEffective, addressEffective, latencyMilliseconds, flagHost, flagLocal);
            if (!_peersKnown.TryAdd(identifier, recordNew)) {
                return;
            }
            if (flagNotify && !flagLocal) {
                NetworkInboundEvent notification = new NetworkInboundEvent();
                notification.Kind = NetworkInboundKind.PeerJoined;
                notification.Peer = recordNew.Peer;
                _queueInbound.Enqueue(notification);
            }
        }
        
        /// <summary>
        /// Removes a participant and reports the departure.
        /// </summary>
        /// <param name="identifier">The identifier of the peer.</param>
        /// <param name="flagNotify">True to report the departure to the game.</param>
        private void RemovePeer(Guid identifier, bool flagNotify) {
            NetworkPeerRecord record;
            if (!_peersKnown.TryRemove(identifier, out record)) {
                return;
            }
            if (flagNotify && !record.Peer.IsLocal) {
                NetworkInboundEvent notification = new NetworkInboundEvent();
                notification.Kind = NetworkInboundKind.PeerLeft;
                notification.Peer = record.Peer;
                _queueInbound.Enqueue(notification);
            }
        }
        
        /// <summary>
        /// Updates the measured latency of one participant.
        /// </summary>
        /// <param name="identifier">The identifier of the peer.</param>
        /// <param name="latencyMilliseconds">The measured round-trip time.</param>
        private void UpdatePeerLatency(Guid identifier, int latencyMilliseconds) {
            NetworkPeerRecord record;
            if (!_peersKnown.TryGetValue(identifier, out record)) {
                return;
            }
            NetworkPeer peerPrevious = record.Peer;
            record.Peer = new NetworkPeer(peerPrevious.Identifier, peerPrevious.DisplayName, peerPrevious.Address, latencyMilliseconds, peerPrevious.IsHost, peerPrevious.IsLocal);
        }
        
        /// <summary>
        /// Returns the snapshot of a peer, creating one when a message arrives before the roster did.
        /// </summary>
        /// <param name="connection">The link the message arrived on.</param>
        /// <returns>The snapshot of the peer.</returns>
        private NetworkPeer EnsurePeerSnapshot(NetworkConnection connection) {
            NetworkPeerRecord record;
            if (_peersKnown.TryGetValue(connection.Identifier, out record)) {
                return record.Peer;
            }
            AddOrUpdatePeer(connection.Identifier, connection.DisplayName, connection.Address, connection.MillisecondsLatency, connection.HostFlag, false, false);
            if (_peersKnown.TryGetValue(connection.Identifier, out record)) {
                return record.Peer;
            }
            return new NetworkPeer(connection.Identifier, connection.DisplayName, connection.Address, connection.MillisecondsLatency, connection.HostFlag, false);
        }
        
        /// <summary>
        /// Builds the authoritative participant list a host sends to its clients.
        /// </summary>
        /// <returns>The roster entries in display order.</returns>
        private List<NetworkPeerEntry> BuildRoster() {
            IReadOnlyList<NetworkPeer> peers = Peers;
            List<NetworkPeerEntry> roster = new List<NetworkPeerEntry>(peers.Count);
            for (int index = 0; index < peers.Count; index += 1) {
                NetworkPeer peer = peers[index];
                NetworkPeerEntry entry = new NetworkPeerEntry();
                entry.Identifier = peer.Identifier;
                entry.DisplayName = peer.DisplayName;
                entry.IsHost = peer.IsHost;
                entry.LatencyMilliseconds = peer.LatencyMilliseconds;
                roster.Add(entry);
            }
            return roster;
        }
        
        /// <summary>
        /// Sends the current roster to every client but the one that just caused the change.
        /// </summary>
        /// <param name="identifierExcept">The identifier to skip, or <see cref="Guid.Empty"/> to reach every client.</param>
        private void BroadcastRoster(Guid identifierExcept) {
            if (Role != NetworkRole.Host) {
                return;
            }
            NetworkRosterMessage roster = new NetworkRosterMessage();
            roster.Peers = BuildRoster();
            byte[] frame = SerializeToFrame(roster);
            foreach (NetworkConnection connection in _connections.Values) {
                if (connection.Identifier == identifierExcept) {
                    continue;
                }
                connection.EnqueueFrame(frame);
            }
        }
        
        /// <summary>
        /// Queues a message for every remote peer of the session.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="flagBestEffort">True to log and drop an unregistered message instead of throwing.</param>
        private void BroadcastFrame(INetworkMessage message, bool flagBestEffort) {
            byte[] frame;
            try {
                frame = SerializeToFrame(message);
            } catch (NetworkProtocolException exception) {
                if (!flagBestEffort) {
                    throw;
                }
                _logger.Warning(exception.Message);
                return;
            }
            foreach (NetworkConnection connection in _connections.Values) {
                connection.EnqueueFrame(frame);
            }
        }
        
        /// <summary>
        /// Serializes a message and wraps it in a transport frame.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The framed payload.</returns>
        private byte[] SerializeToFrame(INetworkMessage message) {
            byte[] payload = _serializerNetwork.Serialize(message);
            return NetworkFrameCodec.Encode(payload);
        }
        
        /// <summary>
        /// Serializes a message and writes it whole, for the handshake paths that need an ordered write.
        /// </summary>
        /// <param name="channel">The channel to write to.</param>
        /// <param name="message">The message to write.</param>
        /// <param name="token">The cancellation token.</param>
        private async Task SendMessageAsync(INetworkChannel channel, INetworkMessage message, CancellationToken token) {
            byte[] frame = SerializeToFrame(message);
            await channel.SendAsync(frame, 0, frame.Length, token).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Reads one complete frame.
        /// </summary>
        /// <param name="channel">The channel to read from.</param>
        /// <param name="countLimit">The largest payload that is accepted.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The payload, or null when the channel closed before a frame started.</returns>
        /// <exception cref="NetworkProtocolException">Thrown if the frame is malformed or oversized.</exception>
        private static async Task<byte[]> ReadFrameAsync(INetworkChannel channel, int countLimit, CancellationToken token) {
            byte[] header = new byte[NetworkFrameCodec.HeaderSize];
            int countHeader = await ReadExactlyAsync(channel, header, 0, header.Length, token).ConfigureAwait(false);
            if (countHeader == 0) {
                return null;
            }
            if (countHeader != header.Length) {
                throw new NetworkProtocolException("The connection ended in the middle of a frame header.");
            }
            int countPayload = NetworkFrameCodec.DecodeLength(header);
            if (countPayload <= 0) {
                throw new NetworkProtocolException($"A frame announced an invalid payload length of {countPayload} bytes.");
            }
            if (countPayload > countLimit) {
                throw new NetworkProtocolException($"A frame of {countPayload} bytes exceeds the limit of {countLimit} bytes.");
            }
            byte[] payload = new byte[countPayload];
            int countRead = await ReadExactlyAsync(channel, payload, 0, countPayload, token).ConfigureAwait(false);
            if (countRead != countPayload) {
                throw new NetworkProtocolException("The connection ended in the middle of a frame payload.");
            }
            return payload;
        }
        
        /// <summary>
        /// Reads until the requested number of bytes arrived or the channel closed.
        /// </summary>
        /// <param name="channel">The channel to read from.</param>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset to start writing at.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The number of bytes that were read; fewer than requested when the channel closed.</returns>
        private static async Task<int> ReadExactlyAsync(INetworkChannel channel, byte[] buffer, int offset, int count, CancellationToken token) {
            int countTotal = 0;
            while (countTotal < count) {
                int countChunk = await channel.ReceiveAsync(buffer, offset + countTotal, count - countTotal, token).ConfigureAwait(false);
                if (countChunk <= 0) {
                    return countTotal;
                }
                countTotal += countChunk;
            }
            return countTotal;
        }
        
        /// <summary>
        /// Starts the two pumps of a link.
        /// </summary>
        /// <param name="connection">The link to pump.</param>
        /// <param name="token">The session token.</param>
        private void StartLinkPumps(NetworkConnection connection, CancellationToken token) {
            CancellationToken tokenLink = connection.LinkSource.Token;
            connection.SendLoopTask = _poolThread.RunAsync(delegate() { return RunSendLoopAsync(connection, tokenLink); }, "Network.Send");
            connection.ReceiveLoopTask = _poolThread.RunAsync(delegate() { return RunReceiveLoopAsync(connection, tokenLink); }, "Network.Receive");
        }
        
        /// <summary>
        /// Removes a link, reports the departure of its peer and keeps the roster consistent.
        /// </summary>
        /// <param name="connection">The link to remove.</param>
        /// <param name="reason">The reason recorded for diagnostics.</param>
        private void TeardownConnection(NetworkConnection connection, string reason) {
            if (connection == null) {
                return;
            }
            NetworkConnection connectionRemoved;
            if (_connections.TryRemove(connection.Identifier, out connectionRemoved)) {
                _logger.Info($"The connection to '{connection.Address}' ended: {reason}");
            }
            RemovePeer(connection.Identifier, true);
            connection.Dispose();
            UpdateRoomPlayerCount();
            BroadcastRoster(Guid.Empty);
        }
        
        /// <summary>
        /// Handles a link the watchdog declared silent.
        /// </summary>
        /// <param name="connection">The silent link.</param>
        /// <param name="reason">The reason reported to the game.</param>
        private void HandleLinkLost(NetworkConnection connection, string reason) {
            bool flagHostLink = connection.HostFlag && Role == NetworkRole.Client;
            TeardownConnection(connection, reason);
            if (flagHostLink && !_flagDisconnectRequested) {
                TransitionStatus(NetworkStatus.Failed, reason);
            }
        }
        
        /// <summary>
        /// Publishes the current number of players in the room description.
        /// </summary>
        private void UpdateRoomPlayerCount() {
            lock (_lock) {
                if (_infoCurrent != null) {
                    _infoCurrent.PlayerCount = _peersKnown.Count;
                }
            }
        }
        
        /// <summary>
        /// Moves the session to a new state and reports the transition to the game.
        /// </summary>
        /// <param name="statusCurrent">The state to enter.</param>
        /// <param name="reason">A human readable explanation, or null.</param>
        private void TransitionStatus(NetworkStatus statusCurrent, string reason) {
            NetworkStatus statusPrevious;
            lock (_lock) {
                if (_status == statusCurrent) {
                    return;
                }
                statusPrevious = _status;
                _status = statusCurrent;
            }
            string reasonReported = reason;
            if (reasonReported == null) {
                reasonReported = "";
            }
            _logger.Info($"The network session moved from '{statusPrevious}' to '{statusCurrent}'. {reasonReported}");
            NetworkInboundEvent notification = new NetworkInboundEvent();
            notification.Kind = NetworkInboundKind.StatusChanged;
            notification.StatusPrevious = statusPrevious;
            notification.CurrentStatus = statusCurrent;
            notification.Reason = reasonReported;
            _queueInbound.Enqueue(notification);
        }
        
        /// <summary>
        /// Logs a background failure and reports it as a failed session.
        /// </summary>
        /// <param name="message">The context of the failure.</param>
        /// <param name="exception">The failure.</param>
        private void ReportFault(string message, Exception exception) {
            _logger.Error(message, exception);
            TransitionStatus(NetworkStatus.Failed, message + " " + exception.Message);
        }
        
        /// <summary>
        /// Rejects a second session while one is still running.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if a session exists or is being established.</exception>
        private void EnsureNoSession() {
            NetworkStatus statusCurrent;
            lock (_lock) {
                statusCurrent = _status;
            }
            if (statusCurrent == NetworkStatus.Connecting || statusCurrent == NetworkStatus.Hosting || statusCurrent == NetworkStatus.Connected || statusCurrent == NetworkStatus.Disconnecting) {
                throw new InvalidOperationException($"The network session is '{statusCurrent}'. Disconnect before starting another session.");
            }
        }
        
        /// <summary>
        /// Rejects a send while no session can carry messages.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no session is hosting or connected.</exception>
        private void EnsureSessionActive() {
            NetworkStatus statusCurrent;
            lock (_lock) {
                statusCurrent = _status;
            }
            if (statusCurrent == NetworkStatus.Hosting || statusCurrent == NetworkStatus.Connected) {
                return;
            }
            throw new InvalidOperationException($"The network session is '{statusCurrent}'. Host or connect before sending messages.");
        }
        
        /// <summary>
        /// Prepares the state a fresh session needs and opens its cancellation scope.
        /// </summary>
        private void ResetSession() {
            bool flagPreviousOpen;
            lock (_lock) {
                flagPreviousOpen = _flagSessionOpen;
            }
            if (flagPreviousOpen) {
                // A session that failed keeps its loops alive until they are told to stop.
                CloseSession("A new session replaced the previous one.", NetworkStatus.Idle);
            }
            CancellationToken tokenApplication = _serviceCancellation.GetTokenForOperation(OperationSession);
            CancellationTokenSource sourceSession = CancellationTokenSource.CreateLinkedTokenSource(tokenApplication);
            lock (_lock) {
                _sourceSession = sourceSession;
                _flagSessionOpen = true;
                _listener = null;
                _taskSession = null;
                _taskWatchdog = null;
            }
            _connections.Clear();
            _peersKnown.Clear();
            _sequencePeer = 0;
            NetworkInboundEvent notificationDiscarded;
            while (_queueInbound.TryDequeue(out notificationDiscarded)) {
                // Notifications of the previous session are obsolete once a new one starts.
            }
        }
        
        /// <summary>
        /// Gets the identifier of the link that leads to the host of the session.
        /// </summary>
        private Guid HostLinkIdentifier {
            get {
                lock (_lock) {
                    return _identifierHost;
                }
            }
        }
        
        /// <summary>
        /// Gets the name of the room for a log message.
        /// </summary>
        /// <returns>The room name, or an empty string when no room exists.</returns>
        private string RoomNameForLog() {
            lock (_lock) {
                if (_infoCurrent == null) {
                    return "";
                }
                return _infoCurrent.RoomName;
            }
        }
        
        /// <summary>
        /// Limits a remote display name to something safe to show and to put in a roster.
        /// </summary>
        /// <param name="name">The announced name.</param>
        /// <returns>The sanitized name.</returns>
        private static string SanitizeDisplayName(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return "Player";
            }
            string nameTrimmed = name.Trim();
            if (nameTrimmed.Length <= DisplayNameLimit) {
                return nameTrimmed;
            }
            return nameTrimmed.Substring(0, DisplayNameLimit);
        }
        
        /// <summary>
        /// Closes the listener, every link and the session cancellation scope.
        /// </summary>
        /// <param name="reason">The reason reported to the game.</param>
        /// <param name="statusFinal">The state to publish once everything is closed.</param>
        private void CloseSession(string reason, NetworkStatus statusFinal) {
            bool flagWasOpen;
            CancellationTokenSource sourceSession;
            INetworkListener listener;
            lock (_lock) {
                flagWasOpen = _flagSessionOpen;
                _flagSessionOpen = false;
                sourceSession = _sourceSession;
                _sourceSession = null;
                listener = _listener;
                _listener = null;
            }
            if (!flagWasOpen) {
                TransitionStatus(statusFinal, reason);
                return;
            }
            
            if (listener != null) {
                listener.Close();
            }
            if (sourceSession != null) {
                try {
                    sourceSession.Cancel();
                } catch (ObjectDisposedException exceptionReleased) {
                    // The scope was already released.
                    _logger.Debug("The resource was already released: " + exceptionReleased.Message);
                }
            }
            
            foreach (NetworkConnection connection in _connections.Values) {
                TeardownConnection(connection, reason);
            }
            _connections.Clear();
            _peersKnown.Clear();
            _sequencePeer = 0;
            
            aitForTask(_taskSession, ShutdownWaitMilliseconds);
            WaitForTask(_taskWatchdog, ShutdownWaitMilliseconds);
            lock (_lock) {
                _taskSession = null;
                _taskWatchdog = null;
                _infoCurrent = null;
                _port = 0;
                _identifierLocal = Guid.Empty;
                _identifierHost = Guid.Empty;
                _nameLocal = "";
                _addressTarget = "";
                _portTarget = 0;
            }
            if (sourceSession != null) {
                sourceSession.Dispose();
            }
            NetworkInboundEvent notificationDiscarded;
            while (_queueInbound.TryDequeue(out notificationDiscarded)) {
                // Notifications of the closed session are obsolete.
            }
            TransitionStatus(statusFinal, reason);
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
                _logger.Warning($"A background network loop ended with an error: {exception.Message}");
            } catch (ObjectDisposedException exceptionReleased) {
                // The task was already released.
                _logger.Debug("The resource was already released: " + exceptionReleased.Message);
            }
        }
        
        /// <summary>
        /// Gives queued farewell messages a moment to leave, then closes the session.
        /// </summary>
        /// <param name="reason">The reason reported to the game.</param>
        private async Task CloseSessionAfterFlushAsync(string reason) {
            try {
                await Task.Delay(DisconnectFlushMilliseconds).ConfigureAwait(false);
                CloseSession(reason, NetworkStatus.Disconnected);
            } catch (Exception exception) {
                // Nothing observes this task, so it reports its own failure instead of faulting.
                ReportFault("The session could not be closed after the farewell was sent.", exception);
            }
        }
    }
}
