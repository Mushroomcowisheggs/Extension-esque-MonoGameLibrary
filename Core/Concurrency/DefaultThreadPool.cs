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
        private int _markDispose;
        private int _countPendingWork;
        private readonly ILogger _logger;
        // Late-bound handler provider. The Optional<T> shape stays on the constructor/setter API;
        // storing an Optional<T> field would make the field itself mutable after construction.
        private Func<Action<Exception, string>> _exceptionHandler;
        
        /// <summary>
        /// Gets the current exception handler by invoking the provider.
        /// Returns a no-op delegate if no provider is configured or the provider returns null.
        /// </summary>
        /// <returns>An action that handles exceptions.</returns>
        private Action<Exception, string> GetCurrentExceptionHandler() {
            Func<Action<Exception, string>> provider = _exceptionHandler;
            if (provider == null) {
                return delegate(Exception exception, string context) { };
            }
            
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
        
        /// <summary>
        /// Records a background failure and invokes the configured handler without allowing the
        /// handler, or the provider that returns it, to escape the pool.
        /// </summary>
        private void ReportFailure(Exception exception, string context) {
            try {
                _logger.Error($"Unhandled exception in {context}.", exception);
            } catch (Exception) {
            }
            
            Action<Exception, string> handler;
            try {
                handler = GetCurrentExceptionHandler();
            } catch (Exception exceptionHandler) {
                AttachHandlerFailure(exception, exceptionHandler);
                return;
            }
            if (handler == null) {
                return;
            }
            try {
                handler(exception, context);
            } catch (Exception exceptionHandler) {
                AttachHandlerFailure(exception, exceptionHandler);
            }
        }
        
        private void AttachHandlerFailure(Exception exception, Exception exceptionHandler) {
            if (!exception.Data.Contains("HandlerException")) {
                exception.Data["HandlerException"] = exceptionHandler;
            }
            try {
                _logger.Error(
                    $"Exception handler failed while processing [{exception}].",
                    exceptionHandler
                );
            } catch (Exception) {
            }
        }
        
        /// <summary>Replaces the provider whose handler receives exceptions raised by background tasks.</summary>
        /// <param name="provider">Returns the handler to invoke; the provider runs on each unhandled exception.</param>
        public void SetExceptionHandlerProvider(Func<Action<Exception, string>> provider) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            _exceptionHandler = provider;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultThreadPool"/> class.
        /// </summary>
        /// <param name="logger">Optional logger for reporting unhandled exceptions. If not provided, a <see cref="NullLogger"/> is used.</param>
        /// <param name="exceptionHandler">Optional provider that returns an exception handler delegate. The provider is invoked when an unhandled exception occurs in a background task. If not provided or if the provider returns null, exceptions are silently ignored.</param>
        public DefaultThreadPool(Optional<ILogger> logger = default, Optional<Func<Action<Exception, string>>> exceptionHandler = default) {
            if (logger.HasValue) {
                _logger = logger.Value;
            } else {
                _logger = NullLogger.Instance;
            }
            _exceptionHandler = exceptionHandler.HasValue ? exceptionHandler.Value : null;
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
            
            if (Volatile.Read(ref _markDispose) != 0) {
                throw new ObjectDisposedException(nameof(DefaultThreadPool));
            }
            
            Interlocked.Increment(ref _countPendingWork);
            ThreadPool.QueueUserWorkItem(delegate(object state) {
                try {
                    work();
                } catch (Exception exception) {
                    ReportFailure(exception, $"DefaultThreadPool.QueueWorkItem({name})");
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
            
            if (Volatile.Read(ref _markDispose) != 0) {
                throw new ObjectDisposedException(nameof(DefaultThreadPool));
            }
            
            Interlocked.Increment(ref _countPendingWork);
            return Task.Run(delegate () {
                try {
                    return work();
                } catch (Exception exception) {
                    ReportFailure(exception, $"DefaultThreadPool.RunAsync<{typeof(T).Name}>({name})");
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
            
            if (Volatile.Read(ref _markDispose) != 0) {
                throw new ObjectDisposedException(nameof(DefaultThreadPool));
            }
            
            Interlocked.Increment(ref _countPendingWork);
            return Task.Run(async delegate() {
                try {
                    await work().ConfigureAwait(false);
                } catch (Exception exception) {
                    ReportFailure(exception, $"DefaultThreadPool.RunAsync({name})");
                    throw;
                } finally {
                    Interlocked.Decrement(ref _countPendingWork);
                }
            });
        }
        
        /// <inheritdoc />
        public void Shutdown(bool flagWaitForCompletion) {
            Interlocked.Exchange(ref _markDispose, 1);
            
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
