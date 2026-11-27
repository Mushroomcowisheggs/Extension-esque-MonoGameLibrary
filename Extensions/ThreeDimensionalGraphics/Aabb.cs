using System;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Represents a three-dimensional axis-aligned bounding box.
    /// </summary>
    public readonly struct Aabb {
        /// <summary>
        /// Initializes a new axis-aligned bounding box.
        /// </summary>
        /// <param name="minimum">The minimum corner.</param>
        /// <param name="maximum">The maximum corner.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when any axis of <paramref name="maximum"/> is below the matching axis of
        /// <paramref name="minimum"/>.
        /// </exception>
        public Aabb(Vector3 minimum, Vector3 maximum) {
            if (maximum.X < minimum.X || maximum.Y < minimum.Y || maximum.Z < minimum.Z) {
                throw new ArgumentException(
                    "The maximum corner must not be below the minimum corner."
                );
            }
            Minimum = minimum;
            Maximum = maximum;
        }
        
        /// <summary>Gets the minimum corner.</summary>
        public Vector3 Minimum { get; }
        
        /// <summary>Gets the maximum corner.</summary>
        public Vector3 Maximum { get; }
        
        /// <summary>Gets the box center.</summary>
        public Vector3 Center {
            get { return (Minimum + Maximum) * 0.5f; }
        }
        
        /// <summary>Gets the box extents along each axis.</summary>
        public Vector3 Size {
            get { return Maximum - Minimum; }
        }
    }
}
