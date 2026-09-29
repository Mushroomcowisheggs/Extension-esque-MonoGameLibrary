using System;
using System.Collections.Concurrent;
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
            private int _inUse;
            private readonly int _capacityMax;
            
            public DefaultObjectPool(Func<T> factory, int capacityInitial, int capacityMax) {
                _factory = factory;
                _capacityMax = capacityMax;
                
                for (int i = 0; i < capacityInitial; i += 1) {
                    _stackAvailable.Push(factory());
                }
            }
            
            /// <inheritdoc />
            public T Get() {
                if (_stackAvailable.TryPop(out var item)) {
                    Interlocked.Increment(ref _inUse);
                    return item;
                }
                
                Interlocked.Increment(ref _inUse);
                return _factory();
            }
            
            /// <inheritdoc />
            public bool TryGet(out T item) {
                if (_stackAvailable.TryPop(out item)) {
                    Interlocked.Increment(ref _inUse);
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
                
                if (CountAvailable >= _capacityMax) {
                    Interlocked.Decrement(ref _inUse);
                    return;
                }
                
                _stackAvailable.Push(item);
                Interlocked.Decrement(ref _inUse);
            }
            
            /// <inheritdoc />
            public int CountInUse { get { return Volatile.Read(ref _inUse); } }
            
            /// <inheritdoc />
            public int CountAvailable { get { return _stackAvailable.Count; } }
            
            /// <inheritdoc />
            public void Clear() {
                // Drops idle objects only. In-use accounting is deliberately preserved so that
                // outstanding leases stay visible in CountInUse; a later Return() simply pools
                // the object again (or drops it when the capacity is reached).
                _stackAvailable.Clear();
            }
        }
    }
}
