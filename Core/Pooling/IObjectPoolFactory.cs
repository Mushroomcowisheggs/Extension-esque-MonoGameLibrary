using System;
using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Core.Pooling {
    /// <summary>
    /// Factory for creating object pools.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IObjectPoolFactory {
        /// <summary>
        /// Creates a new object pool with the given parameters.
        /// </summary>
        /// <typeparam name="T">The type of objects in the pool.</typeparam>
        /// <param name="factory">The factory delegate used to create new instances when the pool is exhausted.</param>
        /// <param name="capacityInitial">The initial number of objects to pre-allocate.</param>
        /// <param name="capacityMax">The maximum capacity of the pool.</param>
        /// <returns>A new <see cref="IObjectPool{T}"/>.</returns>
        IObjectPool<T> CreatePool<T>(Func<T> factory, int capacityInitial, int capacityMax) where T : class;
    }
}