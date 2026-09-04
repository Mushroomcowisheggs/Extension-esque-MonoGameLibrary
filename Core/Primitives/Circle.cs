using System;

namespace MonoGameLibrary.Core.Primitives {
    /// <summary>
    /// A simple circle value type defined by center (X,Y) and radius. 
    /// </summary>
    public readonly struct Circle : IEquatable<Circle> {
        /// <summary>Gets the center X coordinate. </summary>
        public int X { get; }
        
        /// <summary>Gets the center Y coordinate. </summary>
        public int Y { get; }
        
        /// <summary>Gets the radius. </summary>
        public int Radius { get; }
        
        /// <summary>
        /// Initializes a new circle with the given center and radius.
        /// </summary>
        /// <param name="x">Center X. </param>
        /// <param name="y">Center Y. </param>
        /// <param name="radius">Radius (must be positive). </param>
        public Circle(int x, int y, int radius) {
            X = x;
            Y = y;
            Radius = radius;
        }
        
        /// <summary>Gets the center location as a point. </summary>
        public Point Location {
            get { return new Point(X, Y); }
        }
        
        /// <summary>Gets the topmost Y coordinate (Y - Radius). </summary>
        public int Top {
            get { return Y - Radius; }
        }
        
        /// <summary>Gets the bottommost Y coordinate (Y + Radius). </summary>
        public int Bottom {
            get { return Y + Radius; }
        }
        
        /// <summary>Gets the leftmost X coordinate (X - Radius). </summary>
        public int Left {
            get { return X - Radius; }
        }
        
        /// <summary>Gets the rightmost X coordinate (X + Radius). </summary>
        public int Right {
            get { return X + Radius; }
        }
        
        /// <summary>Indicates whether the current circle equals another circle. </summary>
        public bool Equals(Circle other) {
            return X == other.X && Y == other.Y && Radius == other.Radius;
        }
        
        /// <summary>
        /// Checks whether this circle intersects another circle. 
        /// </summary>
        /// <param name="other">The other circle. </param>
        /// <returns>True if they overlap; otherwise false. </returns>
        public bool Intersects(Circle other) {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            return distance < Radius + other.Radius;
        }
        
        /// <summary>Determines whether the specified object is equal to this circle. </summary>
        public override bool Equals(object obj) {
            if (obj is Circle other) {
                return Equals(other);
            }
            return false;
        }
        
        /// <summary>Returns a hash code for this circle. </summary>
        public override int GetHashCode() {
            return HashCode.Combine(X, Y, Radius);
        }
        
        /// <summary>Compares two circles for equality. </summary>
        public static bool operator ==(Circle left, Circle right) {
            return left.Equals(right);
        }
        
        /// <summary>Compares two circles for inequality. </summary>
        public static bool operator !=(Circle left, Circle right) {
            return !left.Equals(right);
        }
    }
}
