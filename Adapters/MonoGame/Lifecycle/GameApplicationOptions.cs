using System;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>Platform bootstrap settings for a MonoGame application.</summary>
    public sealed class GameApplicationOptions {
        public string Title { get; set; } = "MonoGameLibrary Application";
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public string ContentRootDirectory { get; set; } = "Content";
        public bool IsMouseVisible { get; set; } = true;
        public bool IsFullScreen { get; set; }
        public bool IsVerticalSyncEnabled { get; set; } = true;
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
