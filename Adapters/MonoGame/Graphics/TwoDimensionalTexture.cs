using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame adapter for <see cref="ITwoDimensionalTexture"/>. 
    /// Wraps a <see cref="Texture2D"/> and implements <see cref="ITwoDimensionalTexture.DrawInto"/> 
    /// by creating a visitor that delegates to <see cref="RenderContext.DrawTextureInternal"/>. 
    /// </summary>
    public sealed class TwoDimensionalTexture :
        ITwoDimensionalTexture,
        INativeTextureProvider<Texture2D> {
        /// <summary>The underlying MonoGame texture. </summary>
        private readonly Texture2D _texture;
        
        /// <summary>
        /// Returns the native MonoGame texture for integrations implemented
        /// inside this adapter assembly.
        /// </summary>
        public Texture2D GetNativeTexture() {
            return _texture;
        }
        
        /// <inheritdoc/>
        public int Width { get { return _texture.Width; } }
        
        /// <inheritdoc/>
        public int Height { get { return _texture.Height; } }
        
        /// <summary>Initializes a new instance of the <see cref="TwoDimensionalTexture"/> class. </summary>
        /// <param name="texture">The MonoGame <see cref="Texture2D"/> to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="texture"/> is null.</exception>
        public TwoDimensionalTexture(Texture2D texture) {
            if (texture == null) { throw new ArgumentNullException(nameof(texture)); }
            _texture = texture;
        }
        
        /// <inheritdoc />
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
            
            // Create a visitor that captures the drawing parameters
            var visitor = new TextureVisitor(
                _texture, position, rectangleSource, color, rotation, origin, scale, effectsSprite, depthLayer
            );
            contextRender.Accept(visitor);
        }
        
        private sealed class TextureVisitor : IVisitor {
            private readonly Texture2D _texture;
            private readonly TwoDimensionalVector _position;
            private readonly OptionalValue<Rectangle> _rectangleSource;
            private readonly Color _color;
            private readonly float _rotation;
            private readonly TwoDimensionalVector _origin;
            private readonly TwoDimensionalVector _scale;
            private readonly MonoGameLibrary.Extensions.Graphics.SpriteEffects _effectsSprite;
            private readonly float _depthLayer;
            
            public TextureVisitor(
                Texture2D texture,
                TwoDimensionalVector position,
                OptionalValue<Rectangle> rectangleSource,
                Color color,
                float rotation,
                TwoDimensionalVector origin,
                TwoDimensionalVector scale,
                MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
                float depthLayer
            ) {
                _texture = texture;
                _position = position;
                _rectangleSource = rectangleSource;
                _color = color;
                _rotation = rotation;
                _origin = origin;
                _scale = scale;
                _effectsSprite = effectsSprite;
                _depthLayer = depthLayer;
            }
            
            /// <summary>
            /// Performs the drawing by pattern-matching the context to <see cref="RenderContext"/>.
            /// </summary>
            public void Visit(IRenderContext contextRender) {
                if (contextRender is RenderContext contextRenderTyped) {
                    contextRenderTyped.DrawTextureInternal(
                        _texture, 
                        _position, 
                        _rectangleSource, 
                        _color, 
                        _rotation, 
                        _origin, 
                        _scale, 
                        _effectsSprite, 
                        _depthLayer
                    );
                } else {
                    throw new NotSupportedException(
                        $"The asset type '{typeof(TwoDimensionalTexture).FullName}' only supports " +
                        $"the MonoGame render context. Received: {contextRender.GetType().FullName}"
                    );
                }
            }
        }
    }
}
