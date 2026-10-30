using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Describes a renderable three-dimensional mesh asset.
    /// </summary>
    public interface IMesh : IAsset, System.IDisposable {
        /// <summary>Gets the vertex count.</summary>
        int VertexCount { get; }
        
        /// <summary>Gets the index count.</summary>
        int IndexCount { get; }
        
        /// <summary>Gets the primitive count.</summary>
        int PrimitiveCount { get; }
        
        /// <summary>Gets a conservative local-space bounding sphere.</summary>
        BoundingSphere Bounds { get; }
    }
}
