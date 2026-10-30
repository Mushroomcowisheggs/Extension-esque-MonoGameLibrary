using System;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Represents a three-dimensional bounding sphere.
    /// </summary>
    public readonly struct BoundingSphere {
        /// <summary>
        /// Initializes a new bounding sphere.
        /// </summary>
        /// <param name="center">The sphere center.</param>
        /// <param name="radius">The sphere radius.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radius"/> is negative.</exception>
        public BoundingSphere(Vector3 center, float radius) {
            if (radius < 0f) {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }
            Center = center;
            Radius = radius;
        }
        
        /// <summary>Gets the sphere center.</summary>
        public Vector3 Center { get; }
        
        /// <summary>Gets the sphere radius.</summary>
        public float Radius { get; }
    }
}
