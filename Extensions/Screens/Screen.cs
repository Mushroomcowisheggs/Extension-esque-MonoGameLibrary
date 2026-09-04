using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Screens {
    /// <inheritdoc />
    public abstract class Screen : IScreen {
        private bool _flagDisposed;
        
        /// <summary>
        /// Delegate for actual content loading. Set by the game layer. 
        /// Called inside <see cref="LoadContent"/>.
        /// </summary>
        public Action LoadContentAction { get; set; }
        
        /// <summary>
        /// Delegate for handling input. Set by the game layer. 
        /// Receives the current frame time. 
        /// </summary>
        public Action<FrameTime> InputAction { get; set; }
        
        /// <inheritdoc />
        public event EventHandler<ScreenChangeEventArguments> ScreenChangeRequested;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Screen"/> class.
        /// </summary>
        protected Screen(
        ) {
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
        
        protected void RequestPush(Screen screenNew) {
            if (ScreenChangeRequested != null) {
                ScreenChangeRequested(this, new ScreenChangeEventArguments(ScreenChangeType.Push, screenNew));
            }
        }
        
        protected void RequestPop() {
            if (ScreenChangeRequested != null) {
                ScreenChangeRequested(this, new ScreenChangeEventArguments(ScreenChangeType.Pop, null));
            }
        }
        
        protected void RequestChange(Screen screenNew) {
            if (ScreenChangeRequested != null) {
                ScreenChangeRequested(this, new ScreenChangeEventArguments(ScreenChangeType.Change, screenNew));
            }
        }
        
        /// <inheritdoc />
        public virtual bool IsTransparent { get { return false; } }
        /// <inheritdoc />
        public virtual bool IsBlocking { get { return false; } }
        
        /// <inheritdoc />
        public virtual void Enter() { }
        /// <inheritdoc />
        public virtual void Exit() { }
        
        /// <inheritdoc />
        public abstract void Update(FrameTime timeFrame);
        
        /// <inheritdoc />
        public virtual void Draw(FrameTime timeFrame) {
        }
        
        /// <summary>
        /// Override to release managed resources. 
        /// </summary>
        /// <param name="flagDisposing">True if called from Dispose; false if from finalizer.</param>
        protected virtual void Dispose(bool flagDisposing) {
            if (_flagDisposed) { return; }
            if (flagDisposing) {
                // Ensure that all screen-specific resources are unloaded
            }
            
            _flagDisposed = true;
        }
        
        /// <summary>
        /// Disposes the screen and releases resources. 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            
            Dispose(true);
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
