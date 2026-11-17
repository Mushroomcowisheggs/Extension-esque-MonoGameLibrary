using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.Godot.Content {
    /// <summary>
    /// Explicit composition entry point for the Godot content capability. The module is also
    /// discoverable through <see cref="Core.Modularity.ModuleRegistrationAttribute"/>; this member
    /// exists so a composition root can name the module it installs instead of relying on discovery.
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>Registers the content module and the services it owns, with the default content root.</summary>
        /// <param name="builder">The game builder.</param>
        /// <returns>The same builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        public static GameBuilder UseContent(this GameBuilder builder) {
            return UseContent(builder, ContentModule.DirectoryRootDefault);
        }
        
        /// <summary>Registers the content module and the services it owns, with an explicit content root.</summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="directoryRoot">The content root, e.g. <c>"res://Content"</c>.</param>
        /// <returns>The same builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="directoryRoot"/> is null or whitespace.</exception>
        public static GameBuilder UseContent(this GameBuilder builder, string directoryRoot) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            new ContentModule(directoryRoot).Register(builder);
            return builder;
        }
    }
}
