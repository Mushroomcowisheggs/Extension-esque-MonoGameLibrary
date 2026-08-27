using System;
using System.Collections.Generic;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Utilities {
    /// <summary>
    /// A utility for managing compensation actions in LIFO order.
    /// Designed for use in game application code only.
    /// Framework modules must perform explicit cleanup in Dispose.
    /// </summary>
    public sealed class CompensationStack : IDisposable {
        private readonly Stack<Action> _actionsCompensation;
        private readonly ILogger _logger;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CompensationStack"/> class.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        public CompensationStack(Optional<ILogger> logger = default) {
            _actionsCompensation = new Stack<Action>();
            _flagDisposed = false;
            if (logger.HasValue) {
                _logger = logger.Value;
            } else {
                _logger = NullLogger.Instance;
            }
        }
        
        /// <summary>
        /// Registers a compensation action to be executed when the stack is reverted.
        /// Compensations are executed in LIFO order.
        /// </summary>
        /// <param name="actionCompensation">The action to execute on cleanup.</param>
        /// <exception cref="ArgumentNullException">Thrown if compensation is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the stack has been disposed.</exception>
        public void Compensate(Action actionCompensation) {
            if (actionCompensation == null) {
                throw new ArgumentNullException(nameof(actionCompensation));
            }
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(CompensationStack));
            }
            _actionsCompensation.Push(actionCompensation);
        }
        
        /// <summary>
        /// Registers an IDisposable resource for disposal on cleanup.
        /// </summary>
        /// <param name="resource">The resource to dispose.</param>
        public void CompensateDispose(IDisposable resource) {
            if (resource == null) {
                return;
            }
            Compensate(delegate() {
                resource.Dispose();
            });
        }
        
        /// <summary>
        /// Executes all registered compensations in LIFO order.
        /// Exceptions are caught and logged; remaining compensations continue.
        /// </summary>
        public void RevertAll() {
            if (_flagDisposed) {
                return;
            }
            
            while (_actionsCompensation.Count > 0) {
                Action action = _actionsCompensation.Pop();
                try {
                    if (action != null) {
                        action.Invoke();
                    }
                } catch (Exception exception) {
                    _logger.Error("CompensationStack: Compensation action threw an exception.", exception);
                }
            }
        }
        
        /// <summary>
        /// Executes all registered compensations and disposes the stack.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            RevertAll();
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}