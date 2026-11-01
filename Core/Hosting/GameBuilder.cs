using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Pooling;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Provides a fluent API for configuring and building an <see cref="IGameHost"/>.
    /// Each instance can only build one host; subsequent calls to <see cref="Build"/> will throw.
    /// Every member, resolution included, is a composition-phase operation: once the builder has
    /// built, it neither accepts registrations nor resolves services.
    /// <see cref="FailurePolicy"/> is required and decides whether a failed Build releases the
    /// accepted modules or retains them in <see cref="Failure"/> for the caller.
    /// </summary>
    public class GameBuilder : IGameBuilder<GameBuilder> {
        private readonly object _lockBuilder = new object();
        private readonly ServiceRegistry _registryService = new ServiceRegistry();
        private readonly HashSet<object> _setModule = new HashSet<object>();
        private readonly List<ModuleEntry> _listModules = new List<ModuleEntry>();
        private Action<GameHost> _actionConfigHost = delegate { };
        private bool _flagIsBuilding = false;
        private bool _flagIsBuilt = false;
        private bool _flagFailed = false;
        private FailurePolicy _policyFailure = FailurePolicy.Unspecified;
        private LifecycleFailure _failure;
        
        /// <summary>
        /// Gets or sets the failure policy used by <see cref="Build"/>. The policy is required;
        /// <see cref="FailurePolicy.Unspecified"/> is rejected by Build.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown after Build started or completed. </exception>
        public FailurePolicy FailurePolicy {
            get {
                lock (_lockBuilder) {
                    return _policyFailure;
                }
            }
            set {
                if (value != FailurePolicy.Release && value != FailurePolicy.Retain) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock (_lockBuilder) {
                    if (_flagIsBuilding || _flagIsBuilt || _flagFailed) {
                        throw new InvalidOperationException("FailurePolicy cannot be changed after Build starts.");
                    }
                    _policyFailure = value;
                }
            }
        }
        
        /// <summary>
        /// Gets the diagnostic record of the last failed composition or registration, or null.
        /// </summary>
        public LifecycleFailure Failure {
            get {
                lock (_lockBuilder) {
                    return _failure;
                }
            }
        }
        
        private void ThrowIfUnusable() {
            if (_flagFailed) {
                throw new InvalidOperationException(
                    "Build previously failed. This builder has released its modules and cannot be reused."
                );
            }
        }
        
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
                ThrowIfUnusable();
                
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
        
        /// <summary>
        /// Handles a module constructed by <see cref="Modularity.ModuleLoader"/> whose registration
        /// failed. If the module already entered this builder, it is removed first so a later Build
        /// cannot release it twice. <see cref="FailurePolicy.Retain"/> leaves the module in
        /// <see cref="Failure"/>; otherwise construction cleanup releases it.
        /// </summary>
        /// <param name="module">The constructed module. </param>
        /// <param name="exception">The registration failure. </param>
        /// <param name="report">Receives a release failure without propagating it. </param>
        internal void HandleRegistrationFailure(
            object module,
            Exception exception,
            Action<Exception> report
        ) {
            FailurePolicy policyFailure;
            lock (_lockBuilder) {
                policyFailure = _policyFailure;
                if (_setModule.Remove(module)) {
                    _listModules.RemoveAll(delegate(ModuleEntry entry) {
                        return ReferenceEquals(entry.Module, module);
                    });
                }
                if (policyFailure == FailurePolicy.Retain) {
                    List<object> listRetained = new List<object>();
                    listRetained.Add(module);
                    _failure = new LifecycleFailure(
                        "ModuleLoader.Register",
                        exception,
                        policyFailure,
                        listRetained,
                        new List<Exception>()
                    );
                    return;
                }
            }
            
            List<Exception> listReleaseFailure = new List<Exception>();
            IDisposable disposable = module as IDisposable;
            if (disposable != null) {
                try {
                    disposable.Dispose();
                } catch (Exception exceptionRelease) {
                    listReleaseFailure.Add(exceptionRelease);
                    report(exceptionRelease);
                }
            }
            lock (_lockBuilder) {
                _failure = new LifecycleFailure(
                    "ModuleLoader.Register",
                    exception,
                    policyFailure,
                    new List<object>(),
                    listReleaseFailure
                );
            }
        }
        
        /// <inheritdoc />
        public GameBuilder AddModule(object module) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            if (module is IUpdateable || module is IDrawable) {
                throw new NotSupportedException(
                    $"The participant {module.GetType().FullName} takes part in a schedule, so its order " +
                    "must be supplied explicitly: AddModule(module, new ScheduleOptions(orderUpdate, orderDraw)). "
                );
            }
            return AddModule(module, ScheduleOptions.Default);
        }
        
        /// <inheritdoc />
        public GameBuilder AddModule(object module, ScheduleOptions options) {
            if (module == null) { throw new ArgumentNullException(nameof(module)); }
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                ThrowIfUnusable();
                
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
                ThrowIfUnusable();
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
        public GameBuilder UseThreadPool(IThreadPool poolThread, bool flagOverwrite = false) {
            RegisterService(poolThread, flagOverwrite);
            DefaultThreadPool poolDefault = poolThread as DefaultThreadPool;
            if (poolDefault != null) {
                RegisterService<DefaultThreadPool>(poolDefault, flagOverwrite);
            }
            return this;
        }
        
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
                ThrowIfUnusable();
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
                ThrowIfUnusable();
                return _registryService.TryGet<TService>(out instance);
            }
        }
        
        /// <inheritdoc />
        public void ClearServices() {
            lock (_lockBuilder) {
                if (_flagIsBuilt) { throw new InvalidOperationException("Build already called."); }
                if (_flagIsBuilding) { throw new InvalidOperationException("Build in progress."); }
                ThrowIfUnusable();
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
                ThrowIfUnusable();
                if (_policyFailure == FailurePolicy.Unspecified) {
                    throw new InvalidOperationException(
                        "FailurePolicy must be set to Release or Retain before Build."
                    );
                }
                
                _flagIsBuilding = true;
                // The host is constructed without the registry: after this point the capability set is
                // closed and there is no path from a participant back to it.
                host = new GameHost(_policyFailure);
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
                
                lock (_lockBuilder) {
                    _failure = null;
                    _flagIsBuilt = true;
                    _listModules.Clear();
                    _setModule.Clear();
                }
                return host;
            } catch (Exception exception) {
                object[] arrayAdmitted;
                FailurePolicy policyFailure;
                try {
                    arrayAdmitted = host.DetachAdmissions();
                } catch (Exception) {
                    arrayAdmitted = new object[0];
                }
                
                List<ModuleEntry> listSubmitted;
                lock (_lockBuilder) {
                    policyFailure = _policyFailure;
                    listSubmitted = new List<ModuleEntry>(_listModules);
                    _listModules.Clear();
                    _setModule.Clear();
                    _registryService.Clear();
                    _flagIsBuilding = false;
                    _flagFailed = true;
                }
                
                List<object> listOwned = new List<object>();
                HashSet<object> setOwned = new HashSet<object>();
                foreach (object module in arrayAdmitted) {
                    if (setOwned.Add(module)) {
                        listOwned.Add(module);
                    }
                }
                foreach (ModuleEntry entry in listSubmitted) {
                    if (setOwned.Add(entry.Module)) {
                        listOwned.Add(entry.Module);
                    }
                }
                
                List<Exception> listReleaseFailure = new List<Exception>();
                List<object> listRetained = new List<object>();
                if (policyFailure == FailurePolicy.Release) {
                    OwnershipRelease.ReleaseModules(
                        listOwned,
                        delegate(Exception exceptionRelease, string context) {
                            listReleaseFailure.Add(new InvalidOperationException(context, exceptionRelease));
                        },
                        "GameBuilder.Build"
                    );
                    if (listReleaseFailure.Count > 0) {
                        exception.Data["ReleaseFailures"] = new AggregateException(listReleaseFailure);
                    }
                } else {
                    listRetained.AddRange(listOwned);
                }
                
                lock (_lockBuilder) {
                    _failure = new LifecycleFailure(
                        "GameBuilder.Build",
                        exception,
                        policyFailure,
                        listRetained,
                        listReleaseFailure
                    );
                }
                throw;
            }
        }
    }
}
