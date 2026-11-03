using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Describes a compiled three-dimensional model asset.
    /// </summary>
    /// <remarks>
    /// The selected adapter constructs the implementation; the render service accepts only that
    /// implementation.
    /// </remarks>
    public interface IModel : IAsset, System.IDisposable {
        /// <summary>Gets a conservative model-space bounding sphere.</summary>
        BoundingSphere BoundsBoundingSphere { get; }
    }
}
