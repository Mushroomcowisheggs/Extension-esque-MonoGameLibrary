using System;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Fna.Audio {
    /// <summary>
    /// Wraps a FNA <see cref="SoundEffect"/> as an <see cref="IClipAudio"/>. 
    /// </summary>
    internal sealed class ClipAudio : IClipAudio {
        /// <summary>The underlying FNA sound effect. </summary>
        internal SoundEffect SoundEffect { get; }
        
        /// <inheritdoc/>
        public TimeSpan Duration {
            get {
                if (SoundEffect == null) {
                    return TimeSpan.Zero;
                }
                return SoundEffect.Duration;
            }
        }
        
        /// <summary>Creates a new wrapper around a FNA sound effect. </summary>
        public ClipAudio(SoundEffect effectSound) {
            if (effectSound == null) {
                throw new ArgumentNullException(nameof(effectSound));
            }
            SoundEffect = effectSound;
        }
    }
}