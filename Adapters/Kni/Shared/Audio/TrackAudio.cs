using System;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Kni.Audio {
    /// <summary>
    /// Wraps a KNI <see cref="Song"/> as an <see cref="ITrackAudio"/>.
    /// </summary>
    internal sealed class TrackAudio : ITrackAudio {
        /// <summary>The underlying KNI song. </summary>
        internal Song Song { get; }
        
        /// <inheritdoc/>
        public TimeSpan Duration {
            get {
                if (Song == null) {
                    return TimeSpan.Zero;
                }
                return Song.Duration;
            }
        }
        
        /// <summary>Creates a new wrapper around a KNI song. </summary>
        public TrackAudio(Song song) {
            if (song == null) {
                throw new ArgumentNullException(nameof(song));
            }
            Song = song;
        }
    }
}