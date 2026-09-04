using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>A simple sprite wrapper around a texture region.</summary>
    public class Sprite {
        public TextureRegion Region { get; set; }
        public Color Color { get; set; } = Color.White;
        public TwoDimensionalVector Scale { get; set; } = TwoDimensionalVector.One;
        public TwoDimensionalVector Origin { get; set; } = TwoDimensionalVector.Zero;
        public float Width {
            get { return Region == null ? 0.0f : Region.Width * Scale.X; }
        }
        public float Height {
            get { return Region == null ? 0.0f : Region.Height * Scale.Y; }
        }
        
        public Sprite() {
        }
        
        public Sprite(TextureRegion region) {
            Region = region;
        }
        
        public void Draw(IRenderContext contextRender, TwoDimensionalVector position) {
            if (contextRender == null || Region == null || Region.Texture == null) {
                return;
            }
            Region.Texture.DrawInto(
                contextRender,
                position,
                new OptionalValue<Rectangle>(Region.SourceRectangle),
                Color,
                0.0f,
                Origin,
                Scale,
                SpriteEffects.None,
                0.0f
            );
        }
    }
}
