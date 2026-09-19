using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// A bidirectional byte channel to one remote peer. Implementations must be safe to use from a single
    /// reader and a single writer at the same time, because <see cref="INetworkService"/> pumps the two
    /// directions from independent background loops.
    /// </summary>
    public interface INetworkChannel : IDisposable {
        /// <summary>
        /// Gets the remote endpoint in <c>host:port</c> form, used for display and diagnostics.
        /// </summary>
        string RemoteAddress { get; }
        
        /// <summary>
        /// Gets a value indicating whether the channel can still carry data.
        /// </summary>
        bool IsOpen { get; }
        
        /// <summary>
        /// Reads the next chunk of bytes into the supplied buffer without blocking the calling thread.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset to start writing at.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="token">A token that cancels the pending read.</param>
        /// <returns>The number of bytes read, or zero when the remote peer closed the channel.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="count"/> is not positive.</exception>
        Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token);
        
        /// <summary>
        /// Writes the whole supplied range, sending it in several operations when the transport requires it.
        /// </summary>
        /// <param name="buffer">The source buffer.</param>
        /// <param name="offset">The offset to start reading from.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="token">A token that cancels the pending write.</param>
        /// <returns>A task that completes when every byte has been handed to the transport.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="count"/> is not positive.</exception>
        Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token);
        
        /// <summary>
        /// Closes the channel. The method is idempotent and never throws for an already closed channel.
        /// </summary>
        void Close();
    }
}
