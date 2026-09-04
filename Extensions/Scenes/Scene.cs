using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <inheritdoc />
    public abstract class Scene : IScene {
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public int Order { get; }
        
        /// <inheritdoc />
        public bool Enabled { get; set; } = true;
        
        /// <inheritdoc />
        public bool Visible { get; set; } = true;
        
        /// <summary>
        /// Delegate for actual content loading. Set by the game layer. 
        /// Called inside <see cref="LoadContent"/>.
        /// </summary>
        public Action LoadContentAction { get; set; }
        
        /// <summary>
        /// Creates a new scene. 
        /// </summary>
        /// <param name="order">The lifecycle execution order of the scene. Lower values execute earlier. </param>
        protected Scene(int order = 0) {
            Order = order;
        }
        
        /// <inheritdoc />
        public virtual void LoadContent() {
            if (LoadContentAction != null) {
                LoadContentAction.Invoke();
            }
        }
        
        /// <inheritdoc />
        public virtual void Initialize() {
        }
        
        /// <inheritdoc />
        /// <param name="timeFrame">Timing information for the current frame. </param>
        public virtual void Update(FrameTime timeFrame) {
        }

        /// <inheritdoc />
        public virtual void Draw(FrameTime timeFrame) {
        }
        
        /// <summary>
        /// Override to release managed resources. 
        /// </summary>
        /// <param name="flagDisposing">True if called from Dispose; false if from finalizer. </param>
        protected virtual void Dispose(bool flagDisposing) {
            if (_flagDisposed) { return; }
            if (flagDisposing) {
                // Ensure that all scene-specific resources are unloaded
            }
            
            _flagDisposed = true;
        }
        
        /// <summary>
        /// Disposes the scene and releases resources. 
        /// </summary>
        public virtual void Dispose() {
            if (_flagDisposed) {
                return;
            }
            
            Dispose(true);
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
