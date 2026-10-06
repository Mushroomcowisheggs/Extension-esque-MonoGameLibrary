using System;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>Represents a rectangular region inside a texture.</summary>
    public class TextureRegion {
        /// <summary>Gets or sets the texture this region reads from.</summary>
        public ITwoDimensionalTexture Texture { get; set; }
        /// <summary>Gets or sets the area of <see cref="Texture"/> this region covers.</summary>
        public Rectangle SourceRectangle { get; set; }
        /// <summary>Gets the width of <see cref="SourceRectangle"/>, in pixels.</summary>
        public int Width { get { return SourceRectangle.Width; } }
        /// <summary>Gets the height of <see cref="SourceRectangle"/>, in pixels.</summary>
        public int Height { get { return SourceRectangle.Height; } }
        /// <summary>Gets the vertical texture coordinate of the region's top edge, in the range 0 to 1.</summary>
        public float TopTextureCoordinate { get { return (float)SourceRectangle.Top / Texture.Height; } }
        /// <summary>Gets the vertical texture coordinate of the region's bottom edge, in the range 0 to 1.</summary>
        public float BottomTextureCoordinate { get { return (float)SourceRectangle.Bottom / Texture.Height; } }
        /// <summary>Gets the horizontal texture coordinate of the region's left edge, in the range 0 to 1.</summary>
        public float LeftTextureCoordinate { get { return (float)SourceRectangle.Left / Texture.Width; } }
        /// <summary>Gets the horizontal texture coordinate of the region's right edge, in the range 0 to 1.</summary>
        public float RightTextureCoordinate { get { return (float)SourceRectangle.Right / Texture.Width; } }
        
        /// <summary>Initializes an empty region. Set <see cref="Texture"/> and <see cref="SourceRectangle"/> before use.</summary>
        public TextureRegion() {
        }
        
        /// <summary>Initializes a region covering <paramref name="rectangleSource"/> of <paramref name="texture"/>.</summary>
        /// <param name="texture">The texture the region reads from.</param>
        /// <param name="rectangleSource">The area of the texture to cover.</param>
        public TextureRegion(ITwoDimensionalTexture texture, Rectangle rectangleSource) {
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }
            Texture = texture;
            SourceRectangle = rectangleSource;
        }
    }
}
