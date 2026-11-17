using System;
using Godot;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Wraps a Godot <see cref="AudioStream"/> as an <see cref="IClipAudio"/>.
    /// Godot has no separate clip resource type: any audio stream can back a clip, and the
    /// clip-versus-track distinction is carried by the playback policy alone.
    /// </summary>
    internal sealed class ClipAudio : IClipAudio {
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
        public ClipAudio(AudioStream streamAudio) {
            if (streamAudio == null) {
                throw new ArgumentNullException(nameof(streamAudio));
            }
            Stream = streamAudio;
        }
    }
}
