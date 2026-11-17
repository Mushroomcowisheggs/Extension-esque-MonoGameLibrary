using System;
using Godot;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Extension methods for <see cref="IAudioService"/> that provide convenient overloads
    /// for playing Godot audio types directly.
    /// </summary>
    public static class AudioServiceExtensions {
        /// <summary>
        /// Plays a Godot <see cref="AudioStream"/> as a sound effect using the audio service.
        /// </summary>
        /// <param name="service">The audio service instance.</param>
        /// <param name="streamAudio">The audio stream to play.</param>
        /// <param name="volume">Volume (0.0 to 1.0). Default is 1.0.</param>
        /// <param name="pitch">Pitch adjustment (-1.0 to 1.0). Default is 0.0.</param>
        /// <param name="pan">Panning (-1.0 left to 1.0 right). Default is 0.0.</param>
        /// <param name="flagLoop">Whether the sound should loop. Default is false.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="streamAudio"/> is null.</exception>
        public static void PlaySoundEffect(
            this IAudioService service,
            AudioStream streamAudio,
            float volume = 1f,
            float pitch = 0f,
            float pan = 0f,
            bool flagLoop = false
        ) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (streamAudio == null) {
                throw new ArgumentNullException(nameof(streamAudio));
            }
            
            service.PlayClipAudio(new ClipAudio(streamAudio), volume, pitch, pan, flagLoop);
        }
        
        /// <summary>
        /// Plays a Godot <see cref="AudioStream"/> as a music track using the audio service.
        /// </summary>
        /// <param name="service">The audio service instance.</param>
        /// <param name="streamAudio">The track to play.</param>
        /// <param name="flagRepeat">Whether the track should repeat. Default is true.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="streamAudio"/> is null.</exception>
        public static void PlaySong(
            this IAudioService service,
            AudioStream streamAudio,
            bool flagRepeat = true
        ) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (streamAudio == null) {
                throw new ArgumentNullException(nameof(streamAudio));
            }
            
            service.PlayTrackAudio(new TrackAudio(streamAudio), flagRepeat);
        }
    }
}
