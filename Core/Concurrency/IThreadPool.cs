using System;
using System.Threading.Tasks;

namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// Provides a managed thread pool for running background work.
    /// </summary>
    public interface IThreadPool {
        /// <inheritdoc cref="QueueWorkItem(Action, string)"/>
        /// <param name="work">The work to execute.</param>
        /// <remarks>This overload does not accept a debugging name.</remarks>
        void QueueWorkItem(Action work);
        
        /// <summary>
        /// Queues a work item for execution on a background thread.
        /// </summary>
        /// <param name="work">The work to execute.</param>
        /// <param name="name">An optional name for debugging.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="work"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been shut down.</exception>
        void QueueWorkItem(Action work, string name);

        /// <inheritdoc cref="RunAsync{T}(Func{T}, string)"/>
        /// <param name="work">The function to execute.</param>
        /// <remarks>This overload does not accept a debugging name.</remarks>
        Task<T> RunAsync<T>(Func<T> work);

        /// <summary>
        /// Queues a function for execution and returns a task representing the result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="work">The function to execute.</param>
        /// <param name="name">An optional name for debugging purposes.</param>
        /// <returns>A <see cref="Task{T}"/> that completes with the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="work"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been shut down.</exception>
        Task<T> RunAsync<T>(Func<T> work, string name);

        /// <inheritdoc cref="RunAsync(Func{Task}, string)"/>
        /// <param name="work">The asynchronous function to execute.</param>
        /// <remarks>This overload does not accept a debugging name.</remarks>
        Task RunAsync(Func<Task> work);

        /// <summary>
        /// Queues an asynchronous task for execution.
        /// </summary>
        /// <param name="work">A function that returns a <see cref="Task"/> to be executed.</param>
        /// <param name="name">An optional name for debugging purposes.</param>
        /// <returns>A <see cref="Task"/> representing the queued work.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="work"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been shut down.</exception>
        Task RunAsync(Func<Task> work, string name);

        /// <summary>
        /// Gets the number of work items currently pending execution.
        /// </summary>
        int PendingWorkCount { get; }
        
        /// <summary>
        /// Shuts down the thread pool, optionally waiting for all pending work to complete.
        /// Shutdown only prevents new work from being queued: subsequent 
        /// <see cref="QueueWorkItem(Action, string)"/>/<c>RunAsync</c> calls throw 
        /// <see cref="ObjectDisposedException"/>. Work that is already queued or running is 
        /// never cancelled or discarded and will still execute. 
        /// </summary>
        /// <param name="flagWaitForCompletion">
        /// If <c>true</c>, blocks until all queued work items have finished execution;
        /// if <c>false</c>, returns immediately while already queued work items continue
        /// to run to completion in the background.
        /// </param>
        void Shutdown(bool flagWaitForCompletion);
    }
}