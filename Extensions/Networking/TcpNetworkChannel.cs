using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// An <see cref="INetworkChannel"/> backed by a connected TCP socket. Reads and writes use the
    /// asynchronous socket APIs, so no thread is blocked while waiting for the network.
    /// </summary>
    public sealed class TcpNetworkChannel : INetworkChannel {
        private readonly Socket _socketTcp;
        private readonly ILogger _logger;
        private readonly string _addressRemote;
        private int _stateClosed;
        
        /// <summary>
        /// Initializes a channel around an already connected socket.
        /// </summary>
        /// <param name="socketTcp">The connected socket.</param>
        /// <param name="logger">An optional logger that records transport conditions the connection survives.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="socketTcp"/> is null.</exception>
        public TcpNetworkChannel(Socket socketTcp, Optional<ILogger> logger = default) {
            if (socketTcp == null) {
                throw new ArgumentNullException(nameof(socketTcp));
            }
            _socketTcp = socketTcp;
            if (logger.HasValue) { _logger = logger.Value; } else { _logger = NullLogger.Instance; }
            try {
                _socketTcp.NoDelay = true;
            } catch (SocketException exception) {
                _logger.Debug($"The connection to '{RemoteAddress}' continues without NoDelay: {exception.Message}");
            }
            EndPoint endpointRemote = _socketTcp.RemoteEndPoint;
            if (endpointRemote == null) {
                _addressRemote = "";
            } else {
                _addressRemote = endpointRemote.ToString();
            }
        }
        
        /// <inheritdoc />
        public string RemoteAddress {
            get {
                return _addressRemote;
            }
        }
        
        /// <inheritdoc />
        public bool IsOpen {
            get {
                if (Volatile.Read(ref _stateClosed) != 0) {
                    return false;
                }
                return _socketTcp.Connected;
            }
        }
        
        /// <inheritdoc />
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (count <= 0) {
                throw new InvalidOperationException("The requested byte count must be positive.");
            }
            if (Volatile.Read(ref _stateClosed) != 0) {
                return 0;
            }
            
            int countRead = await _socketTcp.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None, token).ConfigureAwait(false);
            return countRead;
        }
        
        /// <inheritdoc />
        public async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (count <= 0) {
                throw new InvalidOperationException("The requested byte count must be positive.");
            }
            
            int countSent = 0;
            while (countSent < count) {
                int countChunk = await _socketTcp.SendAsync(
                    new ArraySegment<byte>(buffer, offset + countSent, count - countSent),
                    SocketFlags.None,
                    token
                ).ConfigureAwait(false);
                if (countChunk <= 0) {
                    throw new NetworkProtocolException("The transmission stalled before every byte was sent.");
                }
                countSent += countChunk;
            }
        }
        
        /// <inheritdoc />
        public void Close() {
            if (Interlocked.Exchange(ref _stateClosed, 1) != 0) {
                return;
            }
            try {
                if (_socketTcp.Connected) {
                    _socketTcp.Shutdown(SocketShutdown.Both);
                }
            } catch (SocketException exception) {
                _logger.Debug($"The connection to '{RemoteAddress}' is already closed by the remote peer: {exception.Message}");
            } catch (ObjectDisposedException exception) {
                _logger.Debug($"The connection to '{RemoteAddress}' is already closed locally: {exception.Message}");
            }
            // Socket.Dispose is idempotent, so no guard is needed here.
            _socketTcp.Dispose();
        }
        
        /// <inheritdoc />
        public void Dispose() {
            Close();
            GC.SuppressFinalize(this);
        }
    }
}
