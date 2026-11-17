using System;

namespace MonoGameLibrary.Adapters.Godot.Lifecycle {
    /// <summary>
    /// Platform bootstrap settings for a Godot application. The settings keep the
    /// window bootstrap surface in one place so a composition root can be written once
    /// across backends. Settings that Godot owns natively (vertical sync, fixed time step,
    /// content root) are configured through project settings or the content module
    /// instead of this type.
    /// </summary>
    public sealed class GameApplicationOptions {
        /// <summary>Gets or sets the window title. Must not be empty.</summary>
        public string Title { get; set; } = "MonoGameLibrary Application";
        /// <summary>Gets or sets the window width in pixels. Must be positive.</summary>
        public int Width { get; set; } = 1280;
        /// <summary>Gets or sets the window height in pixels. Must be positive.</summary>
        public int Height { get; set; } = 720;
        /// <summary>Gets or sets whether the application starts full screen.</summary>
        public bool IsFullScreen { get; set; }
        /// <summary>Gets or sets whether the mouse pointer is shown.</summary>
        public bool IsMouseVisible { get; set; } = true;
        
        internal void Validate() {
            if (string.IsNullOrWhiteSpace(Title)) {
                throw new ArgumentException("The window title cannot be empty.", nameof(Title));
            }
            if (Width <= 0) {
                throw new ArgumentOutOfRangeException(nameof(Width));
            }
            if (Height <= 0) {
                throw new ArgumentOutOfRangeException(nameof(Height));
            }
        }
    }
}
