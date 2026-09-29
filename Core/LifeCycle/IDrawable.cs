using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Core.Lifecycle {
    /// <summary>
    /// Implemented by modules that need to be drawn every frame.
    /// </summary>
    public interface IDrawable {
        /// <summary>
        /// Called once per frame to draw the module.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Draw(FrameTime timeFrame);
        
        /// <summary>
        /// Gets the draw order. Lower values draw first.
        /// </summary>
        int Order { get; }
        
        /// <summary>
        /// Gets or sets whether the module is visible. If <c>false</c>, <see cref="Draw"/> is skipped.
        /// The host only reads this flag; modules and the game layer are expected to change it at runtime
        /// (for example during screen transitions), so the contract exposes a setter.
        /// </summary>
        bool Visible { get; set; }
    }
}