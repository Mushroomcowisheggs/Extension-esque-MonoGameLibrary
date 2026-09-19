using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// An <see cref="INetworkListener"/> backed by a bound TCP socket. Accepting uses the asynchronous
    /// socket API, so the host never blocks a thread while waiting for players.
    /// </summary>
    public sealed class TcpNetworkListener : INetworkListener {
        private readonly ILogger _logger;
        private readonly Socket _socketListener;
        private readonly int _port;
        private int _stateClosed;
        
        /// <summary>
        /// Initializes a listener around an already bound and listening socket.
        /// </summary>
        /// <param name="socketListener">The bound socket.</param>
        /// <param name="port">The port the socket is bound to.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="socketListener"/> is null.</exception>
        public TcpNetworkListener(Socket socketListener, int port, Optional<ILogger> logger = default) {
            if (socketListener == null) {
                throw new ArgumentNullException(nameof(socketListener));
            }
            _socketListener = socketListener;
            _port = port;
            if (logger.HasValue) { _logger = logger.Value; } else { _logger = NullLogger.Instance; }
        }
        
        /// <inheritdoc />
        public int Port {
            get {
                return _port;
            }
        }
        
        /// <inheritdoc />
        public async Task<INetworkChannel> AcceptAsync(CancellationToken token) {
            if (Volatile.Read(ref _stateClosed) != 0) {
                return null;
            }
            
            Socket socketAccepted;
            try {
                socketAccepted = await _socketListener.AcceptAsync(token).ConfigureAwait(false);
            } catch (ObjectDisposedException exception) {
                _logger.Debug($"The listener on port {_port} was closed while accepting: {exception.Message}");
                return null;
            } catch (SocketException exception) {
                if (Volatile.Read(ref _stateClosed) != 0) {
                    _logger.Debug($"The listener on port {_port} stopped accepting: {exception.Message}");
                    return null;
                }
                throw;
            }
            
            if (socketAccepted == null) {
                return null;
            }
            return new TcpNetworkChannel(socketAccepted);
        }
        
        /// <inheritdoc />
        public void Close() {
            if (Interlocked.Exchange(ref _stateClosed, 1) != 0) {
                return;
            }
            // Socket.Dispose is idempotent, so no guard is needed here.
            _socketListener.Dispose();
        }
        
        /// <inheritdoc />
        public void Dispose() {
            Close();
            GC.SuppressFinalize(this);
        }
    }
}
