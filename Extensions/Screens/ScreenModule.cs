using System;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>Integrates screen management with the host lifecycle.</summary>
    [ModuleRegistration(-100)]
    public sealed class ScreenModule : IModule, IUpdateable, IDrawable, IDisposable {
        private IScreenService _service;
        private readonly int _order;
        private bool _flagEnabled = true;
        private bool _flagVisible = true;
        
        public ScreenModule(int order = 0) {
            _order = order;
        }
        
        public int Order { get { return _order; } }
        public bool Enabled { get { return _flagEnabled; } set { _flagEnabled = value; } }
        public bool Visible { get { return _flagVisible; } set { _flagVisible = value; } }
        
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            _service = new ScreenService();
            builder.RegisterService<IScreenService>(_service);
            builder.AddModule(this);
        }
        
        public void Update(FrameTime timeFrame) {
            if (_flagEnabled && _service != null) {
                _service.Update(timeFrame);
            }
        }
        
        public void Draw(FrameTime timeFrame) {
            if (_flagVisible && _service != null) {
                _service.Draw(timeFrame);
            }
        }
        
        public void Dispose() {
            if (_service != null) {
                _service.Dispose();
                _service = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
