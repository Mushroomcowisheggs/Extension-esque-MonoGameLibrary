using System;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Provides a container for registering and resolving services by type. 
    /// </summary>
    /// <remarks>
    /// The registry is a composition-phase tool. It has no removal member on purpose: the capability
    /// set is fixed when <see cref="GameBuilder.Build"/> returns, and after that point neither the
    /// builder nor the host resolves services, so no code can widen or narrow the set at runtime.
    /// </remarks>
    public interface IServiceRegistry {
        /// <summary>
        /// Registers a service instance. Throws if the type is already registered. 
        /// </summary>
        /// <typeparam name="T">The type of the service. </typeparam>
        /// <param name="instance">The service instance. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is null. </exception>
        /// <exception cref="InvalidOperationException">Thrown if the service is already registered. </exception>
        void Register<T>(T instance) where T : class;
        
        /// <summary>
        /// Tries to register a service instance, optionally overwriting an existing registration. 
        /// </summary>
        /// <typeparam name="T">The type of the service. </typeparam>
        /// <param name="instance">The service instance. </param>
        /// <param name="flagOverwrite">If <c>true</c>, replaces any existing registration. </param>
        /// <returns><c>true</c> if the registration succeeded; otherwise <c>false</c>. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is null. </exception>
        bool TryRegister<T>(T instance, bool flagOverwrite = false) where T : class;
        
        /// <summary>
        /// Retrieves a registered service. Throws if not found. 
        /// </summary>
        /// <typeparam name="T">The type of the service. </typeparam>
        /// <returns>The registered service instance. </returns>
        /// <exception cref="InvalidOperationException">Thrown if the service is not registered. </exception>
        T Get<T>() where T : class;
        
        /// <summary>
        /// Attempts to retrieve a registered service. 
        /// </summary>
        /// <typeparam name="T">The type of the service. </typeparam>
        /// <param name="instance">When this method returns, contains the service instance if found; otherwise, <c>null</c>. </param>
        /// <returns><c>true</c> if the service was found; otherwise <c>false</c>. </returns>
        bool TryGet<T>(out T instance) where T : class;
        
        /// <summary>
        /// Checks whether a service of the given type is registered. 
        /// </summary>
        /// <typeparam name="T">The type of the service. </typeparam>
        /// <returns><c>true</c> if a service of type <typeparamref name="T"/> is registered; otherwise <c>false</c>. </returns>
        bool Contains<T>() where T : class;
    }
}
