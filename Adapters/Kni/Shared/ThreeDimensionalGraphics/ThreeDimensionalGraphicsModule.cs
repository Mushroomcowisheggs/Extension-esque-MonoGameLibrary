using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Kni.ThreeDimensionalGraphics {
    /// <summary>
    /// Registers MonoGame three-dimensional camera and mesh rendering services,
    /// model loaders, and their lifecycle participants.
    /// </summary>
    [ModuleRegistration(-445)]
    public sealed class ThreeDimensionalGraphicsModule :
        IModule,
        MonoGameLibrary.Core.Lifecycle.IDrawable,
        IDisposable {
        private readonly int _orderUpdate;
        private readonly int _orderDraw;
        private MeshRenderService _serviceRender;
        private ThreeDimensionalCameraModule _moduleCamera;
        private readonly object _lock = new object();
        private bool _flagVisible;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a three-dimensional graphics module.
        /// </summary>
        /// <param name="orderUpdate">The camera update order.</param>
        /// <param name="orderDraw">The renderer draw order.</param>
        public ThreeDimensionalGraphicsModule(int orderUpdate = 0, int orderDraw = 0) {
            _orderUpdate = orderUpdate;
            _orderDraw = orderDraw;
            _flagVisible = true;
        }
        
        /// <inheritdoc />
        public bool Visible {
            get { lock (_lock) { return _flagVisible; } }
            set { lock (_lock) { _flagVisible = value; } }
        }
        
        /// <inheritdoc />
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            Game game = builder.GetService<Game>();
            if (game == null) {
                throw new InvalidOperationException("Game service is not registered.");
            }
            IContentBackend backendContent = builder.GetService<IContentBackend>();
            if (backendContent == null) {
                throw new InvalidOperationException("Content backend service is not registered.");
            }
            
            backendContent.RegisterScoped<IMesh>(new MeshLoader().Load);
            backendContent.RegisterScoped<IModel>(ThreeDimensionalGraphicsLoaders.Load<IModel>);
            ThreeDimensionalViewport viewport = new ThreeDimensionalViewport(game.GraphicsDevice);
            ThreeDimensionalCameraService serviceCamera = new ThreeDimensionalCameraService(viewport);
            _moduleCamera = new ThreeDimensionalCameraModule(
                serviceCamera,
                _orderUpdate - 1,
                _orderDraw - 1
            );
            _moduleCamera.Register(builder);
            IThreeDimensionalCameraService serviceCameraResolved =
                builder.GetService<IThreeDimensionalCameraService>();
            _serviceRender = new MeshRenderService(game.GraphicsDevice, serviceCameraResolved);
            builder.RegisterService<IMeshRenderService>(_serviceRender);
            builder.AddModule(
                this,
                new ScheduleOptions(orderDraw: _orderDraw)
            );
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            bool flagShouldDraw;
            lock (_lock) {
                flagShouldDraw = _flagVisible && !_flagDisposed && _serviceRender != null;
            }
            if (flagShouldDraw) {
                _serviceRender.Draw(timeFrame);
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            if (_serviceRender != null) {
                _serviceRender.Dispose();
                _serviceRender = null;
            }
            _moduleCamera = null;
            GC.SuppressFinalize(this);
        }
    }
}
