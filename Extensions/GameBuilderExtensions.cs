using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Pooling;

namespace MonoGameLibrary.Extensions {
    /// <summary>
    /// Convenience extensions for registering optional services and modules. 
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>
        /// Registers a default set of services (logger, profiler, thread pool, cancellation, loading progress, object pool) if they are not already registered. 
        /// This helper is idempotent: an existing registration is always kept (whether it was made by an 
        /// earlier call to this method or by an explicit <c>UseLogger</c>/<c>RegisterService</c> call), 
        /// and no exception is thrown for a duplicate. 
        /// </summary>
        /// <param name="builder">The game builder instance.</param>
        /// <param name="logger">Optional logger implementation. If null, <see cref="ConsoleLogger"/> is used. </param>
        /// <param name="profiler">Optional profiler implementation. If null, <see cref="NoOperationProfiler"/> is used. </param>
        /// <param name="poolThread">Optional thread pool implementation. If null, <see cref="DefaultThreadPool"/> is used. </param>
        /// <param name="serviceCancellation">Optional cancellation service. If null, <see cref="DefaultCancellationService"/> is used. </param>
        /// <param name="progressLoading">Optional loading progress service. If null, <see cref="DefaultLoadingProgress"/> is used. </param>
        /// <param name="factoryObjectPool">Optional object pool factory. If null, <see cref="DefaultObjectPoolFactory"/> is used. </param>
        /// <returns>The game builder instance. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        public static GameBuilder UseDefaultServices(
            this GameBuilder builder,
            Optional<ILogger> logger = default,
            Optional<IProfiler> profiler = default,
            Optional<IThreadPool> poolThread = default,
            Optional<ICancellationService> serviceCancellation = default,
            Optional<ILoadingProgress> progressLoading = default,
            Optional<IObjectPoolFactory> factoryObjectPool = default
        ) {
            ILogger loggerResolved = logger.HasValue ? logger.Value : NullLogger.Instance;
            IProfiler profilerResolved = profiler.HasValue ? profiler.Value : new NoOperationProfiler();
            IThreadPool poolResolvedThread = poolThread.HasValue ? poolThread.Value : new DefaultThreadPool();
            ICancellationService serviceResolvedCancellation = serviceCancellation.HasValue ? serviceCancellation.Value : new DefaultCancellationService();
            ILoadingProgress progressResolvedLoading = progressLoading.HasValue ? progressLoading.Value : new DefaultLoadingProgress();
            IObjectPoolFactory factoryResolvedObjectPool = factoryObjectPool.HasValue ? factoryObjectPool.Value : new DefaultObjectPoolFactory();
            
            RegisterIfAbsent<ILogger>(builder, loggerResolved);
            RegisterIfAbsent<IProfiler>(builder, profilerResolved);
            RegisterIfAbsent<IThreadPool>(builder, poolResolvedThread);
            RegisterIfAbsent<ICancellationService>(builder, serviceResolvedCancellation);
            RegisterIfAbsent<ILoadingProgress>(builder, progressResolvedLoading);
            RegisterIfAbsent<IObjectPoolFactory>(builder, factoryResolvedObjectPool);
            return builder;
        }
        
        /// <summary>
        /// Registers the instance only when no service of that type is registered yet, keeping any 
        /// existing registration. Unlike <see cref="GameBuilder.RegisterService{TService}"/>, a 
        /// duplicate is silently ignored instead of throwing. 
        /// </summary>
        private static void RegisterIfAbsent<TService>(GameBuilder builder, TService instance) where TService : class {
            TService serviceExisting;
            if (!builder.TryGetService<TService>(out serviceExisting)) {
                builder.RegisterService<TService>(instance, flagOverwrite: false);
            }
        }
    }
}
