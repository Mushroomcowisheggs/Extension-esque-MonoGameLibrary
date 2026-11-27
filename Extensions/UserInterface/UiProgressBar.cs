using System;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// A horizontal progress bar: a background rectangle with a foreground rectangle
    /// scaled to the current value, and optional centered text.
    /// </summary>
    public sealed class UiProgressBar : UiElement {
        private float _value;
        private string _text;
        
        /// <summary>Gets or sets the background color.</summary>
        public Color BackgroundColor { get; set; }
        
        /// <summary>Gets or sets the foreground color.</summary>
        public Color ForegroundColor { get; set; }
        
        /// <summary>Gets or sets the text color.</summary>
        public Color TextColor { get; set; }
        
        /// <summary>
        /// Initializes a progress bar with empty text.
        /// </summary>
        public UiProgressBar() {
            _text = "";
            BackgroundColor = new Color(0, 0, 0, 160);
            ForegroundColor = new Color(80, 180, 80);
            TextColor = Color.White;
        }
        
        /// <summary>
        /// Gets or sets the progress value, clamped to 0.0-1.0.
        /// </summary>
        public float Value {
            get { return _value; }
            set {
                if (value < 0f) {
                    _value = 0f;
                    return;
                }
                if (value > 1f) {
                    _value = 1f;
                    return;
                }
                _value = value;
            }
        }
        
        /// <summary>Gets or sets the text drawn centered on the bar.</summary>
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
            renderer.DrawRectangle(rectangleBounds, BackgroundColor);
            int widthForeground = (int)(rectangleBounds.Width * _value);
            if (widthForeground > 0) {
                renderer.DrawRectangle(
                    new Rectangle(rectangleBounds.X, rectangleBounds.Y, widthForeground, rectangleBounds.Height),
                    ForegroundColor
                );
            }
            if (_text.Length > 0) {
                TwoDimensionalVector vectorTextSize = renderer.MeasureText(_text);
                renderer.DrawText(
                    _text,
                    new TwoDimensionalVector(
                        rectangleBounds.X + (rectangleBounds.Width - vectorTextSize.X) * 0.5f,
                        rectangleBounds.Y + (rectangleBounds.Height - vectorTextSize.Y) * 0.5f
                    ),
                    TextColor
                );
            }
        }
    }
}
