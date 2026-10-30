using System;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Adapters.Kni.Input;
using MonoGameLibrary.Adapters.Kni.Lifecycle;

namespace MonoGameLibrary.Adapters.Kni.Blazor.GL.Lifecycle {
    /// <summary>
    /// The KNI application bootstrap.
    ///
    /// KNI's Blazor platform does not run a loop of its own. Its ConcreteGame.StartGameLoop is an
    /// empty method with the comment "request next frame", so Run initializes, ticks once and
    /// returns. The page's frame callback advances the game through <see cref="TickRunning"/>
    /// instead, which is why the game is held here for the lifetime of the page and never disposed
    /// on return.
    /// </summary>
    public static class GameApplication {
        private static IntegrationGame _gameRunning;
        
        /// <summary>Advances the running game by one frame. The page's frame callback calls this.</summary>
        public static void TickRunning() {
            IntegrationGame game = _gameRunning;
            if (game != null) {
                game.Tick();
            }
        }
        
        /// <summary>Runs an application with default options.</summary>
        /// <param name="actionConfigureServices">Composes the host's services and modules.</param>
        public static void Run(Action<GameBuilder> actionConfigureServices) {
            Run(new GameApplicationOptions(), actionConfigureServices);
        }
        
        /// <summary>Starts an application. The page's frame callback drives it from here.</summary>
        /// <param name="options">The viewport and timing settings.</param>
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
            IntegrationGame game = new IntegrationGame(options, actionConfigureServices);
            _gameRunning = game;
            game.Run();
        }
        
        /// <summary>Retained for source compatibility. Use <see cref="Run(Action{GameBuilder})"/> instead.</summary>
        [Obsolete("Use GameApplication.Run instead.")]
        public static void Start(Action<GameBuilder> actionConfigureServices) {
            Run(actionConfigureServices);
        }
    }
}
