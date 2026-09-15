using System;

namespace MonoGameLibrary.Extensions.Audio {
    /// <summary>
    /// A streaming PCM playback device. The caller decodes or mixes audio on a
    /// background thread and submits finished blocks from the main thread; the
    /// device never touches the decoder.
    /// Every buffer submitted to one output must contain the same number of frames,
    /// which is what lets the queued length be reported in frames as well as in buffers.
    /// After <see cref="IDisposable.Dispose"/> every member is a safe no-op and the
    /// telemetry properties report zero or false, because audio calls routinely race
    /// with shutdown and must not turn a teardown into an exception.
    /// </summary>
    public interface IPcmAudioOutput : IDisposable {
        /// <summary>Gets the sample rate in hertz the output was created with.</summary>
        int SampleRate { get; }
        
        /// <summary>Gets the number of interleaved channels the output was created with.</summary>
        int ChannelCount { get; }
        
        /// <summary>Gets the sample format the output expects.</summary>
        PcmSampleFormat Format { get; }
        
        /// <summary>Gets the number of submitted buffers the device has not consumed yet.</summary>
        int PendingBufferCount { get; }
        
        /// <summary>
        /// Gets the number of frames still queued in the device, derived from
        /// <see cref="PendingBufferCount"/> and the frame count of the last submitted buffer.
        /// </summary>
        int PendingFrameCount { get; }
        
        /// <summary>
        /// Gets the number of times the device ran out of queued audio while it was playing.
        /// </summary>
        int UnderrunCount { get; }
        
        /// <summary>Gets a value indicating whether the device is currently playing.</summary>
        bool IsPlaying { get; }
        
        /// <summary>Gets or sets the playback volume (0.0 to 1.0). Applied to already playing audio.</summary>
        float Volume { get; set; }
        
        /// <summary>
        /// Copies the supplied block into the device queue.
        /// The caller keeps ownership of the array and may reuse it as soon as this returns.
        /// </summary>
        /// <param name="dataPcm">Interleaved sample data in <see cref="Format"/>.</param>
        /// <param name="countFrameBuffer">The number of frames contained in <paramref name="dataPcm"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dataPcm"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="countFrameBuffer"/> is not positive.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the block is shorter than the declared frame count requires, or does not
        /// contain a whole number of frames.
        /// </exception>
        /// <exception cref="AudioDeviceException">Thrown if the device rejects the block.</exception>
        void SubmitBuffer(byte[] dataPcm, int countFrameBuffer);
        
        /// <summary>Starts or resumes playback.</summary>
        /// <exception cref="AudioDeviceException">Thrown if the device refuses to start.</exception>
        void Play();
        
        /// <summary>Stops playback and discards the queued audio.</summary>
        /// <exception cref="AudioDeviceException">Thrown if the device refuses to stop.</exception>
        void Stop();
    }
}