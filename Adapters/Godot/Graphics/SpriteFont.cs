using System;
using Godot;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Godot.Graphics {
    /// <summary>
    /// Godot adapter for <see cref="ISpriteFont"/>. 
    /// Wraps a Godot <see cref="Font"/> and implements 
    /// <see cref="ISpriteFont.DrawInto"/> by creating a visitor that delegates to 
    /// <see cref="RenderContext.DrawStringInternal"/>. 
    /// Godot fonts are scalable, so the wrapper anchors its metrics on the intrinsic font
    /// size (<see cref="FontFile.FixedSize"/> when the font declares one, otherwise the
    /// Godot default of 16) and lets the per-call scale flow through the draw transform
    /// without changing the font size.
    /// </summary>
    internal sealed class SpriteFont : ISpriteFont {
        /// <summary>The font size assumed when a font declares no fixed size. </summary>
        internal const int DefaultFontSize = 16;
        
        /// <summary>The underlying Godot font. </summary>
        private readonly Font _font;
        
        /// <inheritdoc />
        public float LineSpacing { get { return _font.GetHeight(ResolveIntrinsicFontSize(_font)); } }
        
        /// <inheritdoc />
        /// <remarks>
        /// Godot fonts own their character advance completely; the <see cref="Font"/> API
        /// exposes no inter-character spacing knob, so this always reports zero.
        /// </remarks>
        public float Spacing { get { return 0f; } }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteFont"/> class. 
        /// </summary>
        /// <param name="font">The Godot <see cref="Font"/> to wrap. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="font"/> is null. </exception>
        public SpriteFont(Font font) {
            if (font == null) { throw new ArgumentNullException(nameof(font)); }
            _font = font;
        }
        
        /// <summary>
        /// Resolves the intrinsic size of a Godot font: its fixed size when it declares one,
        /// otherwise the Godot default font size. The size anchors all unscaled metrics.
        /// </summary>
        /// <param name="font">The font to inspect.</param>
        /// <returns>The intrinsic font size in pixels.</returns>
        internal static int ResolveIntrinsicFontSize(Font font) {
            if (font == null) { throw new ArgumentNullException(nameof(font)); }
            FontFile fontFile = font as FontFile;
            if (fontFile != null && fontFile.FixedSize > 0) {
                return fontFile.FixedSize;
            }
            return DefaultFontSize;
        }
        
        /// <inheritdoc/>
        public TwoDimensionalVector MeasureString(string text) {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            Vector2 size = _font.GetStringSize(
                text, 
                HorizontalAlignment.Left, 
                -1f, 
                ResolveIntrinsicFontSize(_font), 
                TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, 
                TextServer.Direction.Auto, 
                TextServer.Orientation.Horizontal
            );
            return new TwoDimensionalVector(size.X, size.Y);
        }
        
        /// <inheritdoc />
        public void DrawInto(
            IRenderContext contextRender,
            string text,
            TwoDimensionalVector vectorPosition,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector vectorOrigin,
            float scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            if (contextRender == null) {
                throw new ArgumentNullException(nameof(contextRender));
            }
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            
            var visitor = new FontVisitor(
                _font, text, vectorPosition, color, rotation, vectorOrigin, scale, effectsSprite, depthLayer
            );
            contextRender.Accept(visitor);
        }
        
        private sealed class FontVisitor : IVisitor {
            private readonly Font _font;
            private readonly string _text;
            private readonly TwoDimensionalVector _vectorPosition;
            private readonly MonoGameLibrary.Core.Primitives.Color _color;
            private readonly float _rotation;
            private readonly TwoDimensionalVector _vectorOrigin;
            private readonly float _scale;
            private readonly MonoGameLibrary.Extensions.Graphics.SpriteEffects _effectsSprite;
            private readonly float _depthLayer;
            
            public FontVisitor(
                Font font,
                string text,
                TwoDimensionalVector vectorPosition,
                MonoGameLibrary.Core.Primitives.Color color,
                float rotation,
                TwoDimensionalVector vectorOrigin,
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
                _vectorPosition = vectorPosition;
                _color = color;
                _rotation = rotation;
                _vectorOrigin = vectorOrigin;
                _scale = scale;
                _effectsSprite = effectsSprite;
                _depthLayer = depthLayer;
            }
            
            public void Visit(IRenderContext contextRender) {
                if (contextRender is RenderContext contextRenderTyped) {
                    contextRenderTyped.DrawStringInternal(
                        _font, 
                        _text, 
                        _vectorPosition,
                        _color, 
                        _rotation,
                        _vectorOrigin,
                        _scale, 
                        _effectsSprite, 
                        _depthLayer
                    );
                } else {
                    throw new NotSupportedException(
                        $"The asset type '{typeof(SpriteFont).FullName}' only supports " +
                        $"the Godot render context. Received: {contextRender.GetType().FullName}"
                    );
                }
            }
        }
    }
}
