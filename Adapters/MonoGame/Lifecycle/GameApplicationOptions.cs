using System;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>Platform bootstrap settings for a MonoGame application.</summary>
    public sealed class GameApplicationOptions {
        /// <summary>Gets or sets the window title. Must not be empty.</summary>
        public string Title { get; set; } = "MonoGameLibrary Application";
        /// <summary>Gets or sets the back-buffer width in pixels. Must be positive.</summary>
        public int Width { get; set; } = 1280;
        /// <summary>Gets or sets the back-buffer height in pixels. Must be positive.</summary>
        public int Height { get; set; } = 720;
        /// <summary>Gets or sets the directory assets are loaded from, relative to the application. Must not be empty.</summary>
        public string ContentRootDirectory { get; set; } = "Content";
        /// <summary>Gets or sets whether the mouse pointer is shown.</summary>
        public bool IsMouseVisible { get; set; } = true;
        /// <summary>Gets or sets whether the application starts full screen.</summary>
        public bool IsFullScreen { get; set; }
        /// <summary>Gets or sets whether presentation is synchronised with the display's refresh.</summary>
        public bool IsVerticalSyncEnabled { get; set; } = true;
        /// <summary>Gets or sets whether the frame time step is fixed rather than measured.</summary>
        public bool IsFixedTimeStep { get; set; } = true;
        
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
            if (string.IsNullOrWhiteSpace(ContentRootDirectory)) {
                throw new ArgumentException("The content root cannot be empty.", nameof(ContentRootDirectory));
            }
        }
    }
}
