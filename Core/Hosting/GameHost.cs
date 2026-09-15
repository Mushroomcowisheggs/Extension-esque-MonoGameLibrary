using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Default implementation of <see cref="IGameHost"/>, coordinating module loading, updating, drawing, and disposal. 
    /// All public instance members are thread‑safe; the host can be used from multiple threads. 
    /// </summary>
    public class GameHost : IGameHost {
        private readonly object _lock = new object();
        private readonly IServiceRegistry _registryService;
        private readonly HashSet<object> _setModule = new HashSet<object>();
        private readonly List<object> _listAllModules = new List<object>();
        private readonly List<ILoadable> _listLoadableModules = new List<ILoadable>();
        private readonly List<IUpdateable> _listUpdateableModules = new List<IUpdateable>();
        private readonly List<IDrawable> _listDrawableModules = new List<IDrawable>();
        
        private List<IUpdateable> _cachedUpdateablesSorted = new List<IUpdateable>();
        private List<IDrawable> _cachedDrawablesSorted = new List<IDrawable>();
        private List<IUpdateable> _backBufferUpdateables = new List<IUpdateable>();
        private List<IDrawable> _backBufferDrawables = new List<IDrawable>();
        
        private Optional<IContentService> _serviceContent;
        
        // Lifecycle state flags
        private bool _flagIsInitialized = false;
        private bool _flagIsFaulted = false;
        private bool _flagDisposing = false;
        private bool _flagDisposed = false;
        
        // Dirty flags
        private bool _flagUpdateOrderDirty = true;
        private bool _flagDrawOrderDirty = true;
        
        // Concurrency tracking
        private int _countActiveOperations = 0;
        private readonly HashSet<int> _setActiveThreads = new HashSet<int>();
        private readonly ManualResetEventSlim _eventOperationsIdle = new ManualResetEventSlim(true);
        
        // Error handling callback
        private Action<Exception, string> _actionOnError = delegate { };
        
        /// <inheritdoc />
        public Action<Exception, string> OnError {
            get { lock (_lock) { return _actionOnError; } }
            set { lock (_lock) { 
                if (value != null) {
                    _actionOnError = value;
                } else { 
                    _actionOnError = delegate { };
                }
            } }
        }
        
        /// <inheritdoc />
        public IServiceRegistry Services {
            get {
                lock (_lock) {
                    if (_flagDisposed) {
                        throw new ObjectDisposedException(
                            nameof(GameHost), 
                            "The GameHost has been disposed. "
                        );
                    }
                    // During disposal (_flagDisposing == true) services remain accessible. 
                    return _registryService;
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GameHost"/> class. 
        /// </summary>
        /// <param name="registryService">The service registry used by the host. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="registryService"/> is null. </exception>
        public GameHost(IServiceRegistry registryService) {
            if (registryService == null) {
                throw new ArgumentNullException(nameof(registryService));
            }
            _registryService = registryService;
        }
        
        /// <inheritdoc />
        public void AddModule(object module) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            
            lock (_lock) {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(
                        nameof(GameHost), 
                        "The GameHost has been disposed. "
                    );
                }
                if (_flagDisposing) {
                    throw new ObjectDisposedException(
                        nameof(GameHost), 
                        "The GameHost is in the process of being disposed. "
                    );
                }
                if (_flagIsFaulted) {
                    throw new InvalidOperationException(
                        "Cannot add modules to a GameHost that is in a faulted state. "
                    );
                }
                
                if (_setModule.Contains(module)) {
                    throw new InvalidOperationException(
                        $"The module {module.GetType().FullName} has already been added. "
                    );
                }
                
                _setModule.Add(module);
                _listAllModules.Add(module);
                
                // Categorize module according to implemented interfaces
                if (module is ILoadable loadable) {
                    _listLoadableModules.Add(loadable);
                }
                if (module is IUpdateable updateable) {
                    _listUpdateableModules.Add(updateable);
                    _flagUpdateOrderDirty = true;
                }
                if (module is IDrawable drawable) {
                    _listDrawableModules.Add(drawable);
                    _flagDrawOrderDirty = true;
                }
            }
        }
        
        /// <inheritdoc />
        public void Initialize(IContentService serviceContent) {
            if (serviceContent == null) {
                throw new ArgumentNullException(nameof(serviceContent));
            }
            
            EnterOperation();
            try {
                List<ILoadable> modulesToLoad;
                
                lock (_lock) {
                    if (_flagIsFaulted) {
                        throw new InvalidOperationException(
                            "Initialization previously failed; this GameHost is in a faulted state and cannot be reused. "
                        );
                    }
                    if (_flagIsInitialized) {
                        throw new InvalidOperationException(
                            "Initialize has already been called on this GameHost. "
                        );
                    }
                    
                    _serviceContent = new Optional<IContentService>(serviceContent);
                    // Snapshot to allow concurrent AddModule calls during loading.
                    modulesToLoad = new List<ILoadable>(_listLoadableModules);
                }
                
                try {
                    foreach (ILoadable module in modulesToLoad) {
                        SafeExecute("LoadContent", module, module.LoadContent);
                    }

                    // All modules succeeded – mark as initialized.
                    lock (_lock) {
                        _flagIsInitialized = true;
                    }
                } catch {
                    // Fault the host on any failure.
                    lock (_lock) {
                        _flagIsFaulted = true;
                    }
                    
                    // Attempt to roll back any partially loaded content.
                    if (_serviceContent.HasValue) {
                        try { _serviceContent.Value.Dispose(); } catch (Exception) { }
                        _serviceContent = default;
                    }
                    throw;
                }
            } finally {
                ExitOperation();
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            EnterOperation();
            try {
                List<IUpdateable> listToIterate;
                bool flagNeedSort = false;
                lock (_lock) {
                    if (_flagIsFaulted)
                        throw new InvalidOperationException("Cannot update a GameHost that is in a faulted state.");
                    if (!_flagIsInitialized)
                        throw new InvalidOperationException("Initialize must be called before Update.");
                    if (_flagUpdateOrderDirty) {
                        flagNeedSort = true;
                        _backBufferUpdateables.Clear();
                        _backBufferUpdateables.AddRange(_listUpdateableModules);
                    }
                }
                if (flagNeedSort) {
                    _backBufferUpdateables.Sort(delegate(IUpdateable a, IUpdateable b) {
                        return a.Order.CompareTo(b.Order);
                    });
                    lock (_lock) {
                        if (_flagUpdateOrderDirty) {
                            // Swap buffers
                            List<IUpdateable> temp = _cachedUpdateablesSorted;
                            _cachedUpdateablesSorted = _backBufferUpdateables;
                            _backBufferUpdateables = temp;
                            _flagUpdateOrderDirty = false;
                        }
                    }
                }
                lock (_lock) {
                    listToIterate = _cachedUpdateablesSorted;
                }
                foreach (IUpdateable module in listToIterate) {
                    if (!module.Enabled) continue;
                    SafeExecute("Update", module, delegate { module.Update(timeFrame); });
                }
            } finally {
                ExitOperation();
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            EnterOperation();
            try {
                List<IDrawable> listToIterate;
                bool flagNeedSort = false;
                lock (_lock) {
                    if (_flagIsFaulted)
                        throw new InvalidOperationException("Cannot draw a GameHost that is in a faulted state.");
                    if (!_flagIsInitialized)
                        throw new InvalidOperationException("Initialize must be called before Draw.");
                    if (_flagDrawOrderDirty) {
                        flagNeedSort = true;
                        _backBufferDrawables.Clear();
                        _backBufferDrawables.AddRange(_listDrawableModules);
                    }
                }
                if (flagNeedSort) {
                    _backBufferDrawables.Sort(delegate(IDrawable a, IDrawable b) {
                        return a.Order.CompareTo(b.Order);
                    });
                    lock (_lock) {
                        if (_flagDrawOrderDirty) {
                            // Swap buffers
                            List<IDrawable> temp = _cachedDrawablesSorted;
                            _cachedDrawablesSorted = _backBufferDrawables;
                            _backBufferDrawables = temp;
                            _flagDrawOrderDirty = false;
                        }
                    }
                }
                lock (_lock) {
                    listToIterate = _cachedDrawablesSorted;
                }
                foreach (IDrawable module in listToIterate) {
                    if (!module.Visible) { continue; }
                    SafeExecute("Draw", module, delegate { module.Draw(timeFrame); });
                }
            } finally {
                ExitOperation();
            }
        }
        
        /// <summary>
        /// Increments the active operation counter and registers the current thread. 
        /// Must be called before any lifecycle method logic. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the host is disposed or being disposed. </exception>
        private void EnterOperation() {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock (_lock) {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(
                        nameof(GameHost),
                        "The GameHost has been disposed. "
                    );
                }
                if (_flagDisposing) {
                    throw new ObjectDisposedException(
                        nameof(GameHost),
                        "The GameHost is in the process of being disposed. "
                    );
                }
                
                _countActiveOperations += 1;
                _setActiveThreads.Add(threadId);
                _eventOperationsIdle.Reset();
            }
        }
        
        /// <summary>
        /// Decrements the active operation counter and removes the current thread.
        /// Must be called in a finally block after lifecycle operations.
        /// </summary>
        private void ExitOperation() {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock (_lock) {
                _setActiveThreads.Remove(threadId);
                _countActiveOperations -= 1;
                if (_countActiveOperations == 0) {
                    _eventOperationsIdle.Set();
                }
            }
        }
        
        /// <summary>
        /// Executes the given action. If an exception occurs, it is first passed 
        /// to the <see cref="OnError"/> handler (if set), then always rethrown. 
        /// If the error handler itself throws, its exception is attached to the 
        /// original exception's <see cref="Exception.Data"/> dictionary for diagnostics. 
        /// </summary>
        /// <param name="nameOperation">The name of the operation (e.g., "Update"). </param>
        /// <param name="module">The module that caused the exception, or null. </param>
        /// <param name="actionToExecute">The delegate to execute. </param>
        private void SafeExecute(string nameOperation, object module, Action actionToExecute) {
            try {
                actionToExecute();
            } catch (Exception exception) {
                // Build context string
                string context = $"{GetType().Name}.{nameOperation}";
                if (module != null) {
                    context = $"{context} (Module: {module.GetType().Name})";
                }
                try {
                    OnError(exception, context);
                } catch (Exception handlerException) {
                    // Store the handler's exception as an object, preserving full stack and inner exceptions.
                    if (!exception.Data.Contains("HandlerException")) {
                        exception.Data["HandlerException"] = handlerException;
                    }
                    
                    // Write a diagnostic trace; does not block the process.
                    System.Diagnostics.Trace.TraceError(
                        $"Error handler threw an exception while processing original error [{exception}] in {context}: {handlerException}"
                    );
                }
                
                // Always rethrow the original exception – the error handler is only a notification.
                throw;
            }
        }
        
        /// <summary>
        /// Disposes the host and all disposable modules, and unloads content. 
        /// Exceptions during individual module disposal or content unload are caught 
        /// and do not prevent disposal of remaining modules. 
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Releases the unmanaged resources used by the host and optionally releases the managed resources. 
        /// </summary>
        /// <param name="flagDisposing">If <c>true</c>, releases both managed and unmanaged resources. </param>
        protected virtual void Dispose(bool flagDisposing) {
            if (_flagDisposed) {
                return;
            }
            
            int idCurrentThread = Thread.CurrentThread.ManagedThreadId;
            
            lock (_lock) {
                // Reentrant call from within an active operation is forbidden.
                if (_setActiveThreads.Contains(idCurrentThread)) {
                    throw new InvalidOperationException(
                        "Cannot dispose GameHost from within Update, Draw, or Initialize. "
                    );
                }
                
                if (_flagDisposed) {
                    return;
                }
                
                // Prevent new operations from entering.
                _flagDisposing = true;
            }
            
            // Wait for all currently active operations to finish without burning CPU cycles.
            _eventOperationsIdle.Wait();
            
            // Dispose all modules that implement IDisposable.
            if (flagDisposing) {
                // Step 1: Dispose all modules first (they may hold references to content resources).
                // Modules are released in reverse registration order so that a module registered
                // early, such as the content module, outlives everything that uses it.
                for (int indexModule = _listAllModules.Count - 1; indexModule >= 0; indexModule -= 1) {
                    object module = _listAllModules[indexModule];
                    if (module is IDisposable disposable) {
                        try {
                            disposable.Dispose();
                        } catch (Exception exception) {
                            string context = $"{GetType().Name}.Dispose (Module: {module.GetType().Name})";
                            try {
                                OnError(exception, context);
                            } catch { }
                        }
                    }
                }
                
                // Step 2: Unload all content (modules have released their references)
                if (_serviceContent.HasValue) {
                    try {
                        _serviceContent.Value.Unload();
                    } catch (Exception exception) {
                        try {
                            OnError(exception, $"{GetType().Name}.Dispose (Content.Unload)");
                        } catch { }
                    }
                }
                
                // Step 3: Dispose the content service itself
                if (_serviceContent.HasValue) {
                    try {
                        _serviceContent.Value.Dispose();
                    } catch (Exception exception) {
                        try {
                            OnError(exception, $"{GetType().Name}.Dispose (Content.Dispose)");
                        } catch { }
                    }
                }
            }
            // Finally mark as fully disposed and clear internal lists.
            lock (_lock) {
                _flagDisposed = true;
                _setModule.Clear();
                _listAllModules.Clear();
                _listLoadableModules.Clear();
                _listUpdateableModules.Clear();
                _listDrawableModules.Clear();
                _cachedUpdateablesSorted.Clear();
                _cachedDrawablesSorted.Clear();
                _backBufferUpdateables.Clear();
                _backBufferDrawables.Clear();
                _flagUpdateOrderDirty = true;
                _flagDrawOrderDirty = true;
            }
        }
    }
}