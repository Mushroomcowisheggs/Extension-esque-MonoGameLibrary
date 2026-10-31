using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Describes a platform-neutral three-dimensional mesh asset.
    /// </summary>
    /// <remarks>
    /// Geometry data is portable. The selected adapter uploads it to its own graphics device;
    /// the interface itself carries no native handle.
    /// </remarks>
    public interface IMesh : IAsset, System.IDisposable {
        /// <summary>Gets the immutable geometry data.</summary>
        MeshData Data { get; }
    }
}
