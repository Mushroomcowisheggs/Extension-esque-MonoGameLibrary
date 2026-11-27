using System;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// Default <see cref="MonoGameLibrary.Extensions.UserInterface.IUiRenderer"/> over
    /// the graphics feature: a one-pixel texture for filled rectangles and a sprite font
    /// for text. This adapter copy is the single place the user-interface seam names
    /// graphics types; the widgets see only
    /// <see cref="MonoGameLibrary.Extensions.UserInterface.IUiRenderer"/>.
    /// </summary>
    public sealed class UiRenderer : MonoGameLibrary.Extensions.UserInterface.IUiRenderer, IDisposable {
        private readonly IRenderContext _contextRender;
        private readonly ISpriteFont _font;
        private ITwoDimensionalTexture _texturePixel;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes the renderer.
        /// </summary>
        /// <param name="contextRender">The render context to draw into.</param>
        /// <param name="font">The font used for text.</param>
        /// <param name="factoryTexture">The factory creating the rectangle fill texture.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public UiRenderer(
            IRenderContext contextRender,
            ISpriteFont font,
            ITextureFactory factoryTexture
        ) {
            if (contextRender == null) {
                throw new ArgumentNullException(nameof(contextRender));
            }
            if (font == null) {
                throw new ArgumentNullException(nameof(font));
            }
            if (factoryTexture == null) {
                throw new ArgumentNullException(nameof(factoryTexture));
            }
            _contextRender = contextRender;
            _font = font;
            _texturePixel = factoryTexture.CreateFromPixels(
                1, 1, new Color[] { Color.White }
            );
        }
        
        /// <inheritdoc />
        public void Begin() {
            _contextRender.Begin();
        }
        
        /// <inheritdoc />
        public void End() {
            _contextRender.End();
        }
        
        /// <inheritdoc />
        public TwoDimensionalVector MeasureText(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            return _font.MeasureString(text);
        }
        
        /// <inheritdoc />
        public void DrawRectangle(Rectangle bounds, Color color) {
            _contextRender.DrawTextureRegion(_texturePixel, bounds, default, color);
        }
        
        /// <inheritdoc />
        public void DrawText(string text, TwoDimensionalVector position, Color color) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            _font.DrawInto(
                _contextRender,
                text,
                position,
                color,
                0f,
                new TwoDimensionalVector(0f, 0f),
                1f,
                SpriteEffects.None,
                0f
            );
        }
        
        /// <summary>
        /// Releases the rectangle fill texture. The render context and font are owned by
        /// the composition root and are not released here.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            if (_texturePixel != null) {
                _texturePixel.Dispose();
                _texturePixel = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
