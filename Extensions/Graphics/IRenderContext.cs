using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Abstraction over a 2D rendering context.
    /// Manages rendering state (begin/end, clear, render target binding) and exposes
    /// two drawing paths: the general asset driven path where a texture draws itself
    /// through double dispatch, and the direct path used by hot loops that draw many
    /// sprites per frame. Both paths produce identical output; the direct path exists
    /// because it does not allocate a visitor per call.
    /// </summary>
    public interface IRenderContext : IDisposable {
        /// <summary>
        /// Gets the render target that is currently bound, or an empty optional when
        /// drawing goes to the default back buffer.
        /// </summary>
        Optional<IRenderTarget> CurrentTarget { get; }
        
        /// <summary>
        /// Gets the area of the current target that is being rendered, in pixels.
        /// </summary>
        Rectangle ViewportBounds { get; }
        
        /// <summary>
        /// Begins a rendering block. All draw operations must occur between <see cref="Begin"/> and <see cref="End"/>. 
        /// </summary>
        /// <param name="stateSampler">Optional sampler state (point, linear, etc.). Defaults to point clamping if not specified. </param>
        /// <param name="stateBlend">Optional blend state. Defaults to alpha blending if not specified. </param>
        /// <param name="effect">Optional shader effect applied to the block. </param>
        /// <param name="transform">
        /// Optional transform applied to every coordinate of the block.
        /// An empty optional means no transform, which is equivalent to <see cref="TwoDimensionalTransform.Identity"/>.
        /// </param>
        void Begin(
            Optional<SamplerState> stateSampler = default, 
            Optional<BlendState> stateBlend = default, 
            Optional<IEffect> effect = default,
            OptionalValue<TwoDimensionalTransform> transform = default
        );
        
        /// <summary>Ends the rendering block and flushes all buffered draw calls. </summary>
        void End();
        
        /// <summary>Clears the entire render target to the specified given color. </summary>
        /// <param name="color">The color written to every pixel of the current target.</param>
        void Clear(Color color);
        
        /// <summary>
        /// Binds a render target so that subsequent drawing goes into it instead of
        /// the back buffer. The previous target and viewport are remembered and are
        /// restored by <see cref="ResetRenderTarget"/>.
        /// </summary>
        /// <param name="targetRenderTarget">The target to bind.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetRenderTarget"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        /// Thrown by an implementation that cannot bind the supplied target type.
        /// </exception>
        void SetRenderTarget(IRenderTarget targetRenderTarget);
        
        /// <summary>
        /// Restores the target and viewport that were active before the last
        /// <see cref="SetRenderTarget"/> call, or the back buffer when none was bound.
        /// </summary>
        void ResetRenderTarget();
        
        /// <summary>
        /// Draws a texture at a position with rotation, origin, scale, flip effects,
        /// source rectangle and tint. This is the direct drawing path; it performs no
        /// allocation and is intended for loops that issue many draw calls per frame.
        /// </summary>
        /// <param name="texture">The texture to draw.</param>
        /// <param name="position">Screen position (top-left origin).</param>
        /// <param name="rectangleSource">Optional sub-rectangle of the texture to draw.</param>
        /// <param name="color">Color tint. Use <see cref="Color.White"/> for no tint.</param>
        /// <param name="rotation">Rotation angle in radians.</param>
        /// <param name="origin">Rotation/pivot point relative to the texture (in pixels).</param>
        /// <param name="scale">Uniform or non-uniform scale factor.</param>
        /// <param name="effectsSprite">Flip effects.</param>
        /// <param name="depthLayer">Sort depth (0 = front, 1 = back).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="texture"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        /// Thrown by an implementation that cannot draw the supplied texture type.
        /// </exception>
        void DrawTexture(
            ITwoDimensionalTexture texture,
            TwoDimensionalVector position,
            OptionalValue<Rectangle> rectangleSource,
            Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            SpriteEffects effectsSprite,
            float depthLayer
        );
        
        /// <summary>
        /// Draws a sub-rectangle of a texture stretched onto a destination rectangle.
        /// This is the direct drawing path; it performs no allocation.
        /// </summary>
        /// <param name="texture">The texture to draw.</param>
        /// <param name="rectangleDestination">The destination rectangle in render coordinates.</param>
        /// <param name="rectangleSource">Optional sub-rectangle of the texture to draw.</param>
        /// <param name="color">Color tint. Use <see cref="Color.White"/> for no tint.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="texture"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        /// Thrown by an implementation that cannot draw the supplied texture type.
        /// </exception>
        void DrawTextureRegion(
            ITwoDimensionalTexture texture,
            Rectangle rectangleDestination,
            OptionalValue<Rectangle> rectangleSource,
            Color color
        );
        
        /// <summary>
        /// Accepts a visitor and allows it to perform drawing operations on this context. 
        /// The visitor can safely access the underlying rendering engine through pattern matching. 
        /// </summary>
        /// <param name="visitor">The visitor to accept.</param>
        void Accept(IVisitor visitor);
    }
}