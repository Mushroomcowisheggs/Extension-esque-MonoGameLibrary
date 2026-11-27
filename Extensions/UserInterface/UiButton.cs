using System;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// A push button: a background rectangle with centered text that raises
    /// <see cref="Clicked"/> when activated through the owning <see cref="UiService"/>'s
    /// navigation input (the confirm key on the selected button).
    /// </summary>
    public sealed class UiButton : UiElement {
        private string _text;
        
        /// <summary>Raised when the button is activated.</summary>
        public event EventHandler Clicked;
        
        /// <summary>Gets or sets the background color.</summary>
        public Color BackgroundColor { get; set; }
        
        /// <summary>Gets or sets the background color while the button holds the selection.</summary>
        public Color SelectedBackgroundColor { get; set; }
        
        /// <summary>Gets or sets the text color.</summary>
        public Color TextColor { get; set; }
        
        /// <summary>
        /// Initializes a button.
        /// </summary>
        /// <param name="text">The text to draw centered on the button.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        public UiButton(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            _text = text;
            BackgroundColor = new Color(40, 40, 48, 220);
            SelectedBackgroundColor = new Color(70, 90, 140, 230);
            TextColor = Color.White;
        }
        
        /// <summary>Gets or sets the text drawn centered on the button.</summary>
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
        
        /// <summary>
        /// Activates the button: raises <see cref="Clicked"/> when the button is enabled
        /// and visible.
        /// </summary>
        public void Activate() {
            if (!Enabled || !Visible) {
                return;
            }
            EventHandler handler = Clicked;
            if (handler == null) {
                return;
            }
            handler(this, EventArgs.Empty);
        }
        
        /// <inheritdoc />
        public override void Draw(IUiRenderer renderer, float zoom) {
            Rectangle rectangleBounds = GetScaledBounds(zoom);
            Color colorBackground;
            if (FlagSelected) {
                colorBackground = SelectedBackgroundColor;
            } else {
                colorBackground = BackgroundColor;
            }
            renderer.DrawRectangle(rectangleBounds, colorBackground);
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
