using System;
using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Core.Content {
    /// <summary>
    /// Loads assets by name. 
    /// The single generic method avoids a proliferation of LoadXXX methods; 
    /// new asset types only require a new <see cref="IAsset"/> subtype. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Single)]
    public interface IContentService : IDisposable {
        /// <summary>
        /// Registers a strongly-typed loader for a specific asset type. 
        /// </summary>
        /// <typeparam name="T">The asset interface type (must implement <see cref="IAsset"/>). </typeparam>
        /// <param name="loader">A function that takes an asset name and returns an instance of <typeparamref name="T"/>. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="loader"/> is null. </exception>
        /// <exception cref="ArgumentException">Thrown if a loader for type <typeparamref name="T"/> is already registered. </exception>
        void Register<T>(Func<string, T> loader) where T : class, IAsset;
        
        /// <summary>
        /// Loads an asset of the specified type. 
        /// </summary>
        /// <typeparam name="T">The asset interface type. </typeparam>
        /// <param name="nameAsset">The asset name (e.g., file path without extension). </param>
        /// <returns>The loaded asset. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="nameAsset"/> is null. </exception>
        /// <exception cref="NotSupportedException">Thrown if no loader is registered for type <typeparamref name="T"/>. </exception>
        T Load<T>(string nameAsset) where T : class, IAsset;
        
        /// <summary>
        /// Unloads loaded assets. 
        /// </summary>
        void Unload();
    }
}
