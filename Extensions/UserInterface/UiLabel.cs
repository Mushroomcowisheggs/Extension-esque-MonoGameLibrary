using System;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// A line of text drawn at the top-left corner of its bounds.
    /// </summary>
    public sealed class UiLabel : UiElement {
        private string _text;
        
        /// <summary>Gets or sets the text color.</summary>
        public Color TextColor { get; set; }
        
        /// <summary>
        /// Initializes a label.
        /// </summary>
        /// <param name="text">The text to draw.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        public UiLabel(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            _text = text;
            TextColor = Color.White;
        }
        
        /// <summary>Gets or sets the text to draw.</summary>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        public string Text {
            get { return _text; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                _text = value;
            }
        }
        
        /// <inheritdoc />
        public override void Draw(IUiRenderer renderer, float zoom) {
            Rectangle rectangleBounds = GetScaledBounds(zoom);
            renderer.DrawText(
                _text,
                new TwoDimensionalVector(rectangleBounds.X, rectangleBounds.Y),
                TextColor
            );
        }
    }
}
