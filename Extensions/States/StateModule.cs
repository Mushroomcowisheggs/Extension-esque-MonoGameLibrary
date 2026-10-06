using System;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.States {
    /// <summary>
    /// Wraps an <see cref="IStateService"/> as a module that can be registered 
    /// with <see cref="GameHost"/>, implementing <see cref="IUpdateable"/>. 
    /// </summary>
    public sealed class StateModule : IUpdateable {
        private readonly IStateService _service;
        private readonly int _order;
        private bool _flagEnabled = true;
        
        /// <summary>Initializes the module around an existing state service.</summary>
        /// <param name="service">The state service to drive.</param>
        /// <param name="order">The value reported by <see cref="Order"/>.</param>
        public StateModule(IStateService service, int order = 0) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            _service = service;
            _order = order;
        }
        
        /// <summary>Gets the position this module occupies in the host's schedule.</summary>
        public int Order { get { return _order; } }
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