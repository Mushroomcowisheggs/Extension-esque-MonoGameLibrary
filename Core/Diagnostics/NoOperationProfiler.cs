using System;

namespace MonoGameLibrary.Core.Diagnostics {
    /// <summary>
    /// A profiler implementation that performs no work.
    /// </summary>
    public sealed class NoOperationProfiler : IProfiler {
        /// <inheritdoc />
        public IDisposable BeginMeasure(string name) {
            return new NoOperationMeasure();
        }
        
        /// <inheritdoc />
        public void RecordMetric(string name, double value) {
        }
        
        /// <inheritdoc />
        public void Flush() {
        }
        
        private sealed class NoOperationMeasure : IDisposable {
            public void Dispose() {
                GC.SuppressFinalize(this);
            }
        }
    }
}
