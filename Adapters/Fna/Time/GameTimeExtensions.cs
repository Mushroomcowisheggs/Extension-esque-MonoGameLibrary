using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Adapters.Fna.Time {
    /// <summary>
    /// Extension methods for converting FNA <see cref="GameTime"/> to <see cref="FrameTime"/>.
    /// </summary>
    public static class GameTimeExtensions {
        /// <summary>
        /// Converts a FNA <see cref="GameTime"/> to a <see cref="FrameTime"/>. 
        /// </summary>
        /// <param name="timeGame">The FNA timing snapshot. </param>
        /// <returns>A new <see cref="FrameTime"/> initialized from <paramref name="timeGame"/>. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="timeGame"/> is null. </exception>
        public static FrameTime ToFrameTime(this GameTime timeGame) {
            if (timeGame == null) { throw new ArgumentNullException(nameof(timeGame)); }
            return new FrameTime(timeGame.TotalGameTime, timeGame.ElapsedGameTime);
        }
    }
}