using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MonoGameLibrary.Core.Pooling {
    /// <summary>
    /// A basic object pool factory implementation backed by a stack. 
    /// </summary>
    public sealed class DefaultObjectPoolFactory : IObjectPoolFactory {
        /// <inheritdoc />
        public IObjectPool<T> CreatePool<T>(Func<T> factory, int capacityInitial, int capacityMax) where T : class {
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            
            if (capacityInitial < 0) {
                throw new ArgumentOutOfRangeException(nameof(capacityInitial));
            }
            
            if (capacityMax < 1) {
                throw new ArgumentOutOfRangeException(nameof(capacityMax));
            }
            
            return new DefaultObjectPool<T>(factory, capacityInitial, capacityMax);
        }
        
        private sealed class DefaultObjectPool<T> : IObjectPool<T> where T : class {
            private readonly Func<T> _factory;
            private readonly ConcurrentStack<T> _stackAvailable = new ConcurrentStack<T>();
            private int _countInUse;
            private readonly int _capacityMax;
            
            // Return and Clear coordinate through this lock: the capacity test and the push must be one
            // step, or two concurrent returns both observe room below the limit and both store an item.
            // Get does not take it — a ConcurrentStack pop is already safe, and retrieval is the hot path.
            private readonly object _lockStore = new object();
            
            public DefaultObjectPool(Func<T> factory, int capacityInitial, int capacityMax) {
                if (factory == null) {
                    throw new ArgumentNullException(nameof(factory));
                }
                _factory = factory;
                _capacityMax = capacityMax;
                
                // The initial fill runs in the constructor, so a failure would discard objects that
                // already exist with no reference left to release them. They are released here instead.
                List<T> listCreated = new List<T>();
                try {
                    for (int i = 0; i < capacityInitial; i += 1) {
                        T item = factory();
                        listCreated.Add(item);
                        _stackAvailable.Push(item);
                    }
                } catch (Exception) {
                    for (int i = 0; i < listCreated.Count; i += 1) {
                        if (listCreated[i] is IDisposable disposable) {
                            disposable.Dispose();
                        }
                    }
                    throw;
                }
            }
            
            /// <inheritdoc />
            public T Get() {
                if (_stackAvailable.TryPop(out var item)) {
                    Interlocked.Increment(ref _countInUse);
                    return item;
                }
                
                // The lease is counted before the factory runs because a successful construction must
                // not be observable as an uncounted lease. A failing factory never reaches a caller, so
                // the count is withdrawn on that path; otherwise the diagnostic would report a lease
                // that does not exist.
                Interlocked.Increment(ref _countInUse);
                try {
                    return _factory();
                } catch (Exception) {
                    Interlocked.Decrement(ref _countInUse);
                    throw;
                }
            }
            
            /// <inheritdoc />
            public bool TryGet(out T item) {
                if (_stackAvailable.TryPop(out item)) {
                    Interlocked.Increment(ref _countInUse);
                    return true;
                }
                
                item = null;
                return false;
            }
            
            /// <inheritdoc />
            public void Return(T item) {
                if (item == null) {
                    throw new ArgumentNullException(nameof(item));
                }
                
                lock (_lockStore) {
                    if (_stackAvailable.Count >= _capacityMax) {
                        Interlocked.Decrement(ref _countInUse);
                        return;
                    }
                    
                    _stackAvailable.Push(item);
                }
                Interlocked.Decrement(ref _countInUse);
            }
            
            /// <inheritdoc />
            public int InUseCount { get { return Volatile.Read(ref _countInUse); } }
            
            /// <inheritdoc />
            public int AvailableCount { get { return _stackAvailable.Count; } }
            
            /// <inheritdoc />
            public void Clear() {
                // Drops idle objects only. In-use accounting is deliberately preserved so that
                // outstanding leases stay visible in InUseCount; a later Return() simply pools
                // the object again (or drops it when the capacity is reached).
                lock (_lockStore) {
                    _stackAvailable.Clear();
                }
            }
        }
    }
}
