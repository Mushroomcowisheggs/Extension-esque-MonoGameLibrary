using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Represents a 2-dimensional texture resource. 
    /// Provides dimensions and a method to draw itself into an <see cref="IRenderContext"/>. 
    /// </summary>
    public interface ITwoDimensionalTexture : ITexture {
        /// <summary>
        /// Draws this texture into the specified render context. 
        /// The implementation (in the adapter layer) knows how to map itself
        /// onto the concrete rendering backend. 
        /// </summary>
        /// <param name="contextRender">The render context to draw into.</param>
        /// <param name="position">Screen position (top-left origin).</param>
        /// <param name="rectangleSource">Optional sub-rectangle of the texture to draw. Pass <see cref="OptionalValue{Rectangle}.None"/> to draw the full texture.</param>
        /// <param name="color">Color tint. Use <see cref="Color.White"/> for no tint.</param>
        /// <param name="rotation">Rotation angle in radians.</param>
        /// <param name="origin">Rotation/pivot point relative to the texture (in pixels).</param>
        /// <param name="scale">Uniform or non-uniform scale factor.</param>
        /// <param name="effectsSprite">Flip effects.</param>
        /// <param name="depthLayer">Sort depth (0 = front, 1 = back).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="contextRender"/> is null. </exception>
        void DrawInto(
            IRenderContext contextRender,
            TwoDimensionalVector position,
            OptionalValue<Rectangle> rectangleSource,
            Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            SpriteEffects effectsSprite,
            float depthLayer
        );
    }
}