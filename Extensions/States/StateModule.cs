using System;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.States {
    /// <summary>
    /// Wraps an <see cref="IStateService"/> as a module that can be registered
    /// with <see cref="GameHost"/>, implementing <see cref="IUpdateable"/>.
    /// </summary>
    [ModuleRegistration(-195)]
    public sealed class StateModule : IModule, IUpdateable {
        private readonly IStateService _service;
        private readonly int _order;
        // Read on the frame thread while the application writes it; a volatile gate keeps the write
        // visible without taking a lock on the per-frame path.
        private volatile bool _flagEnabled = true;

        /// <summary>Initializes the module around a state service the module creates and owns.</summary>
        /// <param name="order">The update and draw order of the module.</param>
        public StateModule(int order = 0) {
            _service = new StateService();
            _order = order;
        }

        /// <summary>Initializes the module around an existing state service.</summary>
        /// <param name="service">The state service to drive.</param>
        /// <param name="order">The update and draw order of the module.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is null.</exception>
        public StateModule(IStateService service, int order = 0) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            _service = service;
            _order = order;
        }

        /// <summary>Registers the state service and adds the module to the host.</summary>
        /// <param name="builder">The game builder.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            builder.RegisterService<IStateService>(_service);
            builder.AddModule(this, new ScheduleOptions(orderUpdate: _order, orderDraw: _order));
        }

        /// <summary>Gets or sets whether <see cref="Update"/> forwards to the state service.</summary>
        public bool Enabled {
            get { return _flagEnabled; }
            set { _flagEnabled = value; }
        }

        /// <summary>Forwards the frame to the state service when the module is enabled.</summary>
        /// <param name="timeFrame">The frame time to pass on.</param>
        public void Update(FrameTime timeFrame) {
            if (!_flagEnabled) { return; }
            _service.Update(timeFrame);
        }
    }
}
