using System;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Pooling;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Fluent API for configuring and building an <see cref="IGameHost"/>. 
    /// Uses the Curiously Recurring Template Pattern. 
    /// </summary>
    public interface IGameBuilder<TBuilder> where TBuilder : IGameBuilder<TBuilder> {
        /// <summary>
        /// Registers a service instance, optionally overwriting an existing registration. 
        /// </summary>
        /// <typeparam name="TService">The type of the service. </typeparam>
        /// <param name="instance">The service instance. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces an existing registration. </param>
        /// <returns>The current builder instance. </returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="flagOverwrite"/> is <c>false</c> 
        /// and a service of type <typeparamref name="TService"/> is already registered. </exception>
        TBuilder RegisterService<TService>(TService instance, bool flagOverwrite = false) where TService : class;
        
        /// <summary>
        /// Adds a module to the host. Duplicate modules are rejected immediately. 
        /// </summary>
        /// <param name="module">The module instance to add. </param>
        /// <returns>The current builder instance. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="module"/> is null. </exception>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="module"/> has already been added. </exception>
        TBuilder AddModule(object module);
        
        /// <summary>
        /// Adds a module with the schedule keys it is admitted under. 
        /// </summary>
        /// <param name="module">The module instance to add. </param>
        /// <param name="options">The fixed update and draw order for this admission. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder AddModule(object module, ScheduleOptions options);
        
        /// <summary>
        /// Provides a callback to configure the <see cref="GameHost"/> before modules are added. 
        /// </summary>
        /// <param name="actionConfigure">The action to run against the host. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder ConfigureHost(Action<GameHost> actionConfigure);
        
        /// <summary>
        /// Registers a logger service. 
        /// </summary>
        /// <param name="logger">The logger implementation. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces any previously registered <see cref="ILogger"/>. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder UseLogger(ILogger logger, bool flagOverwrite = false);
        
        /// <summary>
        /// Registers a profiler service. 
        /// </summary>
        /// <param name="profiler">The profiler implementation. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces any previously registered <see cref="IProfiler"/>. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder UseProfiler(IProfiler profiler, bool flagOverwrite = false);
        
        /// <summary>
        /// Registers a thread pool service. 
        /// </summary>
        /// <param name="poolThread">The thread pool implementation. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces any previously registered <see cref="IThreadPool"/>. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder UseThreadPool(IThreadPool poolThread, bool flagOverwrite = false);
        
        /// <summary>
        /// Registers a cancellation service. 
        /// </summary>
        /// <param name="serviceCancellation">The cancellation service implementation. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces any previously registered <see cref="ICancellationService"/>. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder UseCancellationService(ICancellationService serviceCancellation, bool flagOverwrite = false);
        
        /// <summary>
        /// Registers a loading progress service. 
        /// </summary>
        /// <param name="progressLoading">The loading progress implementation. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces any previously registered <see cref="ILoadingProgress"/>. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder UseLoadingProgress(ILoadingProgress progressLoading, bool flagOverwrite = false);
        
        /// <summary>
        /// Registers an object pool factory. 
        /// </summary>
        /// <param name="factory">The pool factory implementation. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces any previously registered <see cref="IObjectPoolFactory"/>. </param>
        /// <returns>The current builder instance. </returns>
        TBuilder UseObjectPoolFactory(IObjectPoolFactory factoryPool, bool flagOverwrite = false);
        
        /// <summary>
        /// Retrieves a registered service instance of the specified type. 
        /// </summary>
        /// <typeparam name="TService">The service type to retrieve. </typeparam>
        /// <returns>The registered service instance. </returns>
        /// <exception cref="InvalidOperationException">Thrown if the service is not registered. </exception>
        TService GetService<TService>() where TService : class;
        
        /// <summary>
        /// Tries to retrieve a registered service instance of the specified type. 
        /// </summary>
        /// <typeparam name="TService">The service type to retrieve. </typeparam>
        /// <param name="instance">The registered service instance if present; otherwise null.</param>
        /// <returns><c>true</c> if the service is registered; otherwise <c>false</c>.</returns>
        bool TryGetService<TService>(out TService instance) where TService : class;
        
        /// <summary>
        /// Clears all registered services from the builder. 
        /// This does not affect modules already added. 
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Build"/> has already been called or is in progress. 
        /// </exception>
        void ClearServices();
        
        /// <summary>
        /// Builds the <see cref="IGameHost"/> with all configured services and modules. 
        /// This method can only be called once per builder instance. 
        /// </summary>
        /// <returns>An initialized <see cref="IGameHost"/>. </returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Build"/> has already been called. </exception>
        IGameHost Build();
    }
}