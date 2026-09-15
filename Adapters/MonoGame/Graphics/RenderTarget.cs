using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame adapter for <see cref="IRenderTarget"/>.
    /// Wraps a <see cref="RenderTarget2D"/>, which is itself a texture, so the target can
    /// be drawn into through <see cref="IRenderContext.SetRenderTarget"/> and drawn from
    /// with any API that accepts <see cref="ITwoDimensionalTexture"/>.
    /// </summary>
    public sealed class RenderTarget :
        IRenderTarget,
        INativeTextureProvider<Texture2D> {
        /// <summary>The wrapped target, released and cleared by <see cref="Dispose"/>.</summary>
        private RenderTarget2D _target;
        private bool _flagDisposed;
        
        /// <summary>
        /// Returns the native MonoGame render target for use inside this adapter assembly,
        /// or null after this instance was disposed.
        /// </summary>
        public Texture2D GetNativeTexture() {
            return _target;
        }
        
        /// <summary>Gets the native MonoGame render target so the render context can bind it.</summary>
        /// <exception cref="ObjectDisposedException">Thrown when the target has already been released.</exception>
        internal RenderTarget2D NativeTarget {
            get {
                if (_target == null) {
                    throw new ObjectDisposedException(nameof(RenderTarget));
                }
                return _target;
            }
        }
        
        /// <inheritdoc />
        public int Width {
            get {
                if (_target == null) { return 0; }
                return _target.Width;
            }
        }
        
        /// <inheritdoc />
        public int Height {
            get {
                if (_target == null) { return 0; }
                return _target.Height;
            }
        }
        
        /// <inheritdoc />
        public bool IsPreservingContents {
            get {
                if (_target == null) { return false; }
                return _target.RenderTargetUsage == RenderTargetUsage.PreserveContents;
            }
        }
        
        /// <summary>Initializes a new instance of the <see cref="RenderTarget"/> class.</summary>
        /// <param name="target">The MonoGame render target to wrap. Ownership transfers to this instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
        public RenderTarget(RenderTarget2D target) {
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
            _target = target;
        }
        
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="contextRender"/> is null.</exception>
        public void DrawInto(
            IRenderContext contextRender,
            TwoDimensionalVector position,
            OptionalValue<Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            if (contextRender == null) {
                throw new ArgumentNullException(nameof(contextRender));
            }
            contextRender.DrawTexture(
                this, position, rectangleSource, color, rotation, origin, scale, effectsSprite, depthLayer
            );
        }
        
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="streamData"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the target has already been released.</exception>
        public void SavePng(Stream streamData) {
            if (streamData == null) { throw new ArgumentNullException(nameof(streamData)); }
            if (_flagDisposed || _target == null) { throw new ObjectDisposedException(nameof(RenderTarget)); }
            _target.SaveAsPng(streamData, _target.Width, _target.Height);
        }
        
        /// <summary>
        /// Releases the underlying render target. The operation is idempotent.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            if (_target != null) {
                _target.Dispose();
                _target = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}