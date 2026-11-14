using System;
using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Core.Concurrency {
    [ThreadCondition(ThreadAffinity.Caller)]
    public interface ILoadingProgress {
        /// <summary>
        /// Reports progress for a specific operation.
        /// </summary>
        /// <param name="nameOperation">The name of the operation being reported.</param>
        /// <param name="progress">A value between 0 and 1 indicating completion percentage.</param>
        void Report(string nameOperation, float progress);
        
        /// <summary>
        /// Occurs when progress is updated for any operation.
        /// </summary>
        event Action<string, float> ProgressUpdated;
    }
}