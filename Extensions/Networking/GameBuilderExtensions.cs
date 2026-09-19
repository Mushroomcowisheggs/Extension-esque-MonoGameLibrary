using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Extension methods for adding networking support to a <see cref="GameBuilder"/>.
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>
        /// Registers a network session, a status probe and local-network room discovery, together with the two
        /// modules that drive them. No socket is opened until the game hosts, connects, browses or announces,
        /// so registering the extension has no side effect on startup.
        /// </summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="optionsNetwork">Optional session settings. Defaults are used when omitted.</param>
        /// <param name="optionsDiscovery">Optional discovery settings. Defaults are used when omitted.</param>
        /// <param name="serializerNetwork">Optional message serializer. A <see cref="JsonNetworkSerializer"/> is used when omitted.</param>
        /// <param name="transportNetwork">Optional transport. A <see cref="TcpNetworkTransport"/> is used when omitted.</param>
        /// <param name="logger">Optional logger. The registered <see cref="ILogger"/> is used when omitted.</param>
        /// <param name="profiler">Optional profiler. The registered <see cref="IProfiler"/> is used when omitted.</param>
        /// <param name="poolThread">Optional thread pool. The registered <see cref="IThreadPool"/> is required when omitted.</param>
        /// <param name="serviceCancellation">Optional cancellation service. The registered one is used when omitted.</param>
        /// <param name="orderNetwork">The update order of the session module.</param>
        /// <param name="orderDiscovery">The update order of the discovery module.</param>
        /// <returns>The game builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no thread pool is registered.</exception>
        public static GameBuilder UseNetworking(
            this GameBuilder builder,
            Optional<NetworkOptions> optionsNetwork = default,
            Optional<DiscoveryOptions> optionsDiscovery = default,
            Optional<INetworkSerializer> serializerNetwork = default,
            Optional<INetworkTransport> transportNetwork = default,
            Optional<ILogger> logger = default,
            Optional<IProfiler> profiler = default,
            Optional<IThreadPool> poolThread = default,
            Optional<ICancellationService> serviceCancellation = default,
            int orderNetwork = -128,
            int orderDiscovery = -120
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            NetworkOptions optionsNetworkResolved;
            if (optionsNetwork.HasValue) {
                optionsNetworkResolved = optionsNetwork.Value;
            } else {
                optionsNetworkResolved = new NetworkOptions();
            }
            DiscoveryOptions optionsDiscoveryResolved;
            if (optionsDiscovery.HasValue) {
                optionsDiscoveryResolved = optionsDiscovery.Value;
            } else {
                optionsDiscoveryResolved = new DiscoveryOptions();
            }
            optionsNetworkResolved.Validate();
            optionsDiscoveryResolved.Validate();
            
            ILogger loggerResolved = ResolveLogger(builder, logger);
            IThreadPool poolResolved = ResolveThreadPool(builder, poolThread);
            ICancellationService cancellationResolved = ResolveCancellation(builder, serviceCancellation);
            Optional<IProfiler> profilerResolved = ResolveProfiler(builder, profiler);
            INetworkSerializer serializerResolved;
            if (serializerNetwork.HasValue) {
                serializerResolved = serializerNetwork.Value;
            } else {
                serializerResolved = new JsonNetworkSerializer();
            }
            INetworkTransport transportResolved;
            if (transportNetwork.HasValue) {
                transportResolved = transportNetwork.Value;
            } else {
                transportResolved = new TcpNetworkTransport(new Optional<ILogger>(loggerResolved));
            }
            
            NetworkService serviceNetwork = new NetworkService(
                optionsNetworkResolved,
                serializerResolved,
                transportResolved,
                poolResolved,
                cancellationResolved,
                loggerResolved,
                profilerResolved
            );
            NetworkModule moduleNetwork = new NetworkModule(serviceNetwork, serviceNetwork, orderNetwork);
            moduleNetwork.Register(builder);
            
            DiscoveryService serviceDiscovery = new DiscoveryService(
                optionsNetworkResolved,
                optionsDiscoveryResolved,
                serializerResolved,
                poolResolved,
                cancellationResolved,
                loggerResolved,
                profilerResolved
            );
            DiscoveryModule moduleDiscovery = new DiscoveryModule(serviceDiscovery, orderDiscovery);
            moduleDiscovery.Register(builder);
            
            return builder;
        }
        
        /// <summary>
        /// Resolves the logger, accepting an explicit instance, the registered one, or a silent fallback.
        /// </summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="logger">The optional explicit logger.</param>
        /// <returns>The logger to use.</returns>
        private static ILogger ResolveLogger(GameBuilder builder, Optional<ILogger> logger) {
            if (logger.HasValue) {
                return logger.Value;
            }
            ILogger loggerRegistered;
            if (builder.TryGetService<ILogger>(out loggerRegistered)) {
                if (loggerRegistered != null) {
                    return loggerRegistered;
                }
            }
            return NullLogger.Instance;
        }
        
        /// <summary>
        /// Resolves the thread pool, which the extension cannot work without because every socket operation
        /// runs on it.
        /// </summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="poolThread">The optional explicit thread pool.</param>
        /// <returns>The thread pool to use.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no thread pool is available.</exception>
        private static IThreadPool ResolveThreadPool(GameBuilder builder, Optional<IThreadPool> poolThread) {
            if (poolThread.HasValue) {
                return poolThread.Value;
            }
            IThreadPool poolRegistered;
            if (builder.TryGetService<IThreadPool>(out poolRegistered)) {
                if (poolRegistered != null) {
                    return poolRegistered;
                }
            }
            throw new InvalidOperationException(
                "Networking needs a thread pool for its background loops. Call UseDefaultServices() or register an IThreadPool before calling UseNetworking()."
            );
        }
        
        /// <summary>
        /// Resolves the cancellation service, accepting an explicit instance, the registered one, or a private
        /// fallback that still lets the extension stop its loops.
        /// </summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="serviceCancellation">The optional explicit cancellation service.</param>
        /// <returns>The cancellation service to use.</returns>
        private static ICancellationService ResolveCancellation(GameBuilder builder, Optional<ICancellationService> serviceCancellation) {
            if (serviceCancellation.HasValue) {
                return serviceCancellation.Value;
            }
            ICancellationService serviceRegistered;
            if (builder.TryGetService<ICancellationService>(out serviceRegistered)) {
                if (serviceRegistered != null) {
                    return serviceRegistered;
                }
            }
            return new DefaultCancellationService();
        }
        
        /// <summary>
        /// Resolves the profiler, which stays absent when neither an explicit instance nor a registered one
        /// exists.
        /// </summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="profiler">The optional explicit profiler.</param>
        /// <returns>The profiler to use, or an empty optional.</returns>
        private static Optional<IProfiler> ResolveProfiler(GameBuilder builder, Optional<IProfiler> profiler) {
            if (profiler.HasValue) {
                return profiler;
            }
            IProfiler profilerRegistered;
            if (builder.TryGetService<IProfiler>(out profilerRegistered)) {
                if (profilerRegistered != null) {
                    return new Optional<IProfiler>(profilerRegistered);
                }
            }
            return default;
        }
    }
}
