using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;
using IUpdateable = MonoGameLibrary.Core.Lifecycle.IUpdateable;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// Platform-specific module that registers the MonoGame input services. 
    /// Implements <see cref="IModule"/> for automatic discovery and <see cref="IUpdateable"/>
    /// to forward per-frame updates to the keyboard, gamepad, pointer and text services. 
    /// All three services are sampled at the same instant so that a frame never mixes
    /// pointer state from one instant with keyboard state from another.
    /// </summary>
    [ModuleRegistration(-430)]
    public sealed class InputModule : IModule, IUpdateable, IDisposable {
        private IInputService _serviceInput;
        private IPointerInputService _servicePointer;
        private ITextInputService _serviceText;
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
            Game game = builder.GetService<Game>();
            if (game == null) {
                throw new InvalidOperationException("Game service is not registered.");
            }
            
            _serviceInput = new InputService(game);
            _servicePointer = new PointerInputService(game);
            _serviceText = new TextInputService(game);
            builder.RegisterService<IInputService>(_serviceInput);
            builder.RegisterService<IPointerInputService>(_servicePointer);
            builder.RegisterService<ITextInputService>(_serviceText);
            // The single key translation table every MonoGame-flavoured adapter resolves.
            builder.RegisterService<INativeKeyProvider<Keys>>(new MonoGameKeyCodeProvider());
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
            _servicePointer.Update(timeFrame);
            _serviceText.Update(timeFrame);
        }
        
        /// <summary>
        /// Disposes the registered input services. 
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            IDisposable disposableText = _serviceText as IDisposable;
            if (disposableText != null) {
                disposableText.Dispose();
            }
            _serviceText = null;
            IDisposable disposablePointer = _servicePointer as IDisposable;
            if (disposablePointer != null) {
                disposablePointer.Dispose();
            }
            _servicePointer = null;
            IDisposable disposableInput = _serviceInput as IDisposable;
            if (disposableInput != null) {
                disposableInput.Dispose();
            }
            _serviceInput = null;
            GC.SuppressFinalize(this);
        }
    }
}