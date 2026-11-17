using System;
using Godot;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Wraps a Godot <see cref="AudioStream"/> as an <see cref="ITrackAudio"/>.
    /// Godot has no separate song resource type: the track-versus-clip distinction is
    /// carried by the playback policy alone.
    /// </summary>
    internal sealed class TrackAudio : ITrackAudio {
        /// <summary>The underlying Godot audio stream. </summary>
        internal AudioStream Stream { get; }
        
        /// <inheritdoc/>
        public TimeSpan Duration {
            get {
                if (Stream == null) {
                    return TimeSpan.Zero;
                }
                return TimeSpan.FromSeconds(Stream.GetLength());
            }
        }
        
        /// <summary>Creates a new wrapper around a Godot audio stream. </summary>
        public TrackAudio(AudioStream streamAudio) {
            if (streamAudio == null) {
                throw new ArgumentNullException(nameof(streamAudio));
            }
            Stream = streamAudio;
        }
    }
}
