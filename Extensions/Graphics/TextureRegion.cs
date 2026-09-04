using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>Represents a rectangular region inside a texture.</summary>
    public class TextureRegion {
        public ITwoDimensionalTexture Texture { get; set; }
        public Rectangle SourceRectangle { get; set; }
        public int Width { get { return SourceRectangle.Width; } }
        public int Height { get { return SourceRectangle.Height; } }
        public float TopTextureCoordinate { get { return (float)SourceRectangle.Top / Texture.Height; } }
        public float BottomTextureCoordinate { get { return (float)SourceRectangle.Bottom / Texture.Height; } }
        public float LeftTextureCoordinate { get { return (float)SourceRectangle.Left / Texture.Width; } }
        public float RightTextureCoordinate { get { return (float)SourceRectangle.Right / Texture.Width; } }
        
        public TextureRegion() {
        }
        
        public TextureRegion(ITwoDimensionalTexture texture, Rectangle rectangleSource) {
            Texture = texture;
            SourceRectangle = rectangleSource;
        }
    }
}
