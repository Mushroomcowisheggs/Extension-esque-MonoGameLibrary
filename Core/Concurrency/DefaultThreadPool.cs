using System;
using System.Threading;
using System.Threading.Tasks;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// A lightweight default implementation of <see cref="IThreadPool"/>. 
    /// </summary>
    public sealed class DefaultThreadPool : IThreadPool, IDisposable {
        private int _stateDispose;
        private int _countPendingWork;
        private readonly ILogger _logger;
        private Optional<Func<Action<Exception, string>>> _handlerException;
        
        /// <summary>
        /// Gets the current exception handler by invoking the provider.
        /// Returns a no-op delegate if no provider is configured or the provider returns null.
        /// </summary>
        /// <returns>An action that handles exceptions.</returns>
        private Action<Exception, string> GetCurrentExceptionHandler() {
            if (!_handlerException.HasValue) {
                return delegate(Exception exception, string context) { };
            }
            
            Func<Action<Exception, string>> provider = _handlerException.Value;
            Action<Exception, string> handler;
            if (provider == null) {
                handler = delegate(Exception exception, string context) { };
            } else {
                handler = provider.Invoke();
                if (handler == null) {
                    handler = delegate(Exception exception, string context) { };
                }
            }
            return handler;
        }
        
        public void SetExceptionHandlerProvider(Func<Action<Exception, string>> provider) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            _handlerException = new Optional<Func<Action<Exception, string>>>(provider);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultThreadPool"/> class.
        /// </summary>
        /// <param name="logger">Optional logger for reporting unhandled exceptions. If not provided, a <see cref="NullLogger"/> is used.</param>
        /// <param name="handlerException">Optional provider that returns an exception handler delegate. The provider is invoked when an unhandled exception occurs in a background task. If not provided or if the provider returns null, exceptions are silently ignored.</param>
        public DefaultThreadPool(Optional<ILogger> logger = default, Optional<Func<Action<Exception, string>>> handlerException = default) {
            if (logger.HasValue) {
                _logger = logger.Value;
            } else {
                _logger = NullLogger.Instance;
            }
            _handlerException = handlerException;
        }
        
        /// <inheritdoc />
        public int PendingWorkCount { get { return Volatile.Read(ref _countPendingWork); } }
        
        /// <inheritdoc />
        public void QueueWorkItem(Action work) {
            QueueWorkItem(work, string.Empty);
        }
        
        /// <inheritdoc />
        public void QueueWorkItem(Action work, string name) {
            if (work == null) {
                throw new ArgumentNullException(nameof(work));
            }
            
            if (Volatile.Read(ref _stateDispose) != 0) {
                throw new ObjectDisposedException(nameof(DefaultThreadPool));
            }
            
            Interlocked.Increment(ref _countPendingWork);
            ThreadPool.QueueUserWorkItem(delegate(object state) {
                try {
                    work();
                } catch (Exception exception) {
                    _logger.Error($"Unhandled exception in queued work item '{name}'.", exception);
                    Action<Exception, string> handler = GetCurrentExceptionHandler();
                    handler(exception, $"DefaultThreadPool.QueueWorkItem({name})");
                } finally {
                    Interlocked.Decrement(ref _countPendingWork);
                }
            });
        }
        
        /// <inheritdoc />
        public Task<T> RunAsync<T>(Func<T> work) {
            return RunAsync(work, string.Empty);
        }
        
        /// <inheritdoc />
        public Task<T> RunAsync<T>(Func<T> work, string name) {
            if (work == null) {
                throw new ArgumentNullException(nameof(work));
            }
            
            if (Volatile.Read(ref _stateDispose) != 0) {
                throw new ObjectDisposedException(nameof(DefaultThreadPool));
            }
            
            Interlocked.Increment(ref _countPendingWork);
            return Task.Run(delegate () {
                try {
                    return work();
                } catch (Exception exception) {
                    _logger.Error($"Unhandled exception in async work '{name}'.", exception);
                    Action<Exception, string> handler = GetCurrentExceptionHandler();
                    handler(exception, $"DefaultThreadPool.RunAsync<{typeof(T).Name}>({name})");
                    throw;
                } finally {
                    Interlocked.Decrement(ref _countPendingWork);
                }
            });
        }
        
        /// <inheritdoc />
        public Task RunAsync(Func<Task> work) {
            return RunAsync(work, string.Empty);
        }
        
        /// <inheritdoc />
        public Task RunAsync(Func<Task> work, string name) {
            if (work == null) {
                throw new ArgumentNullException(nameof(work));
            }
            
            if (Volatile.Read(ref _stateDispose) != 0) {
                throw new ObjectDisposedException(nameof(DefaultThreadPool));
            }
            
            Interlocked.Increment(ref _countPendingWork);
            return Task.Run(async delegate() {
                try {
                    await work().ConfigureAwait(false);
                } catch (Exception exception) {
                    _logger.Error($"Unhandled exception in async task work '{name}'.", exception);
                    Action<Exception, string> handler = GetCurrentExceptionHandler();
                    handler(exception, $"DefaultThreadPool.RunAsync({name})");
                    throw;
                } finally {
                    Interlocked.Decrement(ref _countPendingWork);
                }
            });
        }
        
        /// <inheritdoc />
        public void Shutdown(bool flagWaitForCompletion) {
            Interlocked.Exchange(ref _stateDispose, 1);
            
            if (flagWaitForCompletion) {
                var spin = new SpinWait();
                while (PendingWorkCount > 0) {
                    spin.SpinOnce();
                }
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            Shutdown(false);
            GC.SuppressFinalize(this);
        }
    }
}
