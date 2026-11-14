using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Reports the viewport aspect ratio used by a three-dimensional camera.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IThreeDimensionalViewport {
        /// <summary>
        /// Gets the current width-to-height ratio.
        /// </summary>
        float AspectRatio { get; }
    }
}
