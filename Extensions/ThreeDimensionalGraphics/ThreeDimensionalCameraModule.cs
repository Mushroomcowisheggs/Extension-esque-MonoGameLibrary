using System;
using System.Threading;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Forwards host lifecycle callbacks to a three-dimensional camera service.
    /// </summary>
    public sealed class ThreeDimensionalCameraModule :
        IModule,
        MonoGameLibrary.Core.Lifecycle.IUpdateable,
        MonoGameLibrary.Core.Lifecycle.IDrawable,
        IDisposable {
        private readonly IThreeDimensionalCameraService _serviceCamera;
        private readonly int _orderUpdate;
        private readonly int _orderDraw;
        private volatile bool _flagEnabled;
        private volatile bool _flagVisible;
        private int _markDisposed;
        
        /// <summary>
        /// Initializes a camera forwarding module.
        /// </summary>
        /// <param name="serviceCamera">The camera service to forward.</param>
        /// <param name="orderUpdate">The update order used at admission.</param>
        /// <param name="orderDraw">The draw order used at admission.</param>
        public ThreeDimensionalCameraModule(
            IThreeDimensionalCameraService serviceCamera,
            int orderUpdate,
            int orderDraw
        ) {
            if (serviceCamera == null) {
                throw new ArgumentNullException(nameof(serviceCamera));
            }
            _serviceCamera = serviceCamera;
            _orderUpdate = orderUpdate;
            _orderDraw = orderDraw;
            _flagEnabled = true;
            _flagVisible = true;
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
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            builder.RegisterService<IThreeDimensionalCameraService>(_serviceCamera);
            builder.AddModule(
                this,
                new ScheduleOptions(orderUpdate: _orderUpdate, orderDraw: _orderDraw)
            );
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            if (_flagEnabled) {
                _serviceCamera.Update(timeFrame);
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            if (_flagVisible) {
                _serviceCamera.Draw(timeFrame);
            }
        }
        
        /// <summary>
        /// Releases the camera service. The operation is idempotent.
        /// </summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _markDisposed, 1) != 0) {
                return;
            }
            _serviceCamera.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
