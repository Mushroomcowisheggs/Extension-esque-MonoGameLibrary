using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Describes a compiled three-dimensional model asset.
    /// </summary>
    public interface IModel : IAsset, System.IDisposable {
        /// <summary>Gets a conservative model-space bounding sphere.</summary>
        BoundingSphere Bounds { get; }
    }
}
