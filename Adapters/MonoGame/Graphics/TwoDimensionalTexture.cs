using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame adapter for <see cref="ITwoDimensionalTexture"/>. 
    /// Wraps a <see cref="Texture2D"/> and implements <see cref="ITwoDimensionalTexture.DrawInto"/>
    /// by forwarding to the direct drawing path of the render context. 
    /// </summary>
    public sealed class TwoDimensionalTexture :
        ITwoDimensionalTexture,
        INativeTextureProvider<Texture2D> {
        /// <summary>The underlying MonoGame texture, released and cleared by <see cref="Dispose"/>.</summary>
        private Texture2D _texture;
        private readonly bool _flagOwnsTexture;
        private bool _flagDisposed;
        
        /// <summary>
        /// Returns the native MonoGame texture for integrations implemented
        /// inside this adapter assembly, or null after this wrapper was disposed.
        /// </summary>
        public Texture2D GetNativeTexture() {
            return _texture;
        }
        
        /// <inheritdoc/>
        public int Width {
            get {
                if (_texture == null) { return 0; }
                return _texture.Width;
            }
        }
        
        /// <inheritdoc/>
        public int Height {
            get {
                if (_texture == null) { return 0; }
                return _texture.Height;
            }
        }
        
        /// <summary>Initializes a new instance of the <see cref="TwoDimensionalTexture"/> class. </summary>
        /// <param name="texture">The MonoGame <see cref="Texture2D"/> to wrap.</param>
        /// <param name="flagOwnsTexture">
        /// True when this wrapper created the texture and must release it on dispose.
        /// False for textures owned by the content manager, whose lifetime this wrapper must not shorten.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="texture"/> is null.</exception>
        public TwoDimensionalTexture(Texture2D texture, bool flagOwnsTexture = true) {
            if (texture == null) { throw new ArgumentNullException(nameof(texture)); }
            _texture = texture;
            _flagOwnsTexture = flagOwnsTexture;
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
            
            // The interface driven path performs double dispatch so that any texture
            // implementation can render itself. Hot loops should call
            // IRenderContext.DrawTexture directly to avoid the per-call indirection.
            contextRender.DrawTexture(
                this, position, rectangleSource, color, rotation, origin, scale, effectsSprite, depthLayer
            );
        }
        
        /// <summary>
        /// Releases the texture when this wrapper owns it.
        /// Disposing an instance that borrows a content manager owned texture has no effect
        /// on that texture. The operation is idempotent.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            if (_flagOwnsTexture && _texture != null) {
                _texture.Dispose();
            }
            _texture = null;
            GC.SuppressFinalize(this);
        }
    }
}