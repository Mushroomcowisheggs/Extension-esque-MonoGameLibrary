using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame-specific implementation of <see cref="IRenderContext"/>. 
    /// Wraps a <see cref="SpriteBatch"/> and provides internal methods for 
    /// drawing textures and strings, accessible only by sibling adapter classes. 
    /// </summary>
    internal sealed class RenderContext : IRenderContext {
        private readonly Microsoft.Xna.Framework.Graphics.SpriteBatch _batchSprite;
        private bool _flagDisposed;
        
        /// <summary>
        /// Gets the MonoGame <see cref="SpriteBatch"/> used for drawing. 
        /// Intended for internal use by adapter classes only. 
        /// </summary>
        internal Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch { get { return _batchSprite; } }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContext"/> class. 
        /// </summary>
        /// <param name="batchSprite">The <see cref="SpriteBatch"/> to use. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="batchSprite"/> is null. </exception>
        public RenderContext(Microsoft.Xna.Framework.Graphics.SpriteBatch batchSprite) {
            if (batchSprite == null) { throw new ArgumentNullException(nameof(batchSprite)); }
            _batchSprite = batchSprite;
        }
        
        /// <summary>
        /// Draws a MonoGame <see cref="Texture2D"/> using the internal sprite batch. 
        /// Called by <see cref="TwoDimensionalTexture"/> via visitor pattern. 
        /// </summary>
        internal void DrawTextureInternal(
            Texture2D texture,
            TwoDimensionalVector position,
            OptionalValue<MonoGameLibrary.Core.Primitives.Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            Nullable<Microsoft.Xna.Framework.Rectangle> source = rectangleSource.HasValue
            ? new Microsoft.Xna.Framework.Rectangle(rectangleSource.Value.X, rectangleSource.Value.Y, rectangleSource.Value.Width, rectangleSource.Value.Height)
            : new Nullable<Microsoft.Xna.Framework.Rectangle>();
            
            _batchSprite.Draw(
                texture,
                new Microsoft.Xna.Framework.Vector2(position.X, position.Y),
                source,
                new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A),
                rotation,
                new Microsoft.Xna.Framework.Vector2(origin.X, origin.Y),
                new Microsoft.Xna.Framework.Vector2(scale.X, scale.Y),
                ConvertSpriteEffects(effectsSprite),
                depthLayer
            );
        }
        
        /// <summary>
        /// Draws a string using a MonoGame <see cref="Microsoft.Xna.Framework.Graphics.SpriteFont"/>. 
        /// Called by <see cref="SpriteFont"/> via visitor pattern. 
        /// </summary>
        internal void DrawStringInternal(
            Microsoft.Xna.Framework.Graphics.SpriteFont font,
            string text,
            TwoDimensionalVector position,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            float scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            _batchSprite.DrawString(
                font,
                text,
                new Microsoft.Xna.Framework.Vector2(position.X, position.Y),
                new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A),
                rotation,
                new Microsoft.Xna.Framework.Vector2(origin.X, origin.Y),
                scale,
                ConvertSpriteEffects(effectsSprite),
                depthLayer
            );
        }
        
        private static Microsoft.Xna.Framework.Graphics.BlendState ConvertBlend(MonoGameLibrary.Extensions.Graphics.BlendState stateBlend) {
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.AlphaBlendState) {
                return Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;
            }
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.AdditiveState) {
                return Microsoft.Xna.Framework.Graphics.BlendState.Additive;
            }
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.OpaqueState) {
                return Microsoft.Xna.Framework.Graphics.BlendState.Opaque;
            }
            return Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;
        }
        
        private static Microsoft.Xna.Framework.Graphics.SamplerState ConvertSampler(MonoGameLibrary.Extensions.Graphics.SamplerState stateSampler) {
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.PointClampState) {
                return Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp;
            }
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.PointWrapState) {
                return Microsoft.Xna.Framework.Graphics.SamplerState.PointWrap;
            }
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.LinearClampState) {
                return Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp;
            }
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.LinearWrapState) {
                return Microsoft.Xna.Framework.Graphics.SamplerState.LinearWrap;
            }
            return Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp;
        }
        
        private static Microsoft.Xna.Framework.Graphics.SpriteEffects ConvertSpriteEffects(MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite) {
            Microsoft.Xna.Framework.Graphics.SpriteEffects result = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            if ((effectsSprite & MonoGameLibrary.Extensions.Graphics.SpriteEffects.FlipHorizontally) != 0) {
                result |= Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
            }
            if ((effectsSprite & MonoGameLibrary.Extensions.Graphics.SpriteEffects.FlipVertically) != 0) {
                result |= Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipVertically;
            }
            return result;
        }
        
        /// <inheritdoc />
        public void Begin(
            Optional<MonoGameLibrary.Extensions.Graphics.SamplerState> stateSampler = default, 
            Optional<MonoGameLibrary.Extensions.Graphics.BlendState> stateBlend = default, 
            Optional<IEffect> effect = default
        ) {
            var stateMonoGameSampler = stateSampler.HasValue 
            ? ConvertSampler(stateSampler.Value) 
            : Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp;
            var stateMonoGameBlend = stateBlend.HasValue 
            ? ConvertBlend(stateBlend.Value) 
            : Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;
            
            Microsoft.Xna.Framework.Graphics.Effect effectNative = null;
                if (effect.HasValue) {
                    var effectConcrete = effect.Value as Effect;
                    if (effectConcrete != null) {
                        effectNative = effectConcrete.NativeEffect;
                    } else {
                        throw new InvalidOperationException(
                            "The provided effect is not a MonoGame Effect adapter."
                        );
                    }
                }
            
            _batchSprite.Begin(
                samplerState: stateMonoGameSampler, 
                blendState: stateMonoGameBlend, 
                effect: effectNative
            );
        }
        
        /// <inheritdoc />
        public void End() {
            _batchSprite.End();
        }
        
        /// <inheritdoc />
        public void Clear(MonoGameLibrary.Core.Primitives.Color color) {
            _batchSprite.GraphicsDevice.Clear(new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A));
        }
        
        /// <inheritdoc />
        public void Accept(IVisitor visitor) {
            if (visitor == null) {
                throw new ArgumentNullException(nameof(visitor));
            }
            visitor.Visit(this);
        }
        
        /// <summary>
        /// Releases resources held by this context. 
        /// Note: The underlying <see cref="SpriteBatch"/> is owned by the game host and shall not be disposed here. 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) { return; }
            _flagDisposed = true;
            // SpriteBatch is managed externally; do not dispose it.
            GC.SuppressFinalize(this);
        }
    }
}
