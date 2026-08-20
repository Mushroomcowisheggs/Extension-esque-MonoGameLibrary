using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// Platform-specific module that registers the MonoGame input service. 
    /// Implements <see cref="IModule"/> for automatic discovery and <see cref="IUpdateable"/>
    /// to forward per-frame updates to the input service. 
    /// </summary>
    public sealed class InputModule : IModule, IUpdateable, IDisposable {
        private IInputService _serviceInput;
        private readonly object _lock = new object();
        private bool _flagEnabled = true;
        private bool _flagDisposed = false;
        
        /// <summary>
        /// Gets the update order. Input should update before most systems, so default is -64.
        /// </summary>
        public int Order { get; } = -64;
        
        /// <summary>
        /// Gets or sets whether the module updates.
        /// </summary>
        public bool Enabled {
            get { lock (_lock) { return _flagEnabled; } }
            set { lock (_lock) _flagEnabled = value; }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InputModule"/> class. 
        /// </summary>
        public InputModule() {
        }
        
        /// <inheritdoc />
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            _serviceInput = new InputService();
            builder.RegisterService<IInputService>(_serviceInput);
            builder.AddModule(this);
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            bool flagShouldUpdate;
            lock (_lock) {
                flagShouldUpdate = _flagEnabled && !_flagDisposed && _serviceInput != null;
            }
            if (!flagShouldUpdate) {
                return;
            }
            
            _serviceInput.Update(timeFrame);
        }
        
        /// <summary>
        /// Disposes the module (no unmanaged resources). 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}