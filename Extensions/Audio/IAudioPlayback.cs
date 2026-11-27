using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Extensions.Audio {
    /// <summary>
    /// Handle to one clip playback, allowing live control of a sound started through
    /// <see cref="IAudioService.PlayClipAudioWithHandle"/>. When the underlying voice is
    /// reclaimed by the adapter (for example after a non-looping clip finished), the
    /// handle turns mute: the getters report zero, every <c>TrySet</c> method returns
    /// false and <see cref="Stop"/> does nothing. The <c>TrySet</c> methods are the only
    /// way to change a live playback, and they report failure instead of discarding it.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IAudioPlayback {
        /// <summary>Gets a value indicating whether the playback is still audible.</summary>
        bool IsPlaying { get; }
        
        /// <summary>Gets the playback volume (0.0 to 1.0). Reports 0 when the handle is mute.</summary>
        float Volume { get; }
        
        /// <summary>Gets the playback pitch adjustment (-1.0 to 1.0). Reports 0 when the handle is mute.</summary>
        float Pitch { get; }
        
        /// <summary>Gets the playback panning (-1.0 left to 1.0 right). Reports 0 when the handle is mute.</summary>
        float Pan { get; }
        
        /// <summary>
        /// Attempts to set the playback volume. The value is clamped to 0.0 to 1.0.
        /// </summary>
        /// <param name="volume">The volume to apply.</param>
        /// <returns><c>true</c> when the volume was applied; <c>false</c> when the handle is mute.</returns>
        bool TrySetVolume(float volume);
        
        /// <summary>
        /// Attempts to set the playback pitch adjustment. The value is clamped to -1.0 to 1.0.
        /// </summary>
        /// <param name="pitch">The pitch adjustment to apply.</param>
        /// <returns><c>true</c> when the pitch was applied; <c>false</c> when the handle is mute.</returns>
        bool TrySetPitch(float pitch);
        
        /// <summary>
        /// Attempts to set the playback panning. The value is clamped to -1.0 to 1.0.
        /// </summary>
        /// <param name="pan">The panning to apply.</param>
        /// <returns><c>true</c> when the panning was applied; <c>false</c> when the handle is mute.</returns>
        bool TrySetPan(float pan);
        
        /// <summary>Stops the playback. Does nothing when the handle is mute; idempotent.</summary>
        void Stop();
    }
}
