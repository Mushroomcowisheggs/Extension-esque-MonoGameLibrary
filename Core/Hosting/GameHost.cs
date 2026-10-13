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
    /// The host holds no service registry: it schedules the participants it is given and exposes no
    /// route back to the capabilities those participants were constructed with.
    /// </summary>
    public class GameHost : IGameHost {
        private readonly object _lock = new object();
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
        
        // The schedule's tie-break key: for each admitted participant, the position at which the
        // composition root admitted it. See CompareScheduleOrder for why the type name is not used.
        private readonly Dictionary<object, int> _indexAdmission = new Dictionary<object, int>();
        private int _countAdmitted = 0;
        
        // Lifecycle state flags
        private bool _flagIsInitialized = false;
        private bool _flagIsLoading = false;
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
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GameHost"/> class. 
        /// </summary>
        /// <remarks>
        /// The host is constructed without the service registry on purpose. The capability set is
        /// resolved and wired by the composition root before the host exists, and a participant
        /// receives what it needs through constructor injection; there is deliberately no path from a
        /// participant back to the registry at run time.
        /// </remarks>
        public GameHost() {
        }
        
        /// <inheritdoc />
        public void AddModule(object module) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            
            ILoadable loadableToLoad = null;
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
                // Admission is publish-after-load, so the module's own dependencies must be settled
                // before it is added. A module that registers a further module while the host is
                // loading would be loaded by the calling thread instead of by Initialize's loop,
                // which would put two threads inside the content services at once.
                if (_flagIsLoading) {
                    throw new InvalidOperationException(
                        "Cannot add modules while the GameHost is initializing. Add every module before " +
                        "Initialize, or add it after Initialize returns. "
                    );
                }
                
                if (_setModule.Contains(module)) {
                    throw new InvalidOperationException(
                        $"The module {module.GetType().FullName} has already been added. "
                    );
                }
                
                _setModule.Add(module);
                _listAllModules.Add(module);
                
                // The admission index records where the composition root placed this participant among
                // all admitted modules. It is the schedule's tie-break key instead of the type name, so
                // that renaming a participant cannot move it in the frame. It is not decremented when a
                // module is withdrawn: the index stays unique for the life of the host.
                _indexAdmission[module] = _countAdmitted;
                _countAdmitted += 1;
                
                if (module is ILoadable loadable) {
                    _listLoadableModules.Add(loadable);
                }
                
                // Three admission cases. Before Initialize, membership is published now and the
                // loading loop will load the module; the frame cannot run yet, so the ordering is
                // not observable. After Initialize, the module is loaded first and published only
                // on success, so a frame on another thread never sees an unloaded participant. A
                // module with nothing to load has no window to hold back and is published at once.
                if (!_flagIsInitialized) {
                    PublishSchedulingMembership(module);
                } else if (module is ILoadable loadableToSchedule) {
                    // Published below, once LoadContent has returned.
                    loadableToLoad = loadableToSchedule;
                } else {
                    PublishSchedulingMembership(module);
                }
            }
            
            if (loadableToLoad != null) {
                try {
                    SafeExecute("LoadContent", loadableToLoad, loadableToLoad.LoadContent);
                } catch (Exception) {
                    // A failed late admission must not leave an unloaded module behind.
                    RemoveModuleCore(module);
                    throw;
                }
                // The load succeeded, so the module may now join the per-frame schedule.
                PublishSchedulingMembership(module);
            }
        }
        
        /// <summary>
        /// Publishes a module's scheduling membership: adds it to the update and draw lists and
        /// marks the schedule caches dirty so the next traversal rebinds.
        /// </summary>
        /// <param name="module">The module to publish. </param>
        /// <returns><c>true</c> if the module participates in a schedule, otherwise <c>false</c>. </returns>
        private bool PublishSchedulingMembership(object module) {
            bool flagSchedulable = false;
            lock (_lock) {
                if (module is IUpdateable updateable) {
                    _listUpdateableModules.Add(updateable);
                    _flagUpdateOrderDirty = true;
                    flagSchedulable = true;
                }
                if (module is IDrawable drawable) {
                    _listDrawableModules.Add(drawable);
                    _flagDrawOrderDirty = true;
                    flagSchedulable = true;
                }
            }
            return flagSchedulable;
        }
        
        /// <summary>
        /// Removes a module from the host and every category list. Used to roll back a failed 
        /// post-initialization admission. 
        /// </summary>
        /// <param name="module">The module to remove. </param>
        /// <remarks>
        /// The scheduling caches are invalidated unconditionally rather than only when a removal
        /// actually happened, because a rollback that leaves a cache holding a withdrawn participant
        /// is exactly the failure this method exists to prevent. The admission counter is not
        /// rewound: an index consumed by a withdrawn module is never handed to another participant.
        /// </remarks>
        private void RemoveModuleCore(object module) {
            lock (_lock) {
                _setModule.Remove(module);
                _listAllModules.Remove(module);
                _indexAdmission.Remove(module);
                if (module is ILoadable loadable) {
                    _listLoadableModules.Remove(loadable);
                }
                if (module is IUpdateable updateable) {
                    _listUpdateableModules.Remove(updateable);
                }
                if (module is IDrawable drawable) {
                    _listDrawableModules.Remove(drawable);
                }
                _flagUpdateOrderDirty = true;
                _flagDrawOrderDirty = true;
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
                    if (_flagIsLoading) {
                        throw new InvalidOperationException(
                            "Initialize is already running on this GameHost. "
                        );
                    }
                    
                    _serviceContent = new Optional<IContentService>(serviceContent);
                    _flagIsLoading = true;
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
                } catch (Exception) {
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
                } finally {
                    lock (_lock) {
                        _flagIsLoading = false;
                    }
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
                Dictionary<object, int> indicesAdmission = null;
                lock (_lock) {
                    if (_flagIsFaulted) {
                        throw new InvalidOperationException("Cannot update a GameHost that is in a faulted state.");
                    }
                    if (!_flagIsInitialized) {
                        throw new InvalidOperationException("Initialize must be called before Update.");
                    }
                    if (_flagUpdateOrderDirty) {
                        flagNeedSort = true;
                        _backBufferUpdateables.Clear();
                        _backBufferUpdateables.AddRange(_listUpdateableModules);
                        // The sort below runs outside the lock, so it reads this snapshot of the
                        // admission indices rather than the live map, which AddModule may be writing.
                        indicesAdmission = new Dictionary<object, int>(_indexAdmission);
                    }
                }
                if (flagNeedSort) {
                    Dictionary<object, int> indicesSort = indicesAdmission;
                    _backBufferUpdateables.Sort(delegate(IUpdateable a, IUpdateable b) {
                        return CompareScheduleOrder(a.Order, a, b.Order, b, indicesSort);
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
                    if (!module.Enabled) { continue; }
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
                Dictionary<object, int> indicesAdmission = null;
                lock (_lock) {
                    if (_flagIsFaulted) {
                        throw new InvalidOperationException("Cannot draw a GameHost that is in a faulted state.");
                    }
                    if (!_flagIsInitialized) {
                        throw new InvalidOperationException("Initialize must be called before Draw.");
                    }
                    if (_flagDrawOrderDirty) {
                        flagNeedSort = true;
                        _backBufferDrawables.Clear();
                        _backBufferDrawables.AddRange(_listDrawableModules);
                        // As in Update: the sort runs outside the lock and reads this snapshot.
                        indicesAdmission = new Dictionary<object, int>(_indexAdmission);
                    }
                }
                if (flagNeedSort) {
                    Dictionary<object, int> indicesSort = indicesAdmission;
                    _backBufferDrawables.Sort(delegate(IDrawable a, IDrawable b) {
                        return CompareScheduleOrder(a.Order, a, b.Order, b, indicesSort);
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
        /// Orders two participants for the per-frame schedule: ascending <see cref="IUpdateable.Order"/>,
        /// and on a tie the position at which the composition root admitted them.
        /// </summary>
        /// <param name="orderLeft">The left participant's <c>Order</c>. </param>
        /// <param name="participantLeft">The left participant. </param>
        /// <param name="orderRight">The right participant's <c>Order</c>. </param>
        /// <param name="participantRight">The right participant. </param>
        /// <param name="indicesAdmission">The admission-index snapshot the sort is reading. </param>
        /// <remarks>
        /// The secondary key is what makes the schedule a <em>total</em> order. Without it, two
        /// participants sharing an <c>Order</c> reach an unstable comparison, and <c>List&lt;T&gt;.Sort</c>
        /// is documented as an unstable introspective sort, so their relative traversal order would fall
        /// back to the order the list happened to be built in - an implementation accident rather than a
        /// contract. Admission order supplies the key without consulting the participant's type name, so
        /// a rename cannot move a participant; the key is the order of <c>AddModule</c> calls, which the
        /// composition root writes and can read. A tie remains a reproducible fallback rather than a way
        /// to state intent: distinct <c>Order</c> values are the declared form of an intended sequence.
        /// </remarks>
        private static int CompareScheduleOrder(
            int orderLeft,
            object participantLeft,
            int orderRight,
            object participantRight,
            Dictionary<object, int> indicesAdmission
        ) {
            int comparison = orderLeft.CompareTo(orderRight);
            if (comparison != 0) {
                return comparison;
            }
            return GetAdmissionIndex(indicesAdmission, participantLeft).CompareTo(
                GetAdmissionIndex(indicesAdmission, participantRight)
            );
        }
        
        /// <summary>
        /// Reads a participant's admission index out of a schedule snapshot. 
        /// </summary>
        /// <param name="indicesAdmission">The admission-index snapshot. </param>
        /// <param name="participant">The participant to look up. </param>
        /// <returns>The admission index, or <see cref="int.MaxValue"/> if the snapshot does not hold one. </returns>
        /// <remarks>
        /// The snapshot is taken under the same lock that admits participants, so a missing entry is not
        /// expected; the fallback assigns a defined position instead of an accidental one.
        /// </remarks>
        private static int GetAdmissionIndex(Dictionary<object, int> indicesAdmission, object participant) {
            int index;
            if (indicesAdmission != null && indicesAdmission.TryGetValue(participant, out index)) {
                return index;
            }
            return int.MaxValue;
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
                            } catch (Exception) { }
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
                        } catch (Exception) { }
                    }
                }
                
                // Step 3: Dispose the content service itself
                if (_serviceContent.HasValue) {
                    try {
                        _serviceContent.Value.Dispose();
                    } catch (Exception exception) {
                        try {
                            OnError(exception, $"{GetType().Name}.Dispose (Content.Dispose)");
                        } catch (Exception) { }
                    }
                }
            }
            // Finally mark as fully disposed and clear internal lists.
            lock (_lock) {
                _flagDisposed = true;
                _setModule.Clear();
                _listAllModules.Clear();
                _indexAdmission.Clear();
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
