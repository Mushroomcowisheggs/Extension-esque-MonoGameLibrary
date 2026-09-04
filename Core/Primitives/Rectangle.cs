namespace MonoGameLibrary.Core.Primitives {
    /// <summary>
    /// The axis-aligned rectangle. 
    /// </summary>
    public struct Rectangle {
        /// <summary>X coordinate of the top-left corner.</summary>
        public int X;
        
        /// <summary>Y coordinate of the top-left corner.</summary>
        public int Y;
        
        /// <summary>Width of the rectangle.</summary>
        public int Width;
        
        /// <summary>Height of the rectangle.</summary>
        public int Height;
        
        /// <summary>Creates a new rectangle from position and size.</summary>
        public Rectangle(int x, int y, int width, int height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        
        /// <summary>Gets the Y coordinate of the bottom edge.</summary>
        public int Bottom { get { return Y + Height; } }
        
        /// <summary>Gets the Y coordinate of the top edge.</summary>
        public int Top { get { return Y; } }
        
        /// <summary>Gets the X coordinate of the right edge.</summary>
        public int Right { get { return X + Width; } }
        
        /// <summary>Gets the X coordinate of the left edge.</summary>
        public int Left { get { return X; } }
        
        /// <summary>Checks whether the given point lies inside this rectangle.</summary>
        public bool Contains(int x, int y) { return x >= X && x < Right && y >= Y && y < Bottom; }
        
        /// <summary>Expands or contracts this rectangle by the supplied amounts.</summary>
        public void Inflate(int amountHorizontal, int amountVertical) {
            X -= amountHorizontal;
            Y -= amountVertical;
            Width += amountHorizontal * 2;
            Height += amountVertical * 2;
        }
    }
}
