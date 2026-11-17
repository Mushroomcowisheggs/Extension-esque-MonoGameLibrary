using System;
using GodotWindow = global::Godot.Window;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Window;

namespace MonoGameLibrary.Adapters.Godot.Window {
    /// <summary>
    /// Platform-specific module that registers the Godot window service.
    /// It updates after the input module so that a shortcut handled from input has already
    /// been observed, and before the modules that translate coordinates or drive the pointer.
    /// </summary>
    [ModuleRegistration(-320)]
    public sealed class WindowModule : IModule, MonoGameLibrary.Core.Lifecycle.IUpdateable, IDisposable {
        private WindowService _serviceWindow;
        private readonly object _lock = new object();
        private bool _flagEnabled = true;
        private bool _flagDisposed;
        
        /// <summary>
        /// Gets or sets whether the module updates.
        /// </summary>
        public bool Enabled {
            get { lock (_lock) { return _flagEnabled; } }
            set { lock (_lock) { _flagEnabled = value; } }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowModule"/> class.
        /// </summary>
        public WindowModule() {
        }
        
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the root window service is not registered.
        /// </exception>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            // Modules are created by the module loader through a parameterless constructor,
            // so the running game window is resolved from the composition root here. Reading
            // it through the platform container is the adapter exemption from the explicit
            // dependency rule; nothing above the adapter sees the native window type.
            GodotWindow window = builder.GetService<GodotWindow>();
            if (window == null) {
                throw new InvalidOperationException("Window service is not registered.");
            }
            
            _serviceWindow = new WindowService(window);
            builder.RegisterService<IWindowService>(_serviceWindow);
            builder.AddModule(this, new ScheduleOptions(orderUpdate: -63));
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            WindowService serviceWindow;
            lock (_lock) {
                if (!_flagEnabled || _flagDisposed) {
                    return;
                }
                serviceWindow = _serviceWindow;
            }
            if (serviceWindow == null) {
                return;
            }
            
            serviceWindow.Update(timeFrame);
        }
        
        /// <summary>
        /// Disposes the window service it created.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            if (_serviceWindow != null) {
                _serviceWindow.Dispose();
                _serviceWindow = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
