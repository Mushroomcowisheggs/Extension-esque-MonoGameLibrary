namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Creates off-screen render targets that match the graphics device back buffer.
    /// Render targets are owned by the caller and must be disposed by it.
    /// </summary>
    public interface IRenderTargetFactory {
        /// <summary>
        /// Creates a render target with the supplied pixel size.
        /// </summary>
        /// <param name="width">Target width in pixels. Must be greater than zero.</param>
        /// <param name="height">Target height in pixels. Must be greater than zero.</param>
        /// <param name="flagPreserveContents">
        /// True to keep the contents of the target between bindings; false to allow the
        /// backend to discard them, which is cheaper.
        /// </param>
        /// <returns>The newly created render target.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="width"/> or <paramref name="height"/> is not positive.
        /// </exception>
        IRenderTarget Create(int width, int height, bool flagPreserveContents);
    }
}