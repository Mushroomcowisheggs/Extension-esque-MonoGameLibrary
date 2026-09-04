using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// A simple in-memory cancellation service. 
    /// </summary>
    public sealed class DefaultCancellationService : ICancellationService, IDisposable {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _sources = new ConcurrentDictionary<string, CancellationTokenSource>();
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public CancellationToken GetTokenForOperation(string idOperation) {
            if (string.IsNullOrWhiteSpace(idOperation)) {
                throw new ArgumentException("Operation id cannot be empty.", nameof(idOperation));
            }
            
            return _sources.GetOrAdd(idOperation, delegate(string key) {
                return new CancellationTokenSource();
            }).Token;
        }
        
        /// <inheritdoc />
        public CancellationToken RenewToken(string idOperation) {
            if (string.IsNullOrWhiteSpace(idOperation)) {
                throw new ArgumentException("Operation id cannot be empty.", nameof(idOperation));
            }
            
            if (_sources.TryGetValue(idOperation, out var current)) {
                current.Cancel();
                current.Dispose();
            }
            
            var next = new CancellationTokenSource();
            _sources[idOperation] = next;
            return next.Token;
        }
        
        /// <inheritdoc />
        public void CancelOperation(string idOperation) {
            if (string.IsNullOrWhiteSpace(idOperation)) {
                throw new ArgumentException("Operation id cannot be empty.", nameof(idOperation));
            }
            
            if (_sources.TryGetValue(idOperation, out var source)) {
                source.Cancel();
            }
        }
        
        /// <inheritdoc />
        public void CancelAll() {
            foreach (var source in _sources.Values) {
                source.Cancel();
            }
        }
        
        /// <summary>
        /// Disposes the service, cancelling all remaining tokens and releasing resources. 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) { return; }
            _flagDisposed = true;
            
            foreach (var source in _sources.Values) {
                source.Cancel();
                source.Dispose();
            }
            _sources.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
