using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Pooling;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Provides a fluent API for configuring and building an <see cref="IGameHost"/>.
    /// Each instance can only build one host; subsequent calls to <see cref="Build"/> will throw.
    /// </summary>
    public class GameBuilder : IGameBuilder<GameBuilder> {
        private readonly object _lockBuilder = new object();
        private readonly ServiceRegistry _registryService = new ServiceRegistry();
        private readonly HashSet<object> _setModule = new HashSet<object>();
        private readonly List<object> _listModules = new List<object>();
        private Action<GameHost> _actionConfigHost = delegate { };
        private bool _flagIsBuilding = false;
        private bool _flagIsBuilt = false;
        
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
            if (module == null) { throw new ArgumentNullException(nameof(module)); }
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }

                if (_setModule.Contains(module)) {
                    throw new InvalidOperationException($"Module {module.GetType().FullName} already added.");
                }
                _setModule.Add(module);
                _listModules.Add(module);
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
        public TService GetService<TService>() where TService : class {
            return _registryService.Get<TService>();
        }
        
        /// <inheritdoc />
        public bool TryGetService<TService>(out TService instance) where TService : class {
            return _registryService.TryGet<TService>(out instance);
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
            List<object> listModulesSnapshot;
            
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already completed."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                
                _flagIsBuilding = true;
                host = new GameHost(_registryService);
                actionToRunConfig = _actionConfigHost;
                listModulesSnapshot = new List<object>(_listModules);
            }
            
            try {
                actionToRunConfig(host);
                
                // Set up exception forwarding for the default thread pool so that unhandled exceptions
                // are reported to the host's OnError callback. Custom IThreadPool implementations must
                // handle exception propagation themselves. 
                if (_registryService.TryGet<DefaultThreadPool>(out var poolThread)) {
                    poolThread.SetExceptionHandlerProvider(delegate() { return host.OnError; });
                }
                
                foreach (object module in listModulesSnapshot) {
                    host.AddModule(module);
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