using System;
using System.Collections.Generic;
using System.Threading;

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
    /// outstanding token. 
    /// </remarks>
    public sealed class DefaultCancellationService : ICancellationService, IDisposable {
        private readonly object _lock = new object();
        private readonly Dictionary<string, CancellationTokenSource> _sources = new Dictionary<string, CancellationTokenSource>();
        private bool _flagDisposed;
        
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
                    current.Cancel();
                    current.Dispose();
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
                
                foreach (var source in _sources.Values) {
                    source.Cancel();
                    source.Dispose();
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
