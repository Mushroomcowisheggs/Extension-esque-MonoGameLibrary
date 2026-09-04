using System;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Host-driven module that forwards update and draw calls to <see cref="IUserInterfaceService"/>.
    /// Contains no platform logic.
    /// </summary>
    public sealed class UserInterfaceModule : IUpdateable, IDrawable, IDisposable {
        private readonly IUserInterfaceService _serviceUserInterface;
        private readonly int _order;
        private bool _flagEnabled = true;
        private bool _flagVisible = true;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterfaceModule"/> class.
        /// </summary>
        /// <param name="serviceUserInterface">The UI service to forward calls to.</param>
        /// <param name="order">Execution order (default 0).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceUserInterface"/> is null.</exception>
        public UserInterfaceModule(IUserInterfaceService serviceUserInterface, int order = 100) {
            if (serviceUserInterface == null) {
                throw new ArgumentNullException(nameof(serviceUserInterface));
            }
            _serviceUserInterface = serviceUserInterface;
            _order = order;
        }
        
        /// <inheritdoc />
        public int Order {
            get { return _order; }
        }
        
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
        public void Update(FrameTime timeFrame) {
            if (!_flagEnabled || _flagDisposed) {
                return;
            }
            _serviceUserInterface.Update(timeFrame);
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            if (!_flagVisible || _flagDisposed) {
                return;
            }
            _serviceUserInterface.Draw();
        }
        
        /// <summary>
        /// Disposes the module (no unmanaged resources).
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _serviceUserInterface.Dispose();
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
