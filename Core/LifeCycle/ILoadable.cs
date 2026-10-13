using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Core.Lifecycle {
    /// <summary>
    /// Implemented by modules that require a one-time content loading phase.
    /// Modules must obtain necessary services (like <see cref="IContentService"/>) 
    /// through constructor injection: the composition root resolves services at build time 
    /// and passes them in. A module cannot reach the registry afterwards even if it tries to:
    /// <see cref="GameBuilder.GetService{TService}"/> and <see cref="GameBuilder.TryGetService{TService}"/>
    /// throw once the builder has built, and the host exposes no registry at all.
    /// </summary>
    public interface ILoadable {
        /// <summary>
        /// Called when the module should load its content.
        /// </summary>
        void LoadContent();
    }
}
