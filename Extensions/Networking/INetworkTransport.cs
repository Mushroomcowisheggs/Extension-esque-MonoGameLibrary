using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Creates channels and listeners for one transport technology. <see cref="TcpNetworkTransport"/> is the
    /// default and provides reliable, ordered delivery; a real-time game can supply a datagram
    /// implementation without touching <see cref="INetworkService"/>.
    /// </summary>
    /// <remarks>
    /// Implementations are consumed from background loops, so they must be thread-safe.
    /// </remarks>
    public interface INetworkTransport : IDisposable {
        /// <summary>
        /// Binds a listener to the given port.
        /// </summary>
        /// <param name="port">The port to bind, or zero to let the operating system choose one.</param>
        /// <param name="token">A token that cancels the bind.</param>
        /// <returns>The bound listener.</returns>
        /// <exception cref="NetworkProtocolException">Thrown if the port cannot be bound.</exception>
        Task<INetworkListener> ListenAsync(int port, CancellationToken token);
        
        /// <summary>
        /// Opens a channel to a remote endpoint.
        /// </summary>
        /// <param name="addressHost">The host name or address literal to reach.</param>
        /// <param name="port">The port to reach.</param>
        /// <param name="token">A token that cancels the attempt.</param>
        /// <returns>The connected channel.</returns>
        /// <exception cref="NetworkProtocolException">Thrown if the remote endpoint cannot be reached.</exception>
        Task<INetworkChannel> ConnectAsync(string addressHost, int port, CancellationToken token);
    }
}
