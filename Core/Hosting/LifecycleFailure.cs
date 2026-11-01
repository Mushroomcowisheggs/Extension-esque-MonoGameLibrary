using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Diagnostic record for a failed composition or initialization.
    /// </summary>
    public sealed class LifecycleFailure {
        private readonly IReadOnlyList<object> _listRetained;
        private readonly IReadOnlyList<Exception> _listReleaseFailure;
        
        internal LifecycleFailure(
            string stage,
            Exception exception,
            FailurePolicy policyFailure,
            IReadOnlyList<object> listRetained,
            IReadOnlyList<Exception> listReleaseFailure
        ) {
            if (stage == null) {
                throw new ArgumentNullException(nameof(stage));
            }
            if (exception == null) {
                throw new ArgumentNullException(nameof(exception));
            }
            Stage = stage;
            Exception = exception;
            Policy = policyFailure;
            if (listRetained == null) {
                _listRetained = new List<object>();
            } else {
                _listRetained = listRetained;
            }
            if (listReleaseFailure == null) {
                _listReleaseFailure = new List<Exception>();
            } else {
                _listReleaseFailure = listReleaseFailure;
            }
        }
        
        /// <summary>Gets the operation that failed. </summary>
        public string Stage { get; }
        
        /// <summary>Gets the original failure. </summary>
        public Exception Exception { get; }
        
        /// <summary>Gets the policy that was in force. </summary>
        public FailurePolicy Policy { get; }
        
        /// <summary>
        /// Gets the resources retained for the caller under <see cref="FailurePolicy.Retain"/>.
        /// Empty under <see cref="FailurePolicy.Release"/>.
        /// </summary>
        public IReadOnlyList<object> RetainedObjects {
            get { return _listRetained; }
        }
        
        /// <summary>Gets failures raised while releasing resources under <see cref="FailurePolicy.Release"/>. </summary>
        public IReadOnlyList<Exception> ReleaseFailures {
            get { return _listReleaseFailure; }
        }
    }
}
