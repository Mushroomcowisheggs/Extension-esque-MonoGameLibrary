using System;
using System.Threading;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>Integrates screen management with the host lifecycle.</summary>
    // Extensions band (-199..-100): unique inside the band, so the loader's tie-break never decides
    // this module's load position.
    [ModuleRegistration(-100)]
    public sealed class ScreenModule : IModule, IUpdateable, IDrawable, IDisposable {
        // Every mutable field is volatile: the gates are read on the frame thread while the application
        // writes them, and the service reference is published by composition and then read per frame.
        private volatile IScreenService _service;
        private readonly int _order;
        private volatile bool _flagEnabled = true;
        private volatile bool _flagVisible = true;
        private int _markDisposed;
        
        /// <summary>Initializes the module with the schedule position it will occupy.</summary>
        /// <param name="order">The order supplied to <c>ScheduleOptions</c> at admission.</param>
        public ScreenModule(int order = 0) {
            _order = order;
        }
        
        /// <summary>Gets or sets whether <see cref="Update"/> forwards to the screen service.</summary>
        public bool Enabled { get { return _flagEnabled; } set { _flagEnabled = value; } }
        /// <summary>Gets or sets whether <see cref="Draw"/> forwards to the screen service.</summary>
        public bool Visible { get { return _flagVisible; } set { _flagVisible = value; } }
        
        /// <summary>Registers the screen service and adds this module to the host.</summary>
        /// <param name="builder">The builder performing the composition fold.</param>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            // A screen whose cleanup fails is reported through the composition's logger; without one the
            // service keeps the silent default rather than dropping the report.
            ILogger logger;
            if (builder.TryGetService<ILogger>(out logger)) {
                _service = new ScreenService(new Optional<ILogger>(logger));
            } else {
                _service = new ScreenService();
            }
            builder.RegisterService<IScreenService>(_service);
            builder.AddModule(this, new ScheduleOptions(orderUpdate: _order, orderDraw: _order));
        }
        
        /// <summary>Forwards the frame to the screen service when the module is enabled.</summary>
        /// <param name="timeFrame">The frame time to pass on.</param>
        public void Update(FrameTime timeFrame) {
            if (_flagEnabled && _service != null) {
                _service.Update(timeFrame);
            }
        }
        
        /// <summary>Forwards the frame to the screen service when the module is visible.</summary>
        /// <param name="timeFrame">The frame time to pass on.</param>
        public void Draw(FrameTime timeFrame) {
            if (_flagVisible && _service != null) {
                _service.Draw(timeFrame);
            }
        }
        
        /// <summary>Releases the screen service. Safe to call more than once, and only one caller releases it.</summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _markDisposed, 1) != 0) {
                return;
            }
            IScreenService service = _service;
            _service = null;
            if (service != null) {
                service.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
