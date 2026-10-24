using System;
using System.Threading;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Integrates room discovery with the host lifecycle. Every frame it expires the rooms that stopped
    /// announcing themselves and reports what the background loops found.
    /// </summary>
    public sealed class DiscoveryModule : IModule, IUpdateable, IDisposable {
        private readonly IDiscoveryService _serviceDiscovery;
        private readonly int _order;
        // Read on the frame thread while the application writes it, and elected atomically so the
        // service is released exactly once.
        private volatile bool _flagEnabled = true;
        private int _markDisposed;
        
        /// <summary>
        /// Initializes a new module.
        /// </summary>
        /// <param name="serviceDiscovery">The discovery service to drive.</param>
        /// <param name="order">The update order.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceDiscovery"/> is null.</exception>
        public DiscoveryModule(IDiscoveryService serviceDiscovery, int order = -120) {
            if (serviceDiscovery == null) {
                throw new ArgumentNullException(nameof(serviceDiscovery));
            }
            _serviceDiscovery = serviceDiscovery;
            _order = order;
        }
        
        /// <inheritdoc />
        public int Order {
            get {
                return _order;
            }
        }
        
        /// <inheritdoc />
        public bool Enabled {
            get {
                return _flagEnabled;
            }
            set {
                _flagEnabled = value;
            }
        }
        
        /// <inheritdoc />
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            builder.RegisterService<IDiscoveryService>(_serviceDiscovery);
            builder.AddModule(this);
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            if (!_flagEnabled) {
                return;
            }
            _serviceDiscovery.Update(timeFrame);
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (Interlocked.Exchange(ref _markDisposed, 1) != 0) {
                return;
            }
            _serviceDiscovery.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
