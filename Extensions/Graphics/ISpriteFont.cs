using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Represents a bitmap font used for text rendering. 
    /// Provides metrics and a method to draw itself into an <see cref="IRenderContext"/>. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface ISpriteFont : IFont {
        /// <summary>Gets the line spacing (vertical distance between lines) of the font in pixels. </summary>
        float LineSpacing { get; }
        
        /// <summary>Gets the horizontal spacing between characters. </summary>
        float Spacing { get; }
        
        /// <summary>
        /// Draws a string into the specified render context using this font.
        /// </summary>
        /// <param name="contextRender">The render context to draw into.</param>
        /// <param name="text">The string to render.</param>
        /// <param name="position">Screen position (top-left origin).</param>
        /// <param name="color">Text color.</param>
        /// <param name="rotation">Rotation angle in radians.</param>
        /// <param name="origin">Rotation/pivot point relative to the string's top-left corner.</param>
        /// <param name="scale">Uniform scale factor.</param>
        /// <param name="effectsSprite">Flip effects.</param>
        /// <param name="depthLayer">Sort depth (0 = front, 1 = back).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="contextRender"/> or <paramref name="text"/> is null. </exception>
        void DrawInto(
            IRenderContext contextRender,
            string text,
            TwoDimensionalVector position,
            Color color,
            float rotation,
            TwoDimensionalVector origin,
            float scale,
            SpriteEffects effectsSprite,
            float depthLayer
        );
    }
}