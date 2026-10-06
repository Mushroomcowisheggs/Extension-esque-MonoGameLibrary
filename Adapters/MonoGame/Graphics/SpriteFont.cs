using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame adapter for <see cref="ISpriteFont"/>. 
    /// Wraps a <see cref="Microsoft.Xna.Framework.Graphics.SpriteFont"/> and implements 
    /// <see cref="ISpriteFont.DrawInto"/> by creating a visitor that delegates to 
    /// <see cref="RenderContext.DrawStringInternal"/>. 
    /// </summary>
    internal sealed class SpriteFont : ISpriteFont {
        /// <summary>The underlying MonoGame sprite font. </summary>
        private readonly Microsoft.Xna.Framework.Graphics.SpriteFont _font;
        
        /// <inheritdoc />
        public float LineSpacing { get { return _font.LineSpacing; } }
        
        /// <inheritdoc />
        public float Spacing { get { return _font.Spacing; } }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteFont"/> class. 
        /// </summary>
        /// <param name="font">The MonoGame <see cref="Microsoft.Xna.Framework.Graphics.SpriteFont"/> to wrap. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="font"/> is null. </exception>
        public SpriteFont(Microsoft.Xna.Framework.Graphics.SpriteFont font) {
            if (font == null) { throw new ArgumentNullException(nameof(font)); }
            _font = font;
        }
        
        /// <inheritdoc/>
        public TwoDimensionalVector MeasureString(string text) {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            Vector2 size = _font.MeasureString(text);
            return new TwoDimensionalVector(size.X, size.Y);
        }
        
        /// <inheritdoc />
        public void DrawInto(
            IRenderContext contextRender,
            string text,
            TwoDimensionalVector position,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            float scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            if (contextRender == null) {
                throw new ArgumentNullException(nameof(contextRender));
            }
            
            var visitor = new FontVisitor(
                _font, text, position, color, rotation, origin, scale, effectsSprite, depthLayer
            );
            contextRender.Accept(visitor);
        }
        
        private sealed class FontVisitor : IVisitor {
            private readonly Microsoft.Xna.Framework.Graphics.SpriteFont _font;
            private readonly string _text;
            private readonly TwoDimensionalVector _position;
            private readonly MonoGameLibrary.Core.Primitives.Color _color;
            private readonly float _rotation;
            private readonly TwoDimensionalVector _origin;
            private readonly float _scale;
            private readonly MonoGameLibrary.Extensions.Graphics.SpriteEffects _effectsSprite;
            private readonly float _depthLayer;
            
            public FontVisitor(
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
                if (font == null) {
                    throw new ArgumentNullException(nameof(font));
                }
                if (text == null) {
                    throw new ArgumentNullException(nameof(text));
                }
                _font = font;
                _text = text;
                _position = position;
                _color = color;
                _rotation = rotation;
                _origin = origin;
                _scale = scale;
                _effectsSprite = effectsSprite;
                _depthLayer = depthLayer;
            }
            
            public void Visit(IRenderContext contextRender) {
                if (contextRender is RenderContext contextRenderTyped) {
                    contextRenderTyped.DrawStringInternal(
                        _font, 
                        _text, 
                        _position, 
                        _color, 
                        _rotation,
                        _origin, 
                        _scale, 
                        _effectsSprite, 
                        _depthLayer
                    );
                } else {
                    throw new NotSupportedException(
                        $"The asset type '{typeof(SpriteFont).FullName}' only supports " +
                        $"the MonoGame render context. Received: {contextRender.GetType().FullName}"
                    );
                }
            }
        }
    }
}