using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.MonoGame.Audio {
    /// <summary>
    /// Extension methods for registering audio services with a <see cref="GameBuilder"/>. 
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>
        /// Registers the audio service and module.
        /// </summary>
        /// <param name="builder">The game builder instance.</param>
        /// <returns>The game builder instance. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null. </exception>
        public static GameBuilder UseAudio(this GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            var module = new AudioModule();
            module.Register(builder);
            return builder;
        }
    }
}