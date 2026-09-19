using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Accepts incoming connections for a host. A listener owns the bound socket and must be disposed to
    /// release the port.
    /// </summary>
    public interface INetworkListener : IDisposable {
        /// <summary>
        /// Gets the port the listener is bound to. When the host requested an ephemeral port, this is the
        /// port the operating system assigned.
        /// </summary>
        int Port { get; }
        
        /// <summary>
        /// Waits for the next incoming connection without blocking the calling thread.
        /// </summary>
        /// <param name="token">A token that cancels the pending accept.</param>
        /// <returns>The accepted channel, or null when the listener was closed before a connection arrived.</returns>
        Task<INetworkChannel> AcceptAsync(CancellationToken token);
        
        /// <summary>
        /// Closes the listener. The method is idempotent and never throws for an already closed listener.
        /// </summary>
        void Close();
    }
}
