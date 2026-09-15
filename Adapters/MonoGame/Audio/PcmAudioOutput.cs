using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.MonoGame.Audio {
    /// <summary>
    /// MonoGame implementation of <see cref="IPcmAudioOutput"/> backed by
    /// <see cref="DynamicSoundEffectInstance"/>.
    /// Submitted blocks are copied by the device before this call returns, so the caller
    /// may immediately reuse or return the array to its own pool.
    /// The queued length is real accounting: the frame count of every submitted block is
    /// remembered and dropped once the device reports that the block has been consumed.
    /// Device failures are surfaced as <see cref="AudioDeviceException"/>; a malformed
    /// block is reported as an argument error instead, because that is a caller mistake.
    /// </summary>
    internal sealed class PcmAudioOutput : IPcmAudioOutput {
        private readonly DynamicSoundEffectInstance _instance;
        private readonly Queue<int> _queueFrameCounts = new Queue<int>();
        private readonly int _rateSample;
        private readonly int _countChannel;
        private int _countUnderrun;
        private bool _flagHasSubmitted;
        private bool _flagStarved;
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public int SampleRate {
            get {
                return _rateSample;
            }
        }
        
        /// <inheritdoc />
        public int ChannelCount {
            get {
                return _countChannel;
            }
        }
        
        /// <inheritdoc />
        public PcmSampleFormat Format {
            get {
                return PcmSampleFormat.Pcm16;
            }
        }
        
        /// <inheritdoc />
        public int PendingBufferCount {
            get {
                if (_flagDisposed) {
                    return 0;
                }
                return RefreshQueue();
            }
        }
        
        /// <inheritdoc />
        public int PendingFrameCount {
            get {
                if (_flagDisposed) {
                    return 0;
                }
                RefreshQueue();
                int countFrame = 0;
                foreach (int countFrameBlock in _queueFrameCounts) {
                    countFrame += countFrameBlock;
                }
                return countFrame;
            }
        }
        
        /// <inheritdoc />
        public int UnderrunCount {
            get {
                if (_flagDisposed) {
                    return 0;
                }
                return _countUnderrun;
            }
        }
        
        /// <inheritdoc />
        public bool IsPlaying {
            get {
                if (_flagDisposed) {
                    return false;
                }
                return _instance.State == SoundState.Playing;
            }
        }
        
        /// <inheritdoc />
        public float Volume {
            get {
                if (_flagDisposed) {
                    return 0f;
                }
                return _instance.Volume;
            } set {
                if (_flagDisposed) {
                    return;
                }
                try {
                    _instance.Volume = Math.Clamp(value, 0f, 1f);
                } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                    throw new AudioDeviceException("The audio device rejected a volume change.", exception);
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PcmAudioOutput"/> class.
        /// </summary>
        /// <param name="instance">The device instance to wrap. Ownership transfers to this instance.</param>
        /// <param name="sampleRate">The sample rate in hertz.</param>
        /// <param name="channelCount">The number of interleaved channels.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is null.</exception>
        public PcmAudioOutput(DynamicSoundEffectInstance instance, int sampleRate, int channelCount) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }
            _instance = instance;
            _rateSample = sampleRate;
            _countChannel = channelCount;
        }
        
        /// <summary>
        /// Drops the frame counts of blocks the device has already consumed.
        /// </summary>
        /// <returns>The number of blocks still queued in the device.</returns>
        private int RefreshQueue() {
            int countPending = _instance.PendingBufferCount;
            while (_queueFrameCounts.Count > countPending) {
                _queueFrameCounts.Dequeue();
            }
            return countPending;
        }
        
        /// <inheritdoc />
        public void SubmitBuffer(byte[] dataPcm, int countFrameBuffer) {
            if (_flagDisposed) {
                return;
            }
            if (dataPcm == null) {
                throw new ArgumentNullException(nameof(dataPcm));
            }
            if (countFrameBuffer <= 0) {
                throw new ArgumentOutOfRangeException(nameof(countFrameBuffer));
            }
            int sizeFrame = _countChannel * 2;
            if (dataPcm.Length % sizeFrame != 0) {
                throw new ArgumentException(
                    "A sample block must contain a whole number of frames.", nameof(dataPcm)
                );
            }
            if (dataPcm.Length < countFrameBuffer * sizeFrame) {
                throw new ArgumentException(
                    "The sample block is shorter than the declared frame count requires.", nameof(dataPcm)
                );
            }
            RefreshQueue();
            try {
                _instance.SubmitBuffer(dataPcm);
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device rejected a sample block.", exception);
            }
            _queueFrameCounts.Enqueue(countFrameBuffer);
            _flagHasSubmitted = true;
        }
        
        /// <inheritdoc />
        public void Play() {
            if (_flagDisposed) {
                return;
            }
            try {
                _instance.Play();
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not start playback.", exception);
            }
        }
        
        /// <inheritdoc />
        public void Stop() {
            if (_flagDisposed) {
                return;
            }
            try {
                _instance.Stop();
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not stop playback.", exception);
            }
            _queueFrameCounts.Clear();
            _flagStarved = false;
        }
        
        /// <summary>
        /// Counts one starvation episode when the playing device has consumed every
        /// submitted block. Called once per frame by the audio module.
        /// A stopped device reports an empty queue by definition, so it is not counted.
        /// </summary>
        internal void UpdateUnderrun() {
            if (_flagDisposed || !_flagHasSubmitted) {
                return;
            }
            bool flagEmpty = _instance.State == SoundState.Playing && _instance.PendingBufferCount == 0;
            if (flagEmpty && !_flagStarved) {
                _countUnderrun += 1;
            }
            _flagStarved = flagEmpty;
        }
        
        /// <summary>
        /// Releases the device instance. The operation is idempotent.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            _instance.Dispose();
            _queueFrameCounts.Clear();
            GC.SuppressFinalize(this);
        }
    }
}