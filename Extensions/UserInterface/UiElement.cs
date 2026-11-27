using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Base class for the neutral user-interface widgets: a rectangle in render
    /// coordinates with visibility and enabled state. Widgets draw themselves through
    /// <see cref="IUiRenderer"/> and never name a rendering backend. Bounds are laid out
    /// in canvas coordinates and scaled by the zoom factor of the owning
    /// <see cref="UiService"/> when drawn.
    /// </summary>
    public abstract class UiElement {
        /// <summary>Gets or sets the element bounds in canvas coordinates.</summary>
        public Rectangle BoundsRectangle { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the element is drawn and updated.
        /// </summary>
        public bool Visible { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the element reacts to interaction.
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the element currently holds the
        /// navigation selection. Managed by the owning <see cref="UiService"/>.
        /// </summary>
        internal bool FlagSelected { get; set; }
        
        /// <summary>
        /// Initializes a visible and enabled element.
        /// </summary>
        protected UiElement() {
            Visible = true;
            Enabled = true;
        }
        
        /// <summary>Draws the element through the supplied renderer.</summary>
        /// <param name="renderer">The renderer to draw through.</param>
        /// <param name="zoom">The canvas zoom factor applied to the element bounds.</param>
        public abstract void Draw(IUiRenderer renderer, float zoom);
        
        /// <summary>Updates the element for one frame.</summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        public virtual void Update(FrameTime timeFrame) {
        }
        
        /// <summary>Returns the element bounds scaled by the zoom factor.</summary>
        /// <param name="zoom">The canvas zoom factor.</param>
        protected Rectangle GetScaledBounds(float zoom) {
            Rectangle rectangleBounds = BoundsRectangle;
            return new Rectangle(
                (int)(rectangleBounds.X * zoom),
                (int)(rectangleBounds.Y * zoom),
                (int)(rectangleBounds.Width * zoom),
                (int)(rectangleBounds.Height * zoom)
            );
        }
    }
}
