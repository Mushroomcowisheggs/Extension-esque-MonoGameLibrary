using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Core.Lifecycle {
    /// <summary>
    /// Implemented by modules that require a one-time content loading phase.
    /// Modules must obtain necessary services (like <see cref="IContentService"/>) 
    /// through constructor injection: the composition root resolves services at build time 
    /// and passes them in. Service-locator lookups (for example through 
    /// <see cref="IServiceRegistry"/>) are not allowed inside a module; the registry is 
    /// reserved for the composition root.
    /// </summary>
    public interface ILoadable {
        /// <summary>
        /// Called when the module should load its content.
        /// </summary>
        void LoadContent();
    }
}