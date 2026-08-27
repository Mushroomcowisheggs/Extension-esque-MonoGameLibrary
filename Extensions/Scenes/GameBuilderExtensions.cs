using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>
    /// Extension methods for registering scene services with a <see cref="GameBuilder"/>. 
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>
        /// Registers the scene service and module. 
        /// </summary>
        /// <param name="builder">The game builder instance. </param>
        /// <param name="order">Execution order for the module (default 0). </param>
        /// <returns>The game builder instance. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null. </exception>
        public static GameBuilder UseScenes(
                this GameBuilder builder, 
                int order = 0
            ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            var module = new SceneModule(order);
            module.Register(builder);
            return builder;
        }
    }
}