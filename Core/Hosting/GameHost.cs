using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// <remarks>
    /// Thread safety has three parts. Membership and the lifecycle flags are guarded by one lock. A
    /// schedule rebuild sorts a private copy outside that lock and publishes it by a single reference
    /// assignment, so a published list is immutable and two traversal threads each read a stable
    /// snapshot. Disposal is elected, so exactly one caller releases the participants while any other
    /// caller waits for that release to finish.
    /// <para>
    /// The guarantee stops at the participants. A participant's <c>Enabled</c> or <c>Visible</c>
    /// getter runs on the calling thread outside the callback wrapper, and a callback must not block
    /// waiting for work that needs this host.
    /// </para>
    /// </remarks>
    public class GameHost : IGameHost {
        private readonly object _lock = new object();
        private readonly HashSet<object> _setModule = new HashSet<object>();
        private readonly List<object> _listAllModules = new List<object>();
        private readonly List<ILoadable> _listLoadableModules = new List<ILoadable>();
        // A schedule entry pairs the participant with the order it was admitted under. The host reads
        // that order once, from ScheduleOptions, and never reads an order from the participant again;
        // the entry is the authority for the life of the admission.
        private readonly List<ScheduleUpdateEntry> _listUpdateEntries = new List<ScheduleUpdateEntry>();
        private readonly List<ScheduleDrawEntry> _listDrawEntries = new List<ScheduleDrawEntry>();
        
        // The published schedule. A rebuild constructs a fresh list and assigns it here under the lock;
        // a published list is never sorted, cleared or reused, so a traversal that captured one keeps a
        // stable snapshot for its whole loop. The earlier implementation sorted a shared back buffer and
        // swapped two lists, which let a rebuild recycle the list another traversal was iterating.
        private List<ScheduleUpdateEntry> _listUpdateEntriesPublished = new List<ScheduleUpdateEntry>();
        private List<ScheduleDrawEntry> _listDrawEntriesPublished = new List<ScheduleDrawEntry>();
        
        /// <summary>
        /// One update participant together with the fixed keys the host admitted it under. 
        /// </summary>
        private readonly struct ScheduleUpdateEntry {
            public ScheduleUpdateEntry(IUpdateable moduleUpdateable, int order, int indexAdmission) {
                if (moduleUpdateable == null) {
                    throw new ArgumentNullException(nameof(moduleUpdateable));
                }
                UpdateableModule = moduleUpdateable;
                Order = order;
                AdmissionIndex = indexAdmission;
            }
            
            public IUpdateable UpdateableModule { get; }
            public int Order { get; }
            public int AdmissionIndex { get; }
        }
        
        /// <summary>
        /// One draw participant together with the fixed keys the host admitted it under. 
        /// </summary>
        private readonly struct ScheduleDrawEntry {
            public ScheduleDrawEntry(IDrawable moduleDrawable, int order, int indexAdmission) {
                if (moduleDrawable == null) {
                    throw new ArgumentNullException(nameof(moduleDrawable));
                }
                DrawableModule = moduleDrawable;
                Order = order;
                AdmissionIndex = indexAdmission;
            }
            
            public IDrawable DrawableModule { get; }
            public int Order { get; }
            public int AdmissionIndex { get; }
        }
        
        private Optional<IContentService> _serviceContent;
        
        // The schedule's tie-break key: for each admitted participant, the position at which the
        // composition root admitted it. The entries copy it when they are published, so the traversal
        // compares two stored integers rather than consulting the live map.
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
        // Membership generation, bumped whenever either schedule list changes. A traversal captures it
        // with its snapshot and publishes only if it is unchanged: the dirty flag alone cannot tell
        // "dirty when I took the snapshot" from "dirty after I took it", and committing in the second
        // case would publish the pre-admission schedule and clear the flag, dropping the new participant
        // until the next unrelated membership change.
        private int _generationSchedule = 0;
        
        // Concurrency tracking
        private int _countActiveOperations = 0;
        private readonly HashSet<int> _setActiveThreads = new HashSet<int>();
        private readonly ManualResetEventSlim _eventOperationsIdle = new ManualResetEventSlim(true);
        
        // Shutdown election. Exactly one caller performs the cleanup; every other caller waits for that
        // cleanup to finish instead of running a second release loop over the same participants. A
        // participant whose own Dispose calls back into Dispose on the same thread returns immediately,
        // because waiting there would wait on itself.
        private int _markDisposalElected = 0;
        private int _idDisposer = 0;
        private readonly ManualResetEventSlim _eventDisposeComplete = new ManualResetEventSlim(false);
        
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
        
        /// <summary>
        /// Adds a module to the host, loading it immediately when the host is already initialized.
        /// </summary>
        /// <param name="module">The module to add. </param>
        /// <remarks>
        /// Admission is a tracked host operation, like <see cref="Update"/>, <see cref="Draw"/> and
        /// <see cref="Initialize"/>: a disposal in progress waits for a late load to finish before it
        /// releases participants, and a disposal already in progress rejects the admission. Without that
        /// participation a successful load could publish scheduling membership into a host whose lists
        /// were already cleared.
        /// <para>
        /// A failed late load withdraws the module from membership and rethrows; the host does not call
        /// <see cref="IDisposable.Dispose"/> on it. The host never owned the object — it only scheduled
        /// it — and the caller that received the exception still holds the reference, so releasing what
        /// the failed load acquired is the caller's decision, not a silent ownership transfer. A
        /// participant whose <c>LoadContent</c> can fail should release what it acquired before it
        /// throws.
        /// </para>
        /// </remarks>
        public void AddModule(object module) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            if (module is IUpdateable || module is IDrawable) {
                throw new NotSupportedException(
                    $"The participant {module.GetType().FullName} takes part in a schedule, so its order " +
                    "must be supplied explicitly: AddModule(module, new ScheduleOptions(orderUpdate, orderDraw)). "
                );
            }
            AddModule(module, ScheduleOptions.Default);
        }
        
        /// <summary>
        /// Adds a module with the fixed schedule keys it is admitted under. 
        /// </summary>
        /// <param name="module">The module to add. </param>
        /// <param name="options">The fixed update and draw order for this admission. </param>
        public void AddModule(object module, ScheduleOptions options) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            RejectLegacyOrder(module);
            
            EnterOperation();
            try {
                ILoadable moduleLoadableToLoad = null;
                lock (_lock) {
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
                    
                    if (module is ILoadable moduleLoadable) {
                        _listLoadableModules.Add(moduleLoadable);
                    }
                    
                    // Three admission cases. Before Initialize, membership is published now and the
                    // loading loop will load the module; the frame cannot run yet, so the ordering is
                    // not observable. After Initialize, the module is loaded first and published only
                    // on success, so a frame on another thread never sees an unloaded participant. A
                    // module with nothing to load has no window to hold back and is published at once.
                    if (!_flagIsInitialized) {
                        PublishSchedulingMembership(module, options);
                    } else if (module is ILoadable moduleLoadableToSchedule) {
                        // Published below, once LoadContent has returned.
                        moduleLoadableToLoad = moduleLoadableToSchedule;
                    } else {
                        PublishSchedulingMembership(module, options);
                    }
                }
                
                if (moduleLoadableToLoad != null) {
                    try {
                        SafeExecute("LoadContent", moduleLoadableToLoad, moduleLoadableToLoad.LoadContent);
                    } catch (Exception) {
                        // A failed late admission must not leave an unloaded module behind.
                        RemoveModuleCore(module);
                        throw;
                    }
                    // The load succeeded, so the module may now join the per-frame schedule.
                    PublishSchedulingMembership(module, options);
                }
            } finally {
                ExitOperation();
            }
        }
        
        /// <summary>
        /// Publishes a module's scheduling membership: adds an entry carrying the admitted order to
        /// the update and draw lists and marks the schedule caches dirty so the next traversal rebinds.
        /// </summary>
        /// <param name="module">The module to publish. </param>
        /// <param name="options">The fixed schedule keys supplied at admission. </param>
        /// <returns><c>true</c> if the module participates in a schedule, otherwise <c>false</c>. </returns>
        private bool PublishSchedulingMembership(object module, ScheduleOptions options) {
            bool flagSchedulable = false;
            lock (_lock) {
                int indexAdmission;
                if (!_indexAdmission.TryGetValue(module, out indexAdmission)) {
                    indexAdmission = int.MaxValue;
                }
                if (module is IUpdateable moduleUpdateable) {
                    _listUpdateEntries.Add(new ScheduleUpdateEntry(moduleUpdateable, options.UpdateOrder, indexAdmission));
                    _flagUpdateOrderDirty = true;
                    flagSchedulable = true;
                }
                if (module is IDrawable moduleDrawable) {
                    _listDrawEntries.Add(new ScheduleDrawEntry(moduleDrawable, options.DrawOrder, indexAdmission));
                    _flagDrawOrderDirty = true;
                    flagSchedulable = true;
                }
                if (flagSchedulable) {
                    _generationSchedule += 1;
                }
            }
            return flagSchedulable;
        }
        
        /// <summary>
        /// Rejects a scheduling participant that still exposes a public <c>Order</c> member. 
        /// </summary>
        /// <param name="module">The module being admitted. </param>
        /// <remarks>
        /// Order is a host-owned scheduling input supplied through <see cref="ScheduleOptions"/>. A
        /// participant that still publishes an Order property or field would invite the composition root
        /// to mutate a value the host has already copied, so admission fails instead of accepting a
        /// misleading member. A mutator that is not a public property or field cannot be detected here;
        /// it is harmless because the host never reads a participant's order after admission.
        /// </remarks>
        private static void RejectLegacyOrder(object module) {
            if (!(module is IUpdateable) && !(module is IDrawable)) {
                return;
            }
            Type type = module.GetType();
            PropertyInfo infoProperty = type.GetProperty("Order", BindingFlags.Public | BindingFlags.Instance);
            FieldInfo infoField = type.GetField("Order", BindingFlags.Public | BindingFlags.Instance);
            if (infoProperty != null || infoField != null) {
                throw new NotSupportedException(
                    $"The participant {type.FullName} exposes a public Order member. Schedule order is " +
                    "supplied through ScheduleOptions at admission and is fixed for that admission; " +
                    "remove the Order member and pass its value to AddModule(module, new ScheduleOptions(...)). "
                );
            }
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
                if (module is ILoadable moduleLoadable) {
                    _listLoadableModules.Remove(moduleLoadable);
                }
                if (module is IUpdateable) {
                    _listUpdateEntries.RemoveAll(delegate(ScheduleUpdateEntry entry) {
                        return ReferenceEquals(entry.UpdateableModule, module);
                    });
                }
                if (module is IDrawable) {
                    _listDrawEntries.RemoveAll(delegate(ScheduleDrawEntry entry) {
                        return ReferenceEquals(entry.DrawableModule, module);
                    });
                }
                _flagUpdateOrderDirty = true;
                _flagDrawOrderDirty = true;
                _generationSchedule += 1;
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
                    foreach (ILoadable moduleLoadable in modulesToLoad) {
                        SafeExecute("LoadContent", moduleLoadable, moduleLoadable.LoadContent);
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
                List<ScheduleUpdateEntry> listToIterate;
                List<ScheduleUpdateEntry> listFresh = null;
                int generationSnapshot = 0;
                lock (_lock) {
                    if (_flagIsFaulted) {
                        throw new InvalidOperationException("Cannot update a GameHost that is in a faulted state.");
                    }
                    if (!_flagIsInitialized) {
                        throw new InvalidOperationException("Initialize must be called before Update.");
                    }
                    if (_flagUpdateOrderDirty) {
                        // A rebuild owns its copy. The sort below runs outside the lock on this list
                        // alone, so a concurrent traversal or rebuild cannot observe or disturb it, and
                        // publishing is a single reference assignment rather than a swap of shared
                        // buffers.
                        listFresh = new List<ScheduleUpdateEntry>(_listUpdateEntries);
                        // Captured under the same lock as the snapshot, so the pair describes one
                        // membership state.
                        generationSnapshot = _generationSchedule;
                    }
                }
                if (listFresh != null) {
                    List<ScheduleUpdateEntry> listToSort = listFresh;
                    listToSort.Sort(CompareScheduleUpdateEntry);
                    lock (_lock) {
                        if (_generationSchedule == generationSnapshot) {
                            _listUpdateEntriesPublished = listToSort;
                            _flagUpdateOrderDirty = false;
                        }
                        // Otherwise a participant was admitted or withdrawn while this copy was being
                        // sorted: the flag stays set and the next traversal rebuilds from the current
                        // lists, so the change is delayed by a frame instead of lost.
                    }
                }
                lock (_lock) {
                    listToIterate = _listUpdateEntriesPublished;
                }
                foreach (ScheduleUpdateEntry entry in listToIterate) {
                    IUpdateable moduleUpdateable = entry.UpdateableModule;
                    if (!moduleUpdateable.Enabled) { continue; }
                    SafeExecute("Update", moduleUpdateable, delegate { moduleUpdateable.Update(timeFrame); });
                }
            } finally {
                ExitOperation();
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            EnterOperation();
            try {
                List<ScheduleDrawEntry> listToIterate;
                List<ScheduleDrawEntry> listFresh = null;
                int generationSnapshot = 0;
                lock (_lock) {
                    if (_flagIsFaulted) {
                        throw new InvalidOperationException("Cannot draw a GameHost that is in a faulted state.");
                    }
                    if (!_flagIsInitialized) {
                        throw new InvalidOperationException("Initialize must be called before Draw.");
                    }
                    if (_flagDrawOrderDirty) {
                        // As in Update: a rebuild owns its copy, so the sort runs outside the lock on a
                        // list no other traversal can observe.
                        listFresh = new List<ScheduleDrawEntry>(_listDrawEntries);
                        generationSnapshot = _generationSchedule;
                    }
                }
                if (listFresh != null) {
                    List<ScheduleDrawEntry> listToSort = listFresh;
                    listToSort.Sort(CompareScheduleDrawEntry);
                    lock (_lock) {
                        if (_generationSchedule == generationSnapshot) {
                            _listDrawEntriesPublished = listToSort;
                            _flagDrawOrderDirty = false;
                        }
                    }
                }
                lock (_lock) {
                    listToIterate = _listDrawEntriesPublished;
                }
                foreach (ScheduleDrawEntry entry in listToIterate) {
                    IDrawable moduleDrawable = entry.DrawableModule;
                    if (!moduleDrawable.Visible) { continue; }
                    SafeExecute("Draw", moduleDrawable, delegate { moduleDrawable.Draw(timeFrame); });
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
        /// Orders two update entries: ascending admitted order, and on a tie the position at which
        /// the composition root admitted them.
        /// </summary>
        /// <param name="left">The left entry. </param>
        /// <param name="right">The right entry. </param>
        /// <remarks>
        /// The secondary key is what makes the schedule a <em>total</em> order. Without it, two
        /// participants sharing an order reach an unstable comparison, and <c>List&lt;T&gt;.Sort</c>
        /// is documented as an unstable introspective sort, so their relative traversal order would fall
        /// back to the order the list happened to be built in - an implementation accident rather than a
        /// contract. Admission order supplies the key without consulting the participant's type name, so
        /// a rename cannot move a participant; the key is the order of <c>AddModule</c> calls, which the
        /// composition root writes and can read. A tie remains a reproducible fallback rather than a way
        /// to state intent: distinct orders are the declared form of an intended sequence.
        /// </remarks>
        private static int CompareScheduleUpdateEntry(ScheduleUpdateEntry left, ScheduleUpdateEntry right) {
            int comparison = left.Order.CompareTo(right.Order);
            if (comparison != 0) {
                return comparison;
            }
            return left.AdmissionIndex.CompareTo(right.AdmissionIndex);
        }
        
        /// <summary>
        /// Orders two draw entries: ascending admitted order, and on a tie the admission index.
        /// </summary>
        /// <param name="left">The left entry. </param>
        /// <param name="right">The right entry. </param>
        private static int CompareScheduleDrawEntry(ScheduleDrawEntry left, ScheduleDrawEntry right) {
            int comparison = left.Order.CompareTo(right.Order);
            if (comparison != 0) {
                return comparison;
            }
            return left.AdmissionIndex.CompareTo(right.AdmissionIndex);
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
        /// Reports a failure raised while releasing one resource, without letting the report stop the
        /// remaining releases.
        /// </summary>
        /// <param name="exception">The release failure. </param>
        /// <param name="context">The operation and participant the failure belongs to. </param>
        /// <remarks>
        /// This is the cleanup counterpart of <see cref="SafeExecute"/>: there the original exception is
        /// rethrown, here it is consumed because a shutdown must still attempt every remaining release. A
        /// handler that itself throws is recorded on the original exception and traced, so the failure of
        /// the report cannot abort the loop.
        /// </remarks>
        private void ReportCleanupFailure(Exception exception, string context) {
            try {
                OnError(exception, context);
            } catch (Exception handlerException) {
                if (!exception.Data.Contains("HandlerException")) {
                    exception.Data["HandlerException"] = handlerException;
                }
                System.Diagnostics.Trace.TraceError(
                    $"Error handler threw an exception while processing cleanup failure [{exception}] in {context}: {handlerException}"
                );
            }
        }
        
        /// <summary>
        /// Disposes the host and all disposable modules, and unloads content. 
        /// Exactly one caller performs the release and every other caller waits for it to finish; each
        /// release is attempted even when an earlier one fails, and a release failure is reported rather
        /// than propagated. 
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
            int idCurrentThread = Thread.CurrentThread.ManagedThreadId;
            
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                // Reentrant call from within an active operation is forbidden.
                if (_setActiveThreads.Contains(idCurrentThread)) {
                    throw new InvalidOperationException(
                        "Cannot dispose GameHost from within Update, Draw, Initialize or a module admission. "
                    );
                }
                // A participant whose own Dispose calls back into Dispose on this thread must not wait
                // for the cleanup it is itself running.
                if (_idDisposer == idCurrentThread) {
                    return;
                }
            }
            
            if (Interlocked.Exchange(ref _markDisposalElected, 1) != 0) {
                // Another caller is already releasing the participants. Waiting makes Dispose mean "the
                // resources are released when this returns" and keeps a second release loop from running
                // over the same participants.
                _eventDisposeComplete.Wait();
                return;
            }
            
            lock (_lock) {
                _idDisposer = idCurrentThread;
                // Prevent new operations from entering.
                _flagDisposing = true;
            }
            
            try {
                // Wait for all currently active operations to finish without burning CPU cycles.
                _eventOperationsIdle.Wait();
                
                // Dispose all modules that implement IDisposable.
                if (flagDisposing) {
                    // Step 1: Dispose all modules first (they may hold references to content resources).
                    // Modules are released in reverse registration order so that a module registered
                    // early, such as the content module, outlives everything that uses it. The list is
                    // snapshotted first so that a participant's cleanup cannot disturb the loop that is
                    // releasing it.
                    object[] modules;
                    lock (_lock) {
                        modules = _listAllModules.ToArray();
                    }
                    for (int indexModule = modules.Length - 1; indexModule >= 0; indexModule -= 1) {
                        object module = modules[indexModule];
                        if (module is IDisposable disposable) {
                            try {
                                disposable.Dispose();
                            } catch (Exception exception) {
                                ReportCleanupFailure(
                                    exception, 
                                    $"{GetType().Name}.Dispose (Module: {module.GetType().Name})"
                                );
                            }
                        }
                    }
                    
                    // Step 2: Unload all content (modules have released their references)
                    if (_serviceContent.HasValue) {
                        try {
                            _serviceContent.Value.Unload();
                        } catch (Exception exception) {
                            ReportCleanupFailure(exception, $"{GetType().Name}.Dispose (Content.Unload)");
                        }
                    }
                    
                    // Step 3: Dispose the content service itself
                    if (_serviceContent.HasValue) {
                        try {
                            _serviceContent.Value.Dispose();
                        } catch (Exception exception) {
                            ReportCleanupFailure(exception, $"{GetType().Name}.Dispose (Content.Dispose)");
                        }
                    }
                }
            } finally {
                // Finally mark as fully disposed and clear internal lists. The published schedules are
                // replaced rather than cleared: a published list is guaranteed immutable, so even a
                // reference that outlived its traversal sees a consistent (stale) snapshot.
                lock (_lock) {
                    _flagDisposed = true;
                    _setModule.Clear();
                    _listAllModules.Clear();
                    _indexAdmission.Clear();
                    _listLoadableModules.Clear();
                    _listUpdateEntries.Clear();
                    _listDrawEntries.Clear();
                    _listUpdateEntriesPublished = new List<ScheduleUpdateEntry>();
                    _listDrawEntriesPublished = new List<ScheduleDrawEntry>();
                    _flagUpdateOrderDirty = true;
                    _flagDrawOrderDirty = true;
                    _generationSchedule += 1;
                }
                _eventDisposeComplete.Set();
            }
        }
    }
}
