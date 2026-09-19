using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The default <see cref="INetworkTransport"/>, providing reliable ordered streams over TCP.
    /// </summary>
    /// <remarks>
    /// A listener prefers a dual-mode IPv6 socket so that a room is reachable over both address families
    /// from a single port, and falls back to IPv4 on hosts that have no IPv6 stack.
    /// </remarks>
    public sealed class TcpNetworkTransport : INetworkTransport {
        /// <summary>
        /// The number of pending connections the operating system queues before the host accepts them.
        /// </summary>
        private const int BacklogSize = 32;
        
        private readonly ILogger _logger;
        private int _flagDisposed;
        
        /// <summary>
        /// Initializes a new transport.
        /// </summary>
        /// <param name="logger">An optional logger that records conditions the transport recovers from.</param>
        public TcpNetworkTransport(Optional<ILogger> logger = default) {
            if (logger.HasValue) { _logger = logger.Value; } else { _logger = NullLogger.Instance; }
        }
        
        /// <inheritdoc />
        public Task<INetworkListener> ListenAsync(int port, CancellationToken token) {
            if (Volatile.Read(ref _flagDisposed) != 0) {
                throw new ObjectDisposedException(nameof(TcpNetworkTransport));
            }
            if (port < 0 || port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(port), "A port must be between 0 and 65535.");
            }
            if (token.IsCancellationRequested) {
                return Task.FromCanceled<INetworkListener>(token);
            }
            
            Socket socketListener = null;
            SocketException exceptionIpv6 = null;
            if (Socket.OSSupportsIPv6) {
                try {
                    socketListener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    socketListener.DualMode = true;
                    socketListener.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                } catch (SocketException exception) {
                    _logger.Debug($"The machine cannot listen over IPv6 on port {port}: {exception.Message}");
                    exceptionIpv6 = exception;
                    socketListener.Dispose();
                    socketListener = null;
                }
            }
            
            if (socketListener == null) {
                try {
                    socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketListener.Bind(new IPEndPoint(IPAddress.Any, port));
                } catch (SocketException exception) {
                    socketListener.Dispose();
                    if (exceptionIpv6 != null) {
                        throw new NetworkProtocolException($"Port {port} cannot be bound: {exception.Message}", exception);
                    }
                    throw new NetworkProtocolException($"Port {port} cannot be bound: {exception.Message}", exception);
                }
            }
            
            socketListener.Listen(BacklogSize);
            int portBound = ((IPEndPoint)socketListener.LocalEndPoint).Port;
            INetworkListener listener = new TcpNetworkListener(socketListener, portBound, new Optional<ILogger>(_logger));
            return Task.FromResult(listener);
        }
        
        /// <inheritdoc />
        public async Task<INetworkChannel> ConnectAsync(string addressHost, int port, CancellationToken token) {
            if (Volatile.Read(ref _flagDisposed) != 0) {
                throw new ObjectDisposedException(nameof(TcpNetworkTransport));
            }
            if (string.IsNullOrWhiteSpace(addressHost)) {
                throw new ArgumentException("A host name or address is required.", nameof(addressHost));
            }
            if (port < 1 || port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(port), "A port must be between 1 and 65535.");
            }
            
            Socket socketClient = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try {
                await socketClient.ConnectAsync(addressHost, port, token).ConfigureAwait(false);
            } catch (SocketException exception) {
                socketClient.Dispose();
                throw new NetworkProtocolException($"'{addressHost}:{port}' cannot be reached: {exception.Message}", exception);
            } catch (OperationCanceledException) {
                socketClient.Dispose();
                throw;
            } catch (ArgumentException exception) {
                socketClient.Dispose();
                throw new NetworkProtocolException($"'{addressHost}' is not a valid host name or address.", exception);
            }
            
            return new TcpNetworkChannel(socketClient, new Optional<ILogger>(_logger));
        }
        
        /// <inheritdoc />
        public void Dispose() {
            Interlocked.Exchange(ref _flagDisposed, 1);
            GC.SuppressFinalize(this);
        }
    }
}
