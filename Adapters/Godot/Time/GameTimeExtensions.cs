using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Adapters.Godot.Time {
    /// <summary>
    /// Extension methods for converting Godot frame timing values to <see cref="FrameTime"/>.
    /// </summary>
    public static class GameTimeExtensions {
        /// <summary>
        /// Converts a Godot frame delta in seconds to a <see cref="FrameTime"/>.
        /// </summary>
        /// <param name="deltaSeconds">The engine-provided frame delta in seconds. </param>
        /// <param name="spanTotalTime">Total elapsed time span since start. </param>
        /// <returns>A new <see cref="FrameTime"/> initialized from the two values. </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="deltaSeconds"/> is negative. </exception>
        public static FrameTime ToFrameTime(this double deltaSeconds, TimeSpan spanTotalTime) {
            if (deltaSeconds < 0) { throw new ArgumentOutOfRangeException(nameof(deltaSeconds)); }
            return new FrameTime(spanTotalTime, TimeSpan.FromSeconds(deltaSeconds));
        }
    }
}
