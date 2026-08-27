using System;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>
    /// Module that registers the scene management service.
    /// Implements <see cref="IModule"/> for automatic discovery and forwards
    /// lifecycle calls to <see cref="ISceneService"/>.
    /// </summary>
    public sealed class SceneModule : IModule, IUpdateable, IDrawable {
        private ISceneService _service;
        private readonly int _order;
        private bool _flagEnabled = true;
        private bool _flagVisible = true;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneModule"/> class. 
        /// </summary>
        /// <param name="order">Execution order for the module (default 0).</param>
        public SceneModule(int order = 0) {
            _order = order;
        }
        
        /// <inheritdoc />
        public int Order { get { return _order; } }
        
        /// <inheritdoc />
        public bool Enabled {
            get { return _flagEnabled; }
            set { _flagEnabled = value; }
        }
        
        /// <inheritdoc />
        public bool Visible {
            get { return _flagVisible; }
            set { _flagVisible = value; }
        }
        
        /// <inheritdoc />
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            _service = new SceneService();
            builder.RegisterService<ISceneService>(_service);
            builder.AddModule(this);
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            if (!_flagEnabled || _service == null) {
                return;
            }
            
            _service.Update(timeFrame);
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            if (!_flagVisible || _service == null) {
                return;
            }
            
            _service.Draw(timeFrame);
        }
    }
}