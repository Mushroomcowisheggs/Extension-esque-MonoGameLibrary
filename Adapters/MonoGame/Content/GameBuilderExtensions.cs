using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.MonoGame.Content {
    /// <summary>
    /// Explicit composition entry point for the MonoGame content capability. The module is also
    /// discoverable through <see cref="Core.Modularity.ModuleRegistrationAttribute"/>; this member
    /// exists so a composition root can name the module it installs instead of relying on discovery.
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>Registers the content module and the services it owns.</summary>
        /// <param name="builder">The game builder.</param>
        /// <returns>The same builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no content manager is registered.</exception>
        public static GameBuilder UseContent(this GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            new ContentModule().Register(builder);
            return builder;
        }
    }
}
