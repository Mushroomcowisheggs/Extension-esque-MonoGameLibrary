using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Tests whether a three-dimensional bounding volume intersects a view frustum.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IFrustum {
        /// <summary>
        /// Tests a bounding sphere against the frustum.
        /// </summary>
        /// <param name="sphereBounds">The sphere to test.</param>
        /// <returns><c>true</c> when the sphere is not fully outside the frustum.</returns>
        bool Intersects(BoundingSphere sphereBounds);
        
        /// <summary>
        /// Tests an axis-aligned bounding box against the frustum with the standard
        /// six-plane test.
        /// </summary>
        /// <param name="aabbBounds">The box to test.</param>
        /// <returns><c>true</c> when the box is not fully outside the frustum.</returns>
        bool Intersects(Aabb aabbBounds);
    }
}
