namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Reports the viewport aspect ratio used by a three-dimensional camera.
    /// </summary>
    public interface IThreeDimensionalViewport {
        /// <summary>
        /// Gets the current width-to-height ratio.
        /// </summary>
        float AspectRatio { get; }
    }
}
