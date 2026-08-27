using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>
    /// Extension methods for registering screen services with a <see cref="GameBuilder"/>. 
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>
        /// Registers the screen service and its host module.
        /// </summary>
        /// <param name="builder">The game builder. </param>
        /// <param name="order">Execution order for the module (default 0). </param>
        /// <returns>The builder. </returns>
        /// <exception cref="ArgumentNullException">Thrown if builder is null. </exception>
        public static GameBuilder UseScreens(this GameBuilder builder, int order = 0) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            var module = new ScreenModule(order);
            module.Register(builder);
            
            return builder;
        }
    }
}