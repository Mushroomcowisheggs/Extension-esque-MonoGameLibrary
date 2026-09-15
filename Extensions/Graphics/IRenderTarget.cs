using System.IO;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// An off-screen texture that can be drawn into and then drawn from.
    /// A render target is a texture, so it can be passed to any drawing API that
    /// accepts <see cref="ITwoDimensionalTexture"/>.
    /// </summary>
    public interface IRenderTarget : ITwoDimensionalTexture {
        /// <summary>
        /// Gets a value indicating whether the previous contents survive when the
        /// target becomes active again.
        /// </summary>
        bool IsPreservingContents { get; }
        
        /// <summary>
        /// Reads the target back from the graphics device and writes it as a PNG image.
        /// Must be called on the graphics thread and while the target is not being drawn into.
        /// </summary>
        /// <param name="streamData">The destination stream for the encoded image.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="streamData"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the target has already been released.</exception>
        void SavePng(Stream streamData);
    }
}