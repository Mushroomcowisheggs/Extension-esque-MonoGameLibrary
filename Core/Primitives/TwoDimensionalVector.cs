using System;

namespace MonoGameLibrary.Core.Primitives {
    /// <summary>
    /// The 2D vector. 
    /// </summary>
    public struct TwoDimensionalVector : IEquatable<TwoDimensionalVector> {
        /// <summary>X component. </summary>
        public float X;
        
        /// <summary>Y component. </summary>
        public float Y;
        
        /// <summary>Creates a new vector from (x, y) components.</summary>
        public TwoDimensionalVector(float x, float y) {
            X = x;
            Y = y;
        }
        
        /// <summary>A vector with both components set to zero. </summary>
        public static TwoDimensionalVector Zero { get { return new TwoDimensionalVector(0f, 0f); } }
        
        /// <summary>A vector with both components set to one. </summary>
        public static TwoDimensionalVector One { get { return new TwoDimensionalVector(1f, 1f); } }
        
        /// <summary>A unit vector pointing along the X axis. </summary>
        public static TwoDimensionalVector UnitX { get { return new TwoDimensionalVector(1f, 0f); } }
        
        /// <summary>A unit vector pointing along the Y axis. </summary>
        public static TwoDimensionalVector UnitY { get { return new TwoDimensionalVector(0f, 1f); } }
        
        /// <summary>Returns a new vector with the same direction but unit length. </summary>
        public TwoDimensionalVector Normalize() {
            float length = (float)Math.Sqrt(X * X + Y * Y);
            if (length < float.Epsilon) { return Zero; }
            return new TwoDimensionalVector(X / length, Y / length);
        }
        
        /// <summary>Returns the length of this vector.</summary>
        public float Length() { return (float)Math.Sqrt(X * X + Y * Y); }
        
        /// <summary>Returns the squared length of this vector.</summary>
        public float LengthSquared() { return X * X + Y * Y; }
        
        /// <summary>Returns a normalized copy of the supplied vector.</summary>
        public static TwoDimensionalVector Normalize(TwoDimensionalVector vector) { return vector.Normalize(); }
        
        /// <summary>Calculates the dot product of two vectors.</summary>
        public static float Dot(TwoDimensionalVector left, TwoDimensionalVector right) { return left.X * right.X + left.Y * right.Y; }
        
        /// <summary>Reflects a vector around the supplied surface.</summary>
        public static TwoDimensionalVector Reflect(TwoDimensionalVector vector, TwoDimensionalVector vectorNormal) {
            return vector - vectorNormal * (2.0f * Dot(vector, vectorNormal));
        }
        
        /// <summary>Linearly interpolates between two vectors.</summary>
        public static TwoDimensionalVector Lerp(TwoDimensionalVector start, TwoDimensionalVector end, float amount) {
            return start + (end - start) * amount;
        }
        
        /// <summary>
        /// Compares two vectors for exact equality.
        /// This is the equality operation required by <see cref="IEquatable{T}"/> and by
        /// <see cref="GetHashCode"/>, so it is reflexive, symmetric, transitive and free of any
        /// assumed precision.
        /// </summary>
        /// <param name="other">The vector to compare against.</param>
        /// <returns>True when both components are bit-for-bit identical.</returns>
        /// <remarks>
        /// A tolerance is deliberately not applied here. Floating point numbers have a precision
        /// that scales with their magnitude, so no single tolerance is correct: any fixed epsilon is
        /// too coarse for small components, too fine for large ones, and the subtraction used to
        /// measure the difference has already lost precision by the time it is compared. A tolerance
        /// would also break transitivity and contradict <see cref="GetHashCode"/>.
        /// Use <see cref="IsNearlyEqual"/> when an approximate comparison is what the caller means.
        /// </remarks>
        public bool Equals(TwoDimensionalVector other) {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }
        
        /// <summary>
        /// Compares two vectors for approximate equality, using a tolerance that scales with the
        /// magnitude of the compared components.
        /// </summary>
        /// <param name="other">The vector to compare against.</param>
        /// <param name="relativeTolerance">
        /// The accepted difference, expressed as a fraction of the compared magnitude
        /// (for example 0.0001 accepts a difference of one ten-thousandth). Must be zero or greater.
        /// </param>
        /// <returns>True when both components are equal or differ by less than the tolerance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="relativeTolerance"/> is negative or not a number.
        /// </exception>
        public bool IsNearlyEqual(TwoDimensionalVector other, float relativeTolerance) {
            if (float.IsNaN(relativeTolerance) || relativeTolerance < 0f) {
                throw new ArgumentOutOfRangeException(nameof(relativeTolerance),
                "The relative tolerance must be zero or greater.");
            }
            return IsComponentNearlyEqual(X, other.X, relativeTolerance)
            && IsComponentNearlyEqual(Y, other.Y, relativeTolerance);
        }
        
        private static bool IsComponentNearlyEqual(float left, float right, float relativeTolerance) {
            if (left.Equals(right)) {
                // Covers the exact case, including values near zero where a relative test is
                // meaningless.
                return true;
            }
            if (float.IsNaN(left) || float.IsNaN(right)) {
                return false;
            }
            if (float.IsInfinity(left) || float.IsInfinity(right)) {
                return false;
            }
            float difference = Math.Abs(left - right);
            float magnitude = Math.Max(Math.Abs(left), Math.Abs(right));
            return difference <= relativeTolerance * magnitude;
        }
        
        /// <inheritdoc />
        public override bool Equals(object value) {
            if (value is TwoDimensionalVector other) { return Equals(other); }
            return false;
        }
        
        /// <inheritdoc />
        public override int GetHashCode() { return HashCode.Combine(X, Y); }
        
        /// <summary>Component-wise addition.</summary>
        public static TwoDimensionalVector operator +(TwoDimensionalVector a, TwoDimensionalVector b) { return new TwoDimensionalVector(a.X + b.X, a.Y + b.Y); }
        
        /// <summary>Component-wise subtraction.</summary>
        public static TwoDimensionalVector operator -(TwoDimensionalVector a, TwoDimensionalVector b) { return new TwoDimensionalVector(a.X - b.X, a.Y - b.Y); }
        
        /// <summary>Scalar multiplication.</summary>
        public static TwoDimensionalVector operator *(TwoDimensionalVector v, float s) { return new TwoDimensionalVector(v.X * s, v.Y * s); }
        
        /// <summary>Unary negation.</summary>
        public static TwoDimensionalVector operator -(TwoDimensionalVector value) { return new TwoDimensionalVector(-value.X, -value.Y); }
        
        /// <summary>Scalar division.</summary>
        public static TwoDimensionalVector operator /(TwoDimensionalVector v, float s) { return new TwoDimensionalVector(v.X / s, v.Y / s); }
        
        /// <summary>Compares two vectors for equality.</summary>
        public static bool operator ==(TwoDimensionalVector left, TwoDimensionalVector right) { return left.Equals(right); }
        
        /// <summary>Compares two vectors for inequality.</summary>
        public static bool operator !=(TwoDimensionalVector left, TwoDimensionalVector right) { return !left.Equals(right); }
    }
}
