using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;
using Rectangle = MonoGameLibrary.Core.Primitives.Rectangle;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame-specific implementation of <see cref="IRenderContext"/>. 
    /// Wraps a <see cref="SpriteBatch"/> and provides the state, target and drawing
    /// operations declared by the extension contract. 
    /// </summary>
    internal sealed class RenderContext : IRenderContext {
        private readonly Microsoft.Xna.Framework.Graphics.SpriteBatch _batchSprite;
        private readonly MonoGameLibrary.Core.Concurrency.ThreadAccess _accessThread = new MonoGameLibrary.Core.Concurrency.ThreadAccess();
        private readonly Stack<(RenderTargetBinding[] Bindings, Microsoft.Xna.Framework.Graphics.Viewport Viewport, Microsoft.Xna.Framework.Rectangle Scissor)> _stackTargets = new Stack<(RenderTargetBinding[] Bindings, Microsoft.Xna.Framework.Graphics.Viewport Viewport, Microsoft.Xna.Framework.Rectangle Scissor)>();
        private IRenderTarget _targetCurrent;
        private bool _flagBatchActive;
        private bool _flagDisposed;
        
        private void VerifyAccess() {
            _accessThread.VerifyAccess();
            if (_flagDisposed) { throw new ObjectDisposedException(nameof(RenderContext)); }
        }
        
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
        
        /// <inheritdoc />
        public Optional<IRenderTarget> CurrentTarget {
            get {
                if (_targetCurrent == null) {
                    return default;
                }
                return new Optional<IRenderTarget>(_targetCurrent);
            }
        }
        
        /// <inheritdoc />
        public Rectangle ViewportBounds {
            get {
                Microsoft.Xna.Framework.Graphics.Viewport viewport = _batchSprite.GraphicsDevice.Viewport;
                return new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height);
            }
        }
        
        /// <summary>
        /// Draws a MonoGame <see cref="Texture2D"/> using the internal sprite batch. 
        /// </summary>
        internal void DrawTextureInternal(
            Texture2D texture,
            TwoDimensionalVector position,
            OptionalValue<Rectangle> rectangleSource,
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
        /// Draws a MonoGame <see cref="Texture2D"/> stretched onto a destination rectangle. 
        /// </summary>
        internal void DrawTextureRegionInternal(
            Texture2D texture,
            Rectangle rectangleDestination,
            OptionalValue<Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color
        ) {
            Microsoft.Xna.Framework.Rectangle destination = new Microsoft.Xna.Framework.Rectangle(
                rectangleDestination.X, rectangleDestination.Y, rectangleDestination.Width, rectangleDestination.Height
            );
            Nullable<Microsoft.Xna.Framework.Rectangle> source = rectangleSource.HasValue
            ? new Nullable<Microsoft.Xna.Framework.Rectangle>(
                new Microsoft.Xna.Framework.Rectangle(
                    rectangleSource.Value.X, rectangleSource.Value.Y, rectangleSource.Value.Width, rectangleSource.Value.Height
                )
            )
            : new Nullable<Microsoft.Xna.Framework.Rectangle>();
            
            _batchSprite.Draw(
                texture,
                destination,
                source,
                new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A)
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
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.NonPremultipliedState) {
                return Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied;
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
        
        private static Matrix ConvertTransform(TwoDimensionalTransform transformTransform) {
            Matrix matrixTransform = Matrix.CreateScale(transformTransform.ScaleX, transformTransform.ScaleY, 1f);
            if (transformTransform.Rotation != 0f) {
                matrixTransform = matrixTransform * Matrix.CreateRotationZ(transformTransform.Rotation);
            }
            matrixTransform = matrixTransform * Matrix.CreateTranslation(
                transformTransform.TranslationX, transformTransform.TranslationY, 0f
            );
            return matrixTransform;
        }
        
        /// <inheritdoc />
        public void Begin(
            Optional<MonoGameLibrary.Extensions.Graphics.SamplerState> stateSampler = default, 
            Optional<MonoGameLibrary.Extensions.Graphics.BlendState> stateBlend = default, 
            Optional<IEffect> effect = default,
            OptionalValue<TwoDimensionalTransform> transform = default
        ) {
            VerifyAccess();
            if (_flagBatchActive) {
                throw new InvalidOperationException("A rendering batch is already active.");
            }
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
            
            Matrix matrixTransform = Matrix.Identity;
            if (transform.HasValue) {
                matrixTransform = ConvertTransform(transform.Value);
            }
            
            _batchSprite.Begin(
                samplerState: stateMonoGameSampler, 
                blendState: stateMonoGameBlend, 
                effect: effectNative,
                transformMatrix: matrixTransform
            );
            _flagBatchActive = true;
        }
        
        /// <inheritdoc />
        public void End() {
            VerifyAccess();
            if (!_flagBatchActive) {
                throw new InvalidOperationException("No rendering batch is active.");
            }
            _batchSprite.End();
            _flagBatchActive = false;
        }
        
        /// <inheritdoc />
        public void Clear(MonoGameLibrary.Core.Primitives.Color color) {
            VerifyAccess();
            if (_flagBatchActive) {
                throw new InvalidOperationException("End the rendering batch before clearing the target.");
            }
            _batchSprite.GraphicsDevice.Clear(new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A));
        }
        
        /// <inheritdoc />
        public void SetRenderTarget(IRenderTarget targetRenderTarget) {
            VerifyAccess();
            if (targetRenderTarget == null) {
                throw new ArgumentNullException(nameof(targetRenderTarget));
            }
            if (_flagBatchActive) {
                throw new InvalidOperationException("End the rendering batch before binding a render target.");
            }
            Microsoft.Xna.Framework.Graphics.GraphicsDevice deviceGraphics = _batchSprite.GraphicsDevice;
            RenderTarget2D targetNative = ResolveTarget(targetRenderTarget, deviceGraphics);
            _stackTargets.Push((deviceGraphics.GetRenderTargets(), deviceGraphics.Viewport, deviceGraphics.ScissorRectangle));
            _targetCurrent = targetRenderTarget;
            deviceGraphics.SetRenderTarget(targetNative);
        }
        
        /// <inheritdoc />
        public void ResetRenderTarget() {
            VerifyAccess();
            if (_flagBatchActive) {
                throw new InvalidOperationException("End the rendering batch before restoring a render target.");
            }
            Microsoft.Xna.Framework.Graphics.GraphicsDevice deviceGraphics = _batchSprite.GraphicsDevice;
            if (_stackTargets.Count == 0) {
                _targetCurrent = null;
                deviceGraphics.SetRenderTarget(null);
                return;
            }
            (RenderTargetBinding[] Bindings, Microsoft.Xna.Framework.Graphics.Viewport Viewport, Microsoft.Xna.Framework.Rectangle Scissor) statePrevious = _stackTargets.Pop();
            _targetCurrent = null;
            deviceGraphics.SetRenderTargets(statePrevious.Bindings);
            deviceGraphics.Viewport = statePrevious.Viewport;
            deviceGraphics.ScissorRectangle = statePrevious.Scissor;
        }
        
        private static RenderTarget2D ResolveTarget(
            IRenderTarget targetRenderTarget,
            Microsoft.Xna.Framework.Graphics.GraphicsDevice deviceGraphics
        ) {
            RenderTarget targetTyped = targetRenderTarget as RenderTarget;
            if (targetTyped == null) {
                throw new NotSupportedException(
                    "The render target is not a MonoGame render target adapter."
                );
            }
            RenderTarget2D targetNative = targetTyped.NativeTarget;
            if (!ReferenceEquals(targetNative.GraphicsDevice, deviceGraphics)) {
                throw new InvalidOperationException("The render target belongs to another graphics device.");
            }
            return targetNative;
        }
        
        /// <inheritdoc />
        public void DrawTexture(
            ITwoDimensionalTexture texture,
            TwoDimensionalVector position,
            OptionalValue<Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            VerifyAccess();
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }
            if (!_flagBatchActive) {
                throw new InvalidOperationException("Begin a rendering batch before drawing.");
            }
            DrawTextureInternal(
                ResolveTexture(texture), position, rectangleSource, color, rotation, origin, scale, effectsSprite, depthLayer
            );
        }
        
        /// <inheritdoc />
        public void DrawTextureRegion(
            ITwoDimensionalTexture texture,
            Rectangle rectangleDestination,
            OptionalValue<Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color
        ) {
            VerifyAccess();
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }
            if (!_flagBatchActive) {
                throw new InvalidOperationException("Begin a rendering batch before drawing.");
            }
            DrawTextureRegionInternal(ResolveTexture(texture), rectangleDestination, rectangleSource, color);
        }
        
        private static Texture2D ResolveTexture(ITwoDimensionalTexture texture) {
            INativeTextureProvider<Texture2D> providerTexture = texture as INativeTextureProvider<Texture2D>;
            if (providerTexture == null) {
                throw new NotSupportedException(
                    $"The texture type '{texture.GetType().FullName}' is not a MonoGame texture adapter."
                );
            }
            return providerTexture.GetNativeTexture();
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
        /// Note: The underlying <see cref="SpriteBatch"/> is owned by the graphics module and shall not be disposed here. 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) { return; }
            if (_flagBatchActive) {
                try {
                    _batchSprite.End();
                } catch (Exception exception) {
                    System.Diagnostics.Trace.TraceError("Could not end the rendering batch during disposal: {0}", exception);
                }
                _flagBatchActive = false;
            }
            if (_targetCurrent != null || _stackTargets.Count > 0) {
                // Never leave the device bound to a target the caller is about to release.
                _batchSprite.GraphicsDevice.SetRenderTarget(null);
            }
            _stackTargets.Clear();
            _targetCurrent = null;
            _flagDisposed = true;
            // SpriteBatch is managed externally; do not dispose it.
            GC.SuppressFinalize(this);
        }
    }
}