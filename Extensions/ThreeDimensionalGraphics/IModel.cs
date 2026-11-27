using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Describes a compiled three-dimensional model asset.
    /// </summary>
    /// <remarks>
    /// The selected adapter constructs the implementation; the render service accepts only that
    /// implementation.
    /// </remarks>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IModel : IAsset, System.IDisposable {
        /// <summary>
        /// Gets the model-space conservative culling hull that is guaranteed to contain
        /// all model geometry: the platform mesh bounding spheres are transformed by
        /// their absolute bone transforms and merged. Exact dimensions are only
        /// available through <see cref="BoundsAabb"/>.
        /// </summary>
        BoundingSphere BoundsBoundingSphere { get; }
        
        /// <summary>
        /// Gets the exact model-space axis-aligned bounding box, or null when the back
        /// end cannot compute it exactly. A null value never masquerades as a coarse
        /// approximation: a back end that cannot measure the geometry exactly reports
        /// null instead of substituting a sphere-enclosed box.
        /// </summary>
        Aabb? BoundsAabb { get; }
    }
}
