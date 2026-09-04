namespace MonoGameLibrary.Core.Primitives {
    /// <summary>
    /// The RGBA color. 
    /// </summary>
    public struct Color {
        /// <summary>Red component (0-255).</summary>
        public byte R;
        
        /// <summary>Green component (0-255).</summary>
        public byte G;
        
        /// <summary>Blue component (0-255).</summary>
        public byte B;
        
        /// <summary>Alpha component (0-255).</summary>
        public byte A;
        
        /// <summary>Creates a fully opaque color from RGB values.</summary>
        public Color(byte r, byte g, byte b) : this(r, g, b, 255) { }
        
        /// <summary>Creates a color with explicit RGBA values.</summary>
        public Color(byte r, byte g, byte b, byte a) {
            R = r; G = g; B = b; A = a;
        }
        
        /// <summary>Creates a color with alpha (0.0-1.0) and opaque RGB.</summary>
        public static Color FromArgb(float a, byte r, byte g, byte b) {
            return new Color(r, g, b, (byte)(a * 255f));
        }
        
        /// <summary>Opaque white.</summary>
        public static Color White { get { return new Color(255, 255, 255); } }
        
        /// <summary>Opaque black.</summary>
        public static Color Black { get { return new Color(0, 0, 0); } }
        
        /// <summary>Opaque gray.</summary>
        public static Color Gray { get { return new Color(128, 128, 128); } }
        
        /// <summary>Opaque dark slate gray.</summary>
        public static Color DarkSlateGray { get { return new Color(47, 79, 79); } }
        
        /// <summary>Cornflower blue (classic Xna/MonoGame clear color).</summary>
        public static Color CornflowerBlue { get { return new Color(100, 149, 237); } }
        
        /// <summary>Transparent (fully invisible) black.</summary>
        public static Color Transparent { get { return new Color(0, 0, 0, 0); } }
        
        /// <summary>Multiplies this color by an alpha factor (0.0-1.0).</summary>
        public Color MultiplyAlpha(float alpha) {
            return new Color(R, G, B, (byte)(A * alpha));
        }
    }
}
