using System;

namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// Records the thread that created it and rejects later calls from any other thread.
    /// Adapters use this to turn "this must run on the graphics thread" into an explicit
    /// failure instead of intermittent corruption.
    /// </summary>
    public sealed class ThreadAccess {
        private readonly int _identifierThread = Environment.CurrentManagedThreadId;
        
        /// <summary>
        /// Verifies that the caller runs on the thread that created this instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the caller is on another thread.</exception>
        public void VerifyAccess() {
            if (Environment.CurrentManagedThreadId != _identifierThread) {
                throw new InvalidOperationException("This operation must run on the thread that owns the resource.");
            }
        }
    }
}