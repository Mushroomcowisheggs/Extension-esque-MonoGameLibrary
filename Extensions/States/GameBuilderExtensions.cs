using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Extensions.States {
    /// <summary>
    /// Explicit composition entry point for the state machine. The module is also discoverable
    /// through <see cref="Core.Modularity.ModuleRegistrationAttribute"/>; this member exists so a
    /// composition root can name the module it installs instead of relying on discovery.
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>Registers the state module and the service it owns.</summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="order">The update and draw order of the state module.</param>
        /// <returns>The same builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        public static GameBuilder UseStates(this GameBuilder builder, int order = 0) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            new StateModule(order).Register(builder);
            return builder;
        }
    }
}
