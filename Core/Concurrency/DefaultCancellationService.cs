using System;
using System.Collections.Generic;
using System.Threading;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// A simple in-memory cancellation service. 
    /// </summary>
    /// <remarks>
    /// Thread safety: every member is guarded by a lock. 
    /// Disposal contract: after <see cref="Dispose"/> the service refuses new work — 
    /// <see cref="GetTokenForOperation"/> and <see cref="RenewToken"/> throw 
    /// <see cref="ObjectDisposedException"/> and no named token is (re)created — while 
    /// <see cref="CancelOperation"/> and <see cref="CancelAll"/> become no-ops so teardown 
    /// code can call them safely. <see cref="Dispose"/> cancels and releases every 
    /// outstanding token. A cancellation callback that throws is reported to the configured logger and
    /// does not keep the remaining operations from being cancelled and released.
    /// </remarks>
    public sealed class DefaultCancellationService : ICancellationService, IDisposable {
        private readonly object _lock = new object();
        private readonly Dictionary<string, CancellationTokenSource> _sources = new Dictionary<string, CancellationTokenSource>();
        private readonly ILogger _logger;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes the service. 
        /// </summary>
        /// <param name="logger">Optional logger for a cancellation callback that throws during release. </param>
        public DefaultCancellationService(Optional<ILogger> logger = default) {
            if (logger.HasValue) { _logger = logger.Value; } else { _logger = NullLogger.Instance; }
        }
        
        /// <inheritdoc />
        public CancellationToken GetTokenForOperation(string idOperation) {
            if (string.IsNullOrWhiteSpace(idOperation)) {
                throw new ArgumentException("Operation id cannot be empty.", nameof(idOperation));
            }
            
            lock (_lock) {
                ThrowIfDisposed();
                CancellationTokenSource source;
                if (!_sources.TryGetValue(idOperation, out source)) {
                    source = new CancellationTokenSource();
                    _sources[idOperation] = source;
                }
                return source.Token;
            }
        }
        
        /// <inheritdoc />
        public CancellationToken RenewToken(string idOperation) {
            if (string.IsNullOrWhiteSpace(idOperation)) {
                throw new ArgumentException("Operation id cannot be empty.", nameof(idOperation));
            }
            
            lock (_lock) {
                ThrowIfDisposed();
                CancellationTokenSource current;
                if (_sources.TryGetValue(idOperation, out current)) {
                    // A callback that throws from Cancel must not skip the release of the previous scope
                    // or the replacement that renewal is defined to produce.
                    try {
                        current.Cancel();
                    } catch (Exception exception) {
                        _logger.Error("Cancelling the previous operation during renewal failed.", exception);
                    }
                    try {
                        current.Dispose();
                    } catch (Exception exception) {
                        _logger.Error("Releasing the previous operation's cancellation source failed.", exception);
                    }
                }
                
                var next = new CancellationTokenSource();
                _sources[idOperation] = next;
                return next.Token;
            }
        }
        
        /// <inheritdoc />
        public void CancelOperation(string idOperation) {
            if (string.IsNullOrWhiteSpace(idOperation)) {
                throw new ArgumentException("Operation id cannot be empty.", nameof(idOperation));
            }
            
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                CancellationTokenSource source;
                if (_sources.TryGetValue(idOperation, out source)) {
                    source.Cancel();
                }
            }
        }
        
        /// <inheritdoc />
        public void CancelAll() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                foreach (var source in _sources.Values) {
                    source.Cancel();
                }
            }
        }
        
        /// <summary>
        /// Disposes the service, cancelling all remaining tokens and releasing resources. 
        /// Subsequent token acquisitions throw <see cref="ObjectDisposedException"/>; 
        /// cancellation requests become no-ops. 
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) { return; }
                _flagDisposed = true;
                
                // Per operation, and per step: a callback that throws from Cancel must not keep this
                // operation's source alive, and it must not keep the remaining operations from being
                // cancelled and released.
                foreach (var source in _sources.Values) {
                    try {
                        source.Cancel();
                    } catch (Exception exception) {
                        _logger.Error("Cancelling a named operation during disposal failed.", exception);
                    }
                    try {
                        source.Dispose();
                    } catch (Exception exception) {
                        _logger.Error("Releasing a named operation's cancellation source failed.", exception);
                    }
                }
                _sources.Clear();
            }
            GC.SuppressFinalize(this);
        }
        
        private void ThrowIfDisposed() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(DefaultCancellationService));
            }
        }
    }
}
