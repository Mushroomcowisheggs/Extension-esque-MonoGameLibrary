using System;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>Integrates scene management with the host lifecycle.</summary>
    [ModuleRegistration(-100)]
    public sealed class SceneModule : IModule, IUpdateable, IDrawable, IDisposable {
        private ISceneService _service;
        private readonly int _order;
        private bool _flagEnabled = true;
        private bool _flagVisible = true;
        
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
        
        /// <summary>Releases the scene service. Safe to call more than once.</summary>
        public void Dispose() {
            if (_service != null) {
                _service.Dispose();
                _service = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
