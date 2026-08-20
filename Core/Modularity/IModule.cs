using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Core.Modularity {
    /// <summary>
    /// Defines a contract for a pluggable module that can register its services
    /// and components with the host during the composition phase. 
    /// Core layer is completely unaware of any business interfaces such as
    /// IAudioService or IInputService. 
    /// </summary>
    public interface IModule {
        /// <summary>
        /// Registers services and modules with the given game builder. 
        /// The implementation may call <see cref="GameBuilder.RegisterService{T}"/>
        /// and <see cref="GameBuilder.AddModule"/> to contribute to the host. 
        /// </summary>
        /// <param name="builder">The game builder to configure. </param>
        void Register(GameBuilder builder);
    }
}