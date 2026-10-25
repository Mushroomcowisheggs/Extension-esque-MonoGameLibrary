using System;
using System.Threading;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Host-driven module that forwards update and draw calls to <see cref="IUserInterfaceService"/>.
    /// Contains no platform logic.
    /// </summary>
    public sealed class UserInterfaceModule : IUpdateable, IDrawable, IDisposable {
        private readonly IUserInterfaceService _serviceUserInterface;
        // Every mutable field is volatile: the gates are read on the frame thread while the application
        // writes them, and disposal is elected atomically so the service is released once.
        private volatile bool _flagEnabled = true;
        private volatile bool _flagVisible = true;
        private int _markDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterfaceModule"/> class.
        /// </summary>
        /// <param name="serviceUserInterface">The UI service to forward calls to.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceUserInterface"/> is null.</exception>
        public UserInterfaceModule(IUserInterfaceService serviceUserInterface) {
            if (serviceUserInterface == null) {
                throw new ArgumentNullException(nameof(serviceUserInterface));
            }
            _serviceUserInterface = serviceUserInterface;
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
            if (!_flagEnabled || Volatile.Read(ref _markDisposed) != 0) {
                return;
            }
            _serviceUserInterface.Update(timeFrame);
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            if (!_flagVisible || Volatile.Read(ref _markDisposed) != 0) {
                return;
            }
            _serviceUserInterface.Draw();
        }
        
        /// <summary>
        /// Disposes the module (no unmanaged resources). Exactly one caller releases the service.
        /// </summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _markDisposed, 1) != 0) {
                return;
            }
            _serviceUserInterface.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
