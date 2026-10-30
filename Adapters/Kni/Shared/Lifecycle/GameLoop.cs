using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Core.Lifecycle;

namespace MonoGameLibrary.Adapters.Kni.Lifecycle {
    /// <summary>
    /// Adapts KNI's internal game loop to the IGameLoop interface.
    /// Creates the window, GraphicsDevice, and runs the update/draw cycle.
    /// </summary>
    public class GameLoop : IGameLoop {
        private readonly Game _game;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a new instance with the given Game subclass.
        /// </summary>
        /// <param name="game">The KNI Game instance (typically created by the adapter).</param>
        /// <exception cref="ArgumentNullException">Thrown if game is null.</exception>
        public GameLoop(Game game) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            _game = game;
        }
        
        /// <inheritdoc />
        public void Run() {
            _game.Run();
        }
        
        /// <inheritdoc />
        public void Exit() {
            _game.Exit();
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) { return; }
            _game.Dispose();
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}