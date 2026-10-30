using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Adapters.Kni.Input {
    /// <summary>
    /// Reads window focus from a running KNI game.
    /// </summary>
    public sealed class GameWindowFocusSource : IWindowFocusSource {
        private readonly Game _game;
        
        /// <summary>
        /// Initializes a source over the given game.
        /// </summary>
        /// <param name="game">The running game. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null. </exception>
        public GameWindowFocusSource(Game game) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            _game = game;
        }
        
        /// <inheritdoc />
        public bool IsActive {
            get { return _game.IsActive; }
        }
    }
}
