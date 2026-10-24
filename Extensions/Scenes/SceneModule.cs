using System;
using System.Threading;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>Integrates scene management with the host lifecycle.</summary>
    // Extensions band (-199..-100), unique in the band: registered after the Adapters that provide the
    // services this module consumes.
    [ModuleRegistration(-199)]
    public sealed class SceneModule : IModule, IUpdateable, IDrawable, IDisposable {
        // Every mutable field is volatile: the gates are read on the frame thread while the application
        // writes them, and the service reference is published by composition and then read per frame.
        private volatile ISceneService _service;
        private readonly int _order;
        private volatile bool _flagEnabled = true;
        private volatile bool _flagVisible = true;
        private int _markDisposed;
        
        /// <summary>Initializes the module with the schedule position it will occupy.</summary>
        /// <param name="order">The value reported by <see cref="Order"/>.</param>
        public SceneModule(int order = 0) {
            _order = order;
        }
        
        /// <summary>Gets the position this module occupies in the host's schedule.</summary>
        public int Order { get { return _order; } }
        /// <summary>Gets or sets whether <see cref="Update"/> forwards to the scene service.</summary>
        public bool Enabled { get { return _flagEnabled; } set { _flagEnabled = value; } }
        /// <summary>Gets or sets whether <see cref="Draw"/> forwards to the scene service.</summary>
        public bool Visible { get { return _flagVisible; } set { _flagVisible = value; } }
        
        /// <summary>Registers the scene service and adds this module to the host.</summary>
        /// <param name="builder">The builder performing the composition fold.</param>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            _service = new SceneService();
            builder.RegisterService<ISceneService>(_service);
            builder.AddModule(this);
        }
        
        /// <summary>Forwards the frame to the scene service when the module is enabled.</summary>
        /// <param name="timeFrame">The frame time to pass on.</param>
        public void Update(FrameTime timeFrame) {
            if (_flagEnabled && _service != null) {
                _service.Update(timeFrame);
            }
        }
        
        /// <summary>Forwards the frame to the scene service when the module is visible.</summary>
        /// <param name="timeFrame">The frame time to pass on.</param>
        public void Draw(FrameTime timeFrame) {
            if (_flagVisible && _service != null) {
                _service.Draw(timeFrame);
            }
        }
        
        /// <summary>Releases the scene service. Safe to call more than once, and only one caller releases it.</summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _markDisposed, 1) != 0) {
                return;
            }
            ISceneService service = _service;
            _service = null;
            if (service != null) {
                service.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
