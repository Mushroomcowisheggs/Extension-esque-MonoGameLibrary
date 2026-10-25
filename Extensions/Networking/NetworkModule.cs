using System;
using System.Threading;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Integrates a network session with the host lifecycle. It owns no logic of its own: every frame it asks
    /// the service to dispatch what the background loops received, so that a game only ever observes network
    /// events between two frames.
    /// </summary>
    public sealed class NetworkModule : IModule, IUpdateable, IDisposable {
        private readonly INetworkService _serviceNetwork;
        private readonly Optional<INetworkProbe> _probeNetwork;
        private readonly int _order;
        // Read on the frame thread while the application writes it, and elected atomically so the
        // service is released exactly once.
        private volatile bool _flagEnabled = true;
        private int _markDisposed;
        
        /// <summary>
        /// Initializes a new module.
        /// </summary>
        /// <param name="serviceNetwork">The session service to drive.</param>
        /// <param name="probeNetwork">The probe capability of the same service, when it offers one.</param>
        /// <param name="order">The update order. The default runs before input and game logic so that a frame reacts to what already arrived.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceNetwork"/> is null.</exception>
        public NetworkModule(INetworkService serviceNetwork, Optional<INetworkProbe> probeNetwork = default, int order = -128) {
            if (serviceNetwork == null) {
                throw new ArgumentNullException(nameof(serviceNetwork));
            }
            _serviceNetwork = serviceNetwork;
            _probeNetwork = probeNetwork;
            _order = order;
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
            builder.RegisterService<INetworkService>(_serviceNetwork);
            if (_probeNetwork.HasValue) {
                builder.RegisterService<INetworkProbe>(_probeNetwork.Value);
            }
            builder.AddModule(this, new ScheduleOptions(orderUpdate: _order));
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            if (!_flagEnabled) {
                return;
            }
            _serviceNetwork.Update(timeFrame);
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (Interlocked.Exchange(ref _markDisposed, 1) != 0) {
                return;
            }
            _serviceNetwork.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
