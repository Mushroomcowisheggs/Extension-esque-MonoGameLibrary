using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>A simple sprite wrapper around a texture region.</summary>
    public class Sprite {
        /// <summary>Gets or sets the region this sprite draws. A null region draws nothing and reports a zero size.</summary>
        public TextureRegion Region { get; set; }
        /// <summary>Gets or sets the tint applied when drawing.</summary>
        public Color Color { get; set; } = Color.White;
        /// <summary>Gets or sets the scale applied to the region's size.</summary>
        public TwoDimensionalVector Scale { get; set; } = TwoDimensionalVector.One;
        /// <summary>Gets or sets the origin used for positioning, in region pixels.</summary>
        public TwoDimensionalVector Origin { get; set; } = TwoDimensionalVector.Zero;
        /// <summary>Gets the drawn width in pixels, or 0 when <see cref="Region"/> is null.</summary>
        public float Width {
            get { return Region == null ? 0.0f : Region.Width * Scale.X; }
        }
        /// <summary>Gets the drawn height in pixels, or 0 when <see cref="Region"/> is null.</summary>
        public float Height {
            get { return Region == null ? 0.0f : Region.Height * Scale.Y; }
        }
        
        /// <summary>Initializes a sprite with no region.</summary>
        public Sprite() {
        }
        
        /// <summary>Initializes a sprite that draws <paramref name="region"/>. Use the parameterless constructor for a sprite with no region.</summary>
        /// <param name="region">The region to draw; must not be null.</param>
        public Sprite(TextureRegion region) {
            if (region == null) {
                throw new ArgumentNullException(nameof(region));
            }
            Region = region;
        }
        
        /// <summary>Draws the sprite at <paramref name="position"/>. Returns without drawing when the context, the region or its texture is null.</summary>
        /// <param name="contextRender">The render context to draw into.</param>
        /// <param name="position">Where to draw, in context coordinates.</param>
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
