using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>Bootstraps a fully integrated MonoGame application.</summary>
    public static class GameApplication {
        public static void Run(Action<GameBuilder> actionConfigureServices) {
            Run(new GameApplicationOptions(), actionConfigureServices);
        }
        
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
        
        [Obsolete("Use GameApplication.Run instead.")]
        public static void Start(Action<GameBuilder> actionConfigureServices) {
            Run(actionConfigureServices);
        }
    }
}
