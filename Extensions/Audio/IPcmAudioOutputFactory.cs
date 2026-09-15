namespace MonoGameLibrary.Extensions.Audio {
    /// <summary>
    /// Creates streaming PCM playback devices. The sample rate and channel count are
    /// fixed at creation time; the sample format is always <see cref="PcmSampleFormat.Pcm16"/>.
    /// Outputs are owned by the caller and must be disposed by it; a game that forgets
    /// still has them released when the audio module shuts down.
    /// </summary>
    public interface IPcmAudioOutputFactory {
        /// <summary>
        /// Creates an output device for the supplied sample rate and channel count.
        /// </summary>
        /// <param name="sampleRate">The sample rate in hertz. Must be greater than zero.</param>
        /// <param name="channelCount">
        /// The number of interleaved channels. One (mono) and two (stereo) are supported.
        /// </param>
        /// <returns>The newly created output, which starts in the stopped state.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="sampleRate"/> or <paramref name="channelCount"/> is not positive.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown if more than two channels are requested.</exception>
        /// <exception cref="AudioDeviceException">Thrown if the audio device cannot be opened.</exception>
        IPcmAudioOutput Create(int sampleRate, int channelCount);
    }
}