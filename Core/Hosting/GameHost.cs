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
    /// caller waits for that release to finish. Each release also elects through an ownership ledger,
    /// so a participant is released at most once per object for the life of the host, including when
    /// it was withdrawn and admitted again; membership, admission indices and the ledger all key on
    /// reference identity. Withdrawal ends participation only: the host keeps ownership, so the
    /// object may return to the schedule and is still released once at disposal.
    /// <para>
    /// The guarantee stops at the participants. A participant's <c>Enabled</c> or <c>Visible</c>
    /// getter runs on the calling thread outside the callback wrapper, and a callback must not block
    /// waiting for work that needs this host.
    /// </para>
    /// </remarks>
    public class GameHost : IGameHost {
        private readonly object _lock = new object();
        private readonly HashSet<object> _setModule = new HashSet<object>(ReferenceEqualityComparer.Instance);
        // Every object the host owns, admitted or withdrawn, in the order ownership was taken. The
        // list outlives a withdrawal: withdrawal ends participation, not ownership, which is what
        // lets the object come back and still be released exactly once at disposal.
        private readonly List<object> _listOwned = new List<object>();
        // The same objects as a set, so "this host has already taken ownership" is one lookup rather
        // than a scan, and so the answer does not depend on the ledger. The ledger records ownership
        // across a whole composition: a builder records a module before this host exists, so a ledger
        // state of Owned at AddModule means "some composition object owns it", not "this host does".
        private readonly HashSet<object> _setOwned = new HashSet<object>(ReferenceEqualityComparer.Instance);
        private readonly List<ILoadable> _listLoadableModules = new List<ILoadable>();
        // Participants whose one-time load already returned. A withdrawal does not unload, so a
        // participant that comes back must not run LoadContent a second time and double every asset
        // it acquired; the set is keyed by reference identity for the same reason membership is.
        private readonly HashSet<object> _setLoaded = new HashSet<object>(ReferenceEqualityComparer.Instance);
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
            public ScheduleUpdateEntry(IUpdateable participantUpdateable, int order, int indexAdmission) {
                if (participantUpdateable == null) {
                    throw new ArgumentNullException(nameof(participantUpdateable));
                }
                UpdateableParticipant = participantUpdateable;
                Order = order;
                AdmissionIndex = indexAdmission;
            }
            
            public IUpdateable UpdateableParticipant { get; }
            public int Order { get; }
            public int AdmissionIndex { get; }
        }
        
        /// <summary>
        /// One draw participant together with the fixed keys the host admitted it under. 
        /// </summary>
        private readonly struct ScheduleDrawEntry {
            public ScheduleDrawEntry(IDrawable participantDrawable, int order, int indexAdmission) {
                if (participantDrawable == null) {
                    throw new ArgumentNullException(nameof(participantDrawable));
                }
                DrawableParticipant = participantDrawable;
                Order = order;
                AdmissionIndex = indexAdmission;
            }
            
            public IDrawable DrawableParticipant { get; }
            public int Order { get; }
            public int AdmissionIndex { get; }
        }
        
        // Late-bound lifecycle input. Optional<T> belongs on dependency-facing APIs, not on a field
        // that Initialize assigns once and Dispose clears.
        private IContentService _serviceContent;
        
        // The schedule's tie-break key: for each admitted participant, the position at which the
        // composition root admitted it. The entries copy it when they are published, so the traversal
        // compares two stored integers rather than consulting the live map.
        private readonly Dictionary<object, int> _indexAdmission = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        private int _countAdmitted = 0;
        
        // Lifecycle state flags
        private bool _flagIsInitialized = false;
        private bool _flagIsLoading = false;
        private bool _flagIsFaulted = false;
        private bool _flagDisposing = false;
        private bool _flagDisposed = false;
        
        private readonly FailurePolicy _policyFailure;
        // Ownership is recorded per object rather than per admission. A withdrawal leaves the state
        // Owned, so the participant can be admitted again; a release leaves it Released, so it cannot.
        // That is what makes "released once" a property of the object and not of one admission.
        private readonly OwnershipLedger _ledgerOwnership;
        private LifecycleFailure _failure;
        
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
        /// Gets the failure policy selected when the host was built.
        /// </summary>
        public FailurePolicy FailurePolicy {
            get { return _policyFailure; }
        }
        
        /// <summary>
        /// Gets the diagnostic record of the last failed initialization or admission, or null.
        /// </summary>
        public LifecycleFailure Failure {
            get { lock (_lock) { return _failure; } }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GameHost"/> class. 
        /// </summary>
        /// <param name="policyFailure">The required failure policy. </param>
        /// <remarks>
        /// The host is constructed without the service registry on purpose. The capability set is
        /// resolved and wired by the composition root before the host exists, and a participant
        /// receives what it needs through constructor injection; there is deliberately no path from a
        /// participant back to the registry at run time.
        /// </remarks>
        public GameHost(FailurePolicy policyFailure)
        : this(policyFailure, new OwnershipLedger()) {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GameHost"/> class with the ownership ledger
        /// of the composition that built it.
        /// </summary>
        /// <param name="policyFailure">The required failure policy. </param>
        /// <param name="ledgerOwnership">The ledger that records per-object ownership. </param>
        /// <remarks>
        /// A <see cref="GameBuilder"/> passes the ledger it already used, so a failed build and the
        /// host it aborts cannot both release one object: they elect through the same record.
        /// </remarks>
        internal GameHost(FailurePolicy policyFailure, OwnershipLedger ledgerOwnership) {
            if (policyFailure != FailurePolicy.Release && policyFailure != FailurePolicy.Retain) {
                throw new ArgumentOutOfRangeException(nameof(policyFailure));
            }
            if (ledgerOwnership == null) {
                throw new ArgumentNullException(nameof(ledgerOwnership));
            }
            _policyFailure = policyFailure;
            _ledgerOwnership = ledgerOwnership;
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
        /// A failed late load withdraws the module from membership and rethrows. Under
        /// <see cref="FailurePolicy.Release"/> the host disposes it; under
        /// <see cref="FailurePolicy.Retain"/> the module stays in <see cref="Failure"/> and the caller
        /// owns it. The host remains initialized in both cases. A disposal or cleanup failure during
        /// release is reported through <see cref="OnError"/>.
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
                ILoadable moduleLoadable = null;
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
                    
                    OwnershipState statePrevious;
                    if (!_ledgerOwnership.TryRecordAdmission(module, out statePrevious)) {
                        throw new InvalidOperationException(
                            $"The module {module.GetType().FullName} cannot be admitted because " +
                            $"{OwnershipLedger.Describe(statePrevious)}. "
                        );
                    }
                    
                    _setModule.Add(module);
                    if (_setOwned.Add(module)) {
                        // The first admission that reaches this host is the point at which the host
                        // takes ownership and records the position the reverse release pass needs.
                        // A re-admission after a withdrawal is already in the list, at the position
                        // where the host first took the object.
                        _listOwned.Add(module);
                    }
                    
                    // The admission index records where the composition root placed this participant among
                    // all admitted modules. It is the schedule's tie-break key instead of the type name, so
                    // that renaming a participant cannot move it in the frame. It is not decremented when a
                    // module is withdrawn: the index stays unique for the life of the host.
                    _indexAdmission[module] = _countAdmitted;
                    _countAdmitted += 1;
                    
                    if (module is ILoadable candidateModuleLoadable) {
                        _listLoadableModules.Add(candidateModuleLoadable);
                    }
                    
                    // Three admission cases. Before Initialize, membership is published now and the
                    // loading loop will load the module; the frame cannot run yet, so the ordering is
                    // not observable. After Initialize, the module is loaded first and published only
                    // on success, so a frame on another thread never sees an unloaded participant. A
                    // module with nothing to load has no window to hold back and is published at once.
                    if (!_flagIsInitialized) {
                        PublishSchedulingMembership(module, options);
                    } else if (module is ILoadable scheduledModuleLoadable) {
                        // A participant that already loaded once (it was withdrawn and is coming back)
                        // is not loaded again: the loading phase is one-time per participant, and a
                        // second LoadContent would double every asset it acquired.
                        if (_setLoaded.Contains(module)) {
                            PublishSchedulingMembership(module, options);
                        } else {
                            // Published below, once LoadContent has returned.
                            moduleLoadable = scheduledModuleLoadable;
                        }
                    } else {
                        PublishSchedulingMembership(module, options);
                    }
                }
                
                if (moduleLoadable != null) {
                    try {
                        SafeExecute("LoadContent", moduleLoadable, moduleLoadable.LoadContent);
                    } catch (Exception exception) {
                        // Withdraw the module before deciding what the explicit policy does with it.
                        RemoveModuleCore(module, flagKeepOwnership: false);
                        if (_policyFailure == FailurePolicy.Release) {
                            OwnershipRelease.ReleaseModule(
                                module,
                                ReportCleanupFailure,
                                $"{GetType().Name}.AddModule",
                                _ledgerOwnership
                            );
                        }
                        List<object> listRetained = new List<object>();
                        if (_policyFailure == FailurePolicy.Retain) {
                            // The caller owns the withdrawn participant from here on, so the transfer
                            // is recorded instead of being inferred from the stage string.
                            _ledgerOwnership.RecordTransferToCaller(module);
                            listRetained.Add(module);
                        }
                        lock (_lock) {
                            _failure = new LifecycleFailure(
                                "GameHost.AddModule",
                                exception,
                                _policyFailure,
                                listRetained,
                                new List<Exception>()
                            );
                        }
                        throw;
                    }
                    // The load succeeded, so the module may now join the per-frame schedule.
                    lock (_lock) {
                        _setLoaded.Add(module);
                    }
                    PublishSchedulingMembership(module, options);
                }
            } finally {
                ExitOperation();
            }
        }
        
        /// <inheritdoc />
        /// <remarks>
        /// Withdrawal is the documented way to reprioritise a participant, and it is deliberately not
        /// release. Participation ends, ownership does not: the module leaves the schedule, its
        /// admission index is consumed, and the host still owns it and releases it once at disposal.
        /// Because the ownership ledger is untouched, the same object may be admitted again; the new
        /// admission mints a new index, which is what moves it in the frame. A frame that captured a
        /// published snapshot before the withdrawal may still call the module once, because the
        /// snapshot is immutable by design.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the module is not admitted or was
        /// withdrawn already, the host is initializing, or the host is faulted. </exception>
        public void WithdrawModule(object module) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            
            EnterOperation();
            try {
                lock (_lock) {
                    if (_flagIsFaulted) {
                        throw new InvalidOperationException(
                            "Cannot withdraw modules from a GameHost that is in a faulted state. "
                        );
                    }
                    if (_flagIsLoading) {
                        throw new InvalidOperationException(
                            "Cannot withdraw modules while the GameHost is initializing. The loading loop " +
                            "iterates a snapshot and would still load a module this call removed. "
                        );
                    }
                    if (!_setModule.Contains(module)) {
                        if (HasOwned(module)) {
                            throw new InvalidOperationException(
                                $"The module {module.GetType().FullName} is not admitted: it was withdrawn already. "
                            );
                        }
                        throw new InvalidOperationException(
                            $"The module {module.GetType().FullName} is not admitted by this host. "
                        );
                    }
                    RemoveModuleCore(module, flagKeepOwnership: true);
                }
            } finally {
                ExitOperation();
            }
        }
        
        /// <summary>
        /// Reports whether the host has taken ownership of an object, whether or not it is still
        /// admitted. Callers hold the host lock.
        /// </summary>
        /// <param name="module">The object to look up. </param>
        /// <returns><c>true</c> if the object appears in the ownership list. </returns>
        private bool HasOwned(object module) {
            for (int index = 0; index < _listOwned.Count; index += 1) {
                if (ReferenceEquals(_listOwned[index], module)) {
                    return true;
                }
            }
            return false;
        }
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
                if (module is IUpdateable participantUpdateable) {
                    _listUpdateEntries.Add(new ScheduleUpdateEntry(participantUpdateable, options.UpdateOrder, indexAdmission));
                    _flagUpdateOrderDirty = true;
                    flagSchedulable = true;
                }
                if (module is IDrawable participantDrawable) {
                    _listDrawEntries.Add(new ScheduleDrawEntry(participantDrawable, options.DrawOrder, indexAdmission));
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
        /// Removes a module from the schedule and from every participation list. Used to roll back a
        /// failed post-initialization admission and by <see cref="WithdrawModule"/>.
        /// </summary>
        /// <param name="module">The module to remove. </param>
        /// <param name="flagKeepOwnership">If <c>true</c>, the host keeps ownership and still releases
        /// the object at disposal. A failed admission passes <c>false</c>, because that path either
        /// releases the object or transfers it to the caller. </param>
        /// <remarks>
        /// The scheduling caches are invalidated unconditionally rather than only when a removal
        /// actually happened, because a rollback that leaves a cache holding a withdrawn participant
        /// is exactly the failure this method exists to prevent. The admission counter is not
        /// rewound: an index consumed by a withdrawn module is never handed to another participant.
        /// </remarks>
        private void RemoveModuleCore(object module, bool flagKeepOwnership) {
            lock (_lock) {
                _setModule.Remove(module);
                if (!flagKeepOwnership) {
                    _listOwned.RemoveAll(delegate(object candidate) {
                        return ReferenceEquals(candidate, module);
                    });
                    _setOwned.Remove(module);
                }
                _indexAdmission.Remove(module);
                if (module is ILoadable moduleLoadable) {
                    _listLoadableModules.RemoveAll(delegate(ILoadable candidate) {
                        return ReferenceEquals(candidate, moduleLoadable);
                    });
                }
                if (module is IUpdateable) {
                    _listUpdateEntries.RemoveAll(delegate(ScheduleUpdateEntry entry) {
                        return ReferenceEquals(entry.UpdateableParticipant, module);
                    });
                }
                if (module is IDrawable) {
                    _listDrawEntries.RemoveAll(delegate(ScheduleDrawEntry entry) {
                        return ReferenceEquals(entry.DrawableParticipant, module);
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
                    
                    OwnershipState stateContent;
                    if (!_ledgerOwnership.TryRecordAdmission(serviceContent, out stateContent)) {
                        throw new InvalidOperationException(
                            $"The content service cannot be initialized because " +
                            $"{OwnershipLedger.Describe(stateContent)}. "
                        );
                    }
                    
                    _serviceContent = serviceContent;
                    _flagIsLoading = true;
                    // Snapshot to allow concurrent AddModule calls during loading.
                    modulesToLoad = new List<ILoadable>(_listLoadableModules);
                }
                
                try {
                    foreach (ILoadable moduleLoadable in modulesToLoad) {
                        SafeExecute("LoadContent", moduleLoadable, moduleLoadable.LoadContent);
                        lock (_lock) {
                            _setLoaded.Add(moduleLoadable);
                        }
                    }
                    
                    // All modules succeeded – mark as initialized.
                    lock (_lock) {
                        _flagIsInitialized = true;
                    }
                } catch (Exception exception) {
                    if (_policyFailure == FailurePolicy.Release) {
                        ReleaseAfterFailedInitialization(exception);
                    } else {
                        RetainAfterFailedInitialization(exception);
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
                    IUpdateable participantUpdateable = entry.UpdateableParticipant;
                    if (!participantUpdateable.Enabled) { continue; }
                    SafeExecute("Update", participantUpdateable, timeFrame, participantUpdateable.Update);
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
                    IDrawable participantDrawable = entry.DrawableParticipant;
                    if (!participantDrawable.Visible) { continue; }
                    SafeExecute("Draw", participantDrawable, timeFrame, participantDrawable.Draw);
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
                ReportSafeExecuteFailure(nameOperation, module, exception);
                throw;
            }
        }
        
        private void SafeExecute<T>(string nameOperation, object module, T argument, Action<T> actionToExecute) {
            try {
                actionToExecute(argument);
            } catch (Exception exception) {
                ReportSafeExecuteFailure(nameOperation, module, exception);
                throw;
            }
        }
        
        private void ReportSafeExecuteFailure(string nameOperation, object module, Exception exception) {
            // Build context string
            string context = $"{GetType().Name}.{nameOperation}";
            if (module != null) {
                context = $"{context} (Module: {module.GetType().Name})";
            }
            try {
                OnError(exception, context);
            } catch (Exception exceptionHandler) {
                // Store the handler's exception as an object, preserving full stack and inner exceptions.
                if (!exception.Data.Contains("HandlerException")) {
                    exception.Data["HandlerException"] = exceptionHandler;
                }
                
                // Write a diagnostic trace; does not block the process.
                System.Diagnostics.Trace.TraceError(
                    $"Error handler threw an exception while processing original error [{exception}] in {context}: {exceptionHandler}"
                );
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
            } catch (Exception exceptionHandler) {
                if (!exception.Data.Contains("HandlerException")) {
                    exception.Data["HandlerException"] = exceptionHandler;
                }
                System.Diagnostics.Trace.TraceError(
                    $"Error handler threw an exception while processing cleanup failure [{exception}] in {context}: {exceptionHandler}"
                );
            }
        }
        
        /// <summary>
        /// Detaches all admissions without releasing them and returns them in admission order.
        /// Used by a failed <see cref="GameBuilder.Build"/>, where the builder and the aborted host
        /// must release the union of their admissions exactly once.
        /// </summary>
        internal object[] DetachAdmissions() {
            lock (_lock) {
                if (_flagDisposed) {
                    return new object[0];
                }
                object[] modules = _listOwned.ToArray();
                _flagDisposed = true;
                _flagDisposing = false;
                _setModule.Clear();
                _listOwned.Clear();
                _setOwned.Clear();
                _listLoadableModules.Clear();
                _setLoaded.Clear();
                _indexAdmission.Clear();
                _listUpdateEntries.Clear();
                _listDrawEntries.Clear();
                _listUpdateEntriesPublished = new List<ScheduleUpdateEntry>();
                _listDrawEntriesPublished = new List<ScheduleDrawEntry>();
                return modules;
            }
        }
        
        /// <summary>
        /// Releases the modules and content the host owns. Callers guarantee this is entered once.
        /// </summary>
        /// <param name="context">The operation context used for reporting. </param>
        private void ReleaseOwnedResources(string context) {
            object[] modules;
            IContentService serviceContent;
            lock (_lock) {
                modules = _listOwned.ToArray();
                serviceContent = _serviceContent;
            }
            
            OwnershipRelease.ReleaseModules(modules, ReportCleanupFailure, context, _ledgerOwnership);
            
            if (serviceContent != null) {
                OwnershipState stateContent;
                if (!_ledgerOwnership.TryElectRelease(serviceContent, out stateContent)) {
                    ReportCleanupFailure(
                        new InvalidOperationException(
                            $"The content service was not released because " +
                            $"{OwnershipLedger.Describe(stateContent)}. "
                        ),
                        $"{context} (Content)"
                    );
                    return;
                }
                try {
                    serviceContent.Unload();
                } catch (Exception exception) {
                    ReportCleanupFailure(exception, $"{context} (Content.Unload)");
                }
                try {
                    serviceContent.Dispose();
                } catch (Exception exception) {
                    ReportCleanupFailure(exception, $"{context} (Content.Dispose)");
                }
            }
        }
        
        /// <summary>
        /// Releases everything a failed <see cref="Initialize"/> accepted, then marks the host
        /// disposed. If another caller is already elected to dispose, that caller performs the
        /// release after this operation exits.
        /// </summary>
        private void ReleaseAfterFailedInitialization(Exception exception) {
            int idCurrentThread = Thread.CurrentThread.ManagedThreadId;
            lock (_lock) {
                _failure = new LifecycleFailure(
                    "GameHost.Initialize",
                    exception,
                    FailurePolicy.Release,
                    new List<object>(),
                    new List<Exception>()
                );
                _flagIsFaulted = true;
                if (_flagDisposed) {
                    return;
                }
                if (Interlocked.CompareExchange(ref _markDisposalElected, 1, 0) != 0) {
                    return;
                }
                _idDisposer = idCurrentThread;
                _flagDisposing = true;
            }
            
            try {
                ReleaseOwnedResources($"{GetType().Name}.Initialize");
            } finally {
                lock (_lock) {
                    _flagIsInitialized = false;
                    _flagIsFaulted = true;
                    _flagDisposing = false;
                    _flagDisposed = true;
                    _serviceContent = null;
                    _setModule.Clear();
                    _listOwned.Clear();
                    _setOwned.Clear();
                    _indexAdmission.Clear();
                    _listLoadableModules.Clear();
                    _setLoaded.Clear();
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
        
        /// <summary>
        /// Retains everything a failed <see cref="Initialize"/> accepted and records the failure.
        /// The caller inspects the retained objects and must call <see cref="Dispose"/> to release them.
        /// </summary>
        private void RetainAfterFailedInitialization(Exception exception) {
            lock (_lock) {
                _flagIsFaulted = true;
                _flagIsInitialized = false;
                List<object> listRetained = new List<object>(_listOwned);
                if (_serviceContent != null) {
                    listRetained.Add(_serviceContent);
                }
                _failure = new LifecycleFailure(
                    "GameHost.Initialize",
                    exception,
                    FailurePolicy.Retain,
                    listRetained,
                    new List<Exception>()
                );
            }
        }
        
        /// <summary>
        /// Disposes the host and all disposable modules, and unloads content. 
        /// Exactly one caller performs the release and every other caller waits for it to finish; each
        /// release is attempted even when an earlier one fails, and a release failure is reported rather
        /// than propagated. Under <see cref="FailurePolicy.Release"/>, a host whose
        /// <see cref="Initialize"/> failed is already disposed and this method is a no-op. Under
        /// <see cref="FailurePolicy.Retain"/>, this method is the release point for the retained
        /// modules and content.
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
                    ReportCleanupFailure(
                        new InvalidOperationException(
                            "Cannot dispose GameHost from within Update, Draw, Initialize or a module admission. "
                        ),
                        "GameHost.Dispose (reentrant)"
                    );
                    return;
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
                
                // Dispose all modules that implement IDisposable, then unload and dispose content.
                if (flagDisposing) {
                    ReleaseOwnedResources($"{GetType().Name}.Dispose");
                }
            } finally {
                // Finally mark as fully disposed and clear internal lists. The published schedules are
                // replaced rather than cleared: a published list is guaranteed immutable, so even a
                // reference that outlived its traversal sees a consistent (stale) snapshot.
                lock (_lock) {
                    _flagDisposed = true;
                    _serviceContent = null;
                    _setModule.Clear();
                    _listOwned.Clear();
                    _setOwned.Clear();
                    _indexAdmission.Clear();
                    _listLoadableModules.Clear();
                    _setLoaded.Clear();
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
