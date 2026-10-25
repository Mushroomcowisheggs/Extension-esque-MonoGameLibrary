using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Pooling;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Provides a fluent API for configuring and building an <see cref="IGameHost"/>.
    /// Each instance can only build one host; subsequent calls to <see cref="Build"/> will throw.
    /// Every member, resolution included, is a composition-phase operation: once the builder has
    /// built, it neither accepts registrations nor resolves services.
    /// </summary>
    public class GameBuilder : IGameBuilder<GameBuilder> {
        private readonly object _lockBuilder = new object();
        private readonly ServiceRegistry _registryService = new ServiceRegistry();
        private readonly HashSet<object> _setModule = new HashSet<object>();
        private readonly List<ModuleEntry> _listModules = new List<ModuleEntry>();
        private Action<GameHost> _actionConfigHost = delegate { };
        private bool _flagIsBuilding = false;
        private bool _flagIsBuilt = false;
        
        /// <summary>
        /// One admitted module together with the schedule keys it was registered under. 
        /// </summary>
        private readonly struct ModuleEntry {
            /// <summary>
            /// Initializes a new entry. 
            /// </summary>
            /// <param name="module">The module instance. </param>
            /// <param name="options">The fixed schedule keys. </param>
            public ModuleEntry(object module, ScheduleOptions options) {
                if (module == null) {
                    throw new ArgumentNullException(nameof(module));
                }
                Module = module;
                Options = options;
            }
            
            /// <summary>
            /// Gets the module instance. 
            /// </summary>
            public object Module { get; }
            
            /// <summary>
            /// Gets the schedule keys. 
            /// </summary>
            public ScheduleOptions Options { get; }
        }
        
        /// <inheritdoc />
        public GameBuilder RegisterService<TService>(TService instance, bool flagOverwrite = false) where TService : class {
            if (instance == null) { throw new ArgumentNullException(nameof(instance)); }
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }

                if (flagOverwrite) {
                    _registryService.TryRegister(instance, flagOverwrite: true);
                } else {
                    if (!_registryService.TryRegister(instance, flagOverwrite: false)) {
                        throw new InvalidOperationException($"Service of type {typeof(TService).FullName} is already registered.");
                    }
                }
            }
            return this;
        }
        
        /// <inheritdoc />
        public GameBuilder AddModule(object module) {
            return AddModule(module, ScheduleOptions.Default);
        }
        
        /// <inheritdoc />
        public GameBuilder AddModule(object module, ScheduleOptions options) {
            if (module == null) { throw new ArgumentNullException(nameof(module)); }
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }

                if (_setModule.Contains(module)) {
                    throw new InvalidOperationException($"Module {module.GetType().FullName} already added.");
                }
                _setModule.Add(module);
                _listModules.Add(new ModuleEntry(module, options));
            }
            return this;
        }
        
        /// <inheritdoc />
        public GameBuilder ConfigureHost(Action<GameHost> actionConfigure) {
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                if (actionConfigure == null) { throw new ArgumentNullException(nameof(actionConfigure)); }
                _actionConfigHost = actionConfigure;
            }
            return this;
        }
        
        /// <inheritdoc />
        public GameBuilder UseLogger(ILogger logger, bool flagOverwrite = false) { RegisterService(logger, flagOverwrite); return this; }
        
        /// <inheritdoc />
        public GameBuilder UseProfiler(IProfiler profiler, bool flagOverwrite = false) { RegisterService(profiler, flagOverwrite); return this; }
        
        /// <inheritdoc />
        public GameBuilder UseThreadPool(IThreadPool poolThread, bool flagOverwrite = false) { RegisterService(poolThread, flagOverwrite); return this; }
        
        /// <inheritdoc />
        public GameBuilder UseCancellationService(ICancellationService serviceCancellation, bool flagOverwrite = false) { RegisterService(serviceCancellation, flagOverwrite); return this; }
        
        /// <inheritdoc />
        public GameBuilder UseLoadingProgress(ILoadingProgress progressLoading, bool flagOverwrite = false) { RegisterService(progressLoading, flagOverwrite); return this; }
        
        /// <inheritdoc />
        public GameBuilder UseObjectPoolFactory(IObjectPoolFactory factoryPool, bool flagOverwrite = false) { RegisterService(factoryPool, flagOverwrite); return this; }
        
        /// <inheritdoc />
        /// <remarks>
        /// Resolution is bound to the composition phase exactly as registration is. A module is given
        /// the builder in <c>IModule.Register</c> and may resolve there, but a module that stores the
        /// builder and resolves after <see cref="Build"/> has returned gets an exception instead of a
        /// capability, which is what keeps the registry a composition-root tool rather than a service
        /// locator reachable from the frame loop.
        /// </remarks>
        public TService GetService<TService>() where TService : class {
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                return _registryService.Get<TService>();
            }
        }
        
        /// <inheritdoc />
        /// <remarks>
        /// The same composition-phase bound as <see cref="GetService{TService}"/>: after
        /// <see cref="Build"/> has returned this reports failure instead of resolving.
        /// </remarks>
        public bool TryGetService<TService>(out TService instance) where TService : class {
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                return _registryService.TryGet<TService>(out instance);
            }
        }
        
        /// <inheritdoc />
        public void ClearServices() {
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                _registryService.Clear();
            }
        }
        
        /// <inheritdoc />
        public IGameHost Build() {
            GameHost host;
            Action<GameHost> actionToRunConfig;
            List<ModuleEntry> listModulesSnapshot;
            
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already completed."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                
                _flagIsBuilding = true;
                // The host is constructed without the registry: after this point the capability set is
                // closed and there is no path from a participant back to it.
                host = new GameHost();
                actionToRunConfig = _actionConfigHost;
                listModulesSnapshot = new List<ModuleEntry>(_listModules);
            }
            
            try {
                actionToRunConfig(host);
                
                // Set up exception forwarding for the default thread pool so that unhandled exceptions
                // are reported to the host's OnError callback. Custom IThreadPool implementations must
                // handle exception propagation themselves. 
                if (_registryService.TryGet<DefaultThreadPool>(out var poolThread)) {
                    poolThread.SetExceptionHandlerProvider(delegate() { return host.OnError; });
                }
                
                foreach (ModuleEntry entry in listModulesSnapshot) {
                    host.AddModule(entry.Module, entry.Options);
                }
                
                lock (_lockBuilder) { _flagIsBuilt = true; }
                return host;
            } catch (Exception) {
                try { host.Dispose(); } catch (Exception) { }
                lock (_lockBuilder) {
                    _registryService.Clear();
                    _flagIsBuilding = false;
                }
                throw;
            }
        }
    }
}
