using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>Bootstraps a fully integrated MonoGame application.</summary>
    public static class GameApplication {
        /// <summary>Runs an application with default options.</summary>
        /// <param name="actionConfigureServices">Composes the host's services and modules.</param>
        public static void Run(Action<GameBuilder> actionConfigureServices) {
            Run(new GameApplicationOptions(), actionConfigureServices);
        }
        
        /// <summary>Runs an application with the given options, and disposes the platform host when it exits.</summary>
        /// <param name="options">The window and timing settings.</param>
        /// <param name="actionConfigureServices">Composes the host's services and modules.</param>
        public static void Run(
            GameApplicationOptions options,
            Action<GameBuilder> actionConfigureServices
        ) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (actionConfigureServices == null) {
                throw new ArgumentNullException(nameof(actionConfigureServices));
            }
            using (IntegrationGame game = new IntegrationGame(options, actionConfigureServices)) {
                game.Run();
            }
        }
        
        /// <summary>Retained for source compatibility. Use <see cref="Run(Action{GameBuilder})"/> instead.</summary>
        [Obsolete("Use GameApplication.Run instead.")]
        public static void Start(Action<GameBuilder> actionConfigureServices) {
            Run(actionConfigureServices);
        }
    }
}
