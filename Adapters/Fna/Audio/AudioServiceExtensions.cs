using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Fna.Audio {
    /// <summary>
    /// Extension methods for <see cref="IAudioService"/> that provide convenient overloads
    /// for playing FNA audio types directly.
    /// </summary>
    public static class AudioServiceExtensions {
        /// <summary>
        /// Plays a FNA <see cref="SoundEffect"/> using the audio service.
        /// </summary>
        /// <param name="service">The audio service instance.</param>
        /// <param name="effectSound">The sound effect to play.</param>
        /// <param name="volume">Volume (0.0 to 1.0). Default is 1.0.</param>
        /// <param name="pitch">Pitch adjustment (-1.0 to 1.0). Default is 0.0.</param>
        /// <param name="pan">Panning (-1.0 left to 1.0 right). Default is 0.0.</param>
        /// <param name="flagLoop">Whether the sound should loop. Default is false.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="effectSound"/> is null.</exception>
        public static void PlaySoundEffect(
            this IAudioService service,
            SoundEffect effectSound,
            float volume = 1f,
            float pitch = 0f,
            float pan = 0f,
            bool flagLoop = false
        ) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (effectSound == null) {
                throw new ArgumentNullException(nameof(effectSound));
            }
            
            service.PlayClipAudio(new ClipAudio(effectSound), volume, pitch, pan, flagLoop);
        }
        
        /// <summary>
        /// Plays a FNA <see cref="Song"/> using the audio service.
        /// </summary>
        /// <param name="service">The audio service instance.</param>
        /// <param name="song">The song to play.</param>
        /// <param name="flagRepeat">Whether the song should repeat. Default is true.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="song"/> is null.</exception>
        public static void PlaySong(
            this IAudioService service,
            Song song,
            bool flagRepeat = true
        ) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (song == null) {
                throw new ArgumentNullException(nameof(song));
            }
            
            service.PlayTrackAudio(new TrackAudio(song), flagRepeat);
        }
    }
}