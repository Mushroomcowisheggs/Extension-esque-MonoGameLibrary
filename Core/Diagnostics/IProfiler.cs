using System;
using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Core.Diagnostics {
    /// <summary>
    /// A lightweight profiling interface for measuring performance.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IProfiler {
        /// <summary>
        /// Begins measuring a named section. Dispose the returned object to stop measurement.
        /// </summary>
        /// <param name="name">The name of the measured section.</param>
        /// <returns>An <see cref="IDisposable"/> that stops the measurement when disposed.</returns>
        IDisposable BeginMeasure(string name);
        
        /// <summary>
        /// Records a metric value for a named key.
        /// </summary>
        /// <param name="name">The metric name.</param>
        /// <param name="value">The metric value.</param>
        void RecordMetric(string name, double value);
        
        /// <summary>
        /// Flushes any buffered profile data to the underlying storage.
        /// </summary>
        void Flush();
    }
}