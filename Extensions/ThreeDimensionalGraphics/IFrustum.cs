namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Tests whether a three-dimensional bounding volume intersects a view frustum.
    /// </summary>
    public interface IFrustum {
        /// <summary>
        /// Tests a bounding sphere against the frustum.
        /// </summary>
        /// <param name="sphereBounds">The sphere to test.</param>
        /// <returns><c>true</c> when the sphere is not fully outside the frustum.</returns>
        bool Intersects(BoundingSphere sphereBounds);
    }
}
