using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Godot implementation of <see cref="IPcmAudioOutput"/> backed by an
    /// <see cref="AudioStreamGenerator"/> played through a dedicated <see cref="AudioStreamPlayer"/>.
    /// Submitted blocks are converted from the contract's interleaved 16 bit samples to the
    /// generator's stereo frames and copied by the device before this call returns, so the
    /// caller may immediately reuse or return the array to its own pool.
    /// The queued length is real accounting: the frame count of every submitted block is
    /// remembered and dropped once the device has consumed the block, which is derived from
    /// the free space of the generator's ring buffer.
    /// Device failures are surfaced as <see cref="AudioDeviceException"/>; a malformed
    /// block is reported as an argument error instead, because that is a caller mistake.
    /// </summary>
    /// <remarks>
    /// Godot only creates the generator playback once the player starts, so blocks submitted
    /// while the output is stopped are held by this instance and pushed when playback starts.
    /// A block the ring buffer cannot accept while playing is rejected as an
    /// <see cref="AudioDeviceException"/> because the device cannot accept more audio.
    /// </remarks>
    internal sealed class PcmAudioOutput : IPcmAudioOutput {
        private readonly AudioStreamPlayer _player;
        private readonly Queue<int> _queueFrameCounts = new Queue<int>();
        private readonly Queue<Vector2[]> _queueBlocksHeld = new Queue<Vector2[]>();
        private readonly int _rateSample;
        private readonly int _countChannel;
        private AudioStreamGeneratorPlayback _playback;
        private int _countFramePushed;
        private int _countFrameCapacity;
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
                return _player.Playing;
            }
        }
        
        /// <inheritdoc />
        public float Volume {
            get {
                if (_flagDisposed) {
                    return 0f;
                }
                return _player.VolumeLinear;
            } set {
                if (_flagDisposed) {
                    return;
                }
                try {
                    _player.VolumeLinear = Math.Clamp(value, 0f, 1f);
                } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                    throw new AudioDeviceException("The audio device rejected a volume change.", exception);
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PcmAudioOutput"/> class.
        /// </summary>
        /// <param name="player">The device player to wrap. Ownership transfers to this instance.</param>
        /// <param name="sampleRate">The sample rate in hertz.</param>
        /// <param name="channelCount">The number of interleaved channels.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="player"/> is null.</exception>
        public PcmAudioOutput(AudioStreamPlayer player, int sampleRate, int channelCount) {
            if (player == null) {
                throw new ArgumentNullException(nameof(player));
            }
            _player = player;
            _rateSample = sampleRate;
            _countChannel = channelCount;
        }
        
        /// <summary>
        /// Drops the frame counts of blocks the device has already consumed. The consumed
        /// length is derived from the free space of the generator's ring buffer, because the
        /// Godot playback reports no pending block count.
        /// </summary>
        /// <returns>The number of blocks still queued in the device.</returns>
        private int RefreshQueue() {
            if (_playback == null) {
                return _queueFrameCounts.Count;
            }
            int framesQueuedDevice = _countFrameCapacity - _playback.GetFramesAvailable();
            int framesConsumed = _countFramePushed - framesQueuedDevice;
            int framesAccounted = 0;
            while (_queueFrameCounts.Count > 0) {
                int countFrameBlock = _queueFrameCounts.Peek();
                if (framesAccounted + countFrameBlock > framesConsumed) {
                    break;
                }
                framesAccounted += countFrameBlock;
                _queueFrameCounts.Dequeue();
            }
            return _queueFrameCounts.Count;
        }
        
        /// <summary>
        /// Converts interleaved 16 bit samples into the stereo frames the generator expects.
        /// A mono block is duplicated to both output channels.
        /// </summary>
        /// <param name="dataPcm">Interleaved sample data in <see cref="PcmSampleFormat.Pcm16"/>.</param>
        /// <returns>The converted frames.</returns>
        private Vector2[] ConvertToFrames(byte[] dataPcm) {
            int sizeFrame = _countChannel * 2;
            int countFrame = dataPcm.Length / sizeFrame;
            Vector2[] frames = new Vector2[countFrame];
            for (int indexFrame = 0; indexFrame < countFrame; indexFrame += 1) {
                int indexByte = indexFrame * sizeFrame;
                short sampleLeft = (short)(dataPcm[indexByte] | (dataPcm[indexByte + 1] << 8));
                float valueLeft = sampleLeft / 32768f;
                float valueRight = valueLeft;
                if (_countChannel == 2) {
                    short sampleRight = (short)(dataPcm[indexByte + 2] | (dataPcm[indexByte + 3] << 8));
                    valueRight = sampleRight / 32768f;
                }
                frames[indexFrame] = new Vector2(valueLeft, valueRight);
            }
            return frames;
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
            Vector2[] frames = ConvertToFrames(dataPcm);
            if (_playback == null || _queueBlocksHeld.Count > 0) {
                // Godot accepts no samples before playback starts, and blocks queued
                // before an earlier held block must not overtake it, so the block is
                // held and pushed by Play or by the per-frame pump.
                _queueBlocksHeld.Enqueue(frames);
            } else {
                bool flagAccepted;
                try {
                    flagAccepted = _playback.PushBuffer(frames);
                } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                    throw new AudioDeviceException("The audio device rejected a sample block.", exception);
                }
                if (!flagAccepted) {
                    throw new AudioDeviceException("The audio device rejected a sample block.");
                }
                _countFramePushed += frames.Length;
            }
            _queueFrameCounts.Enqueue(countFrameBuffer);
            _flagHasSubmitted = true;
        }
        
        /// <summary>
        /// Pushes the blocks held while the output was stopped into the generator playback.
        /// A block the ring buffer cannot accept yet stays held and is retried on the next frame.
        /// </summary>
        private void FlushHeldBlocks() {
            if (_playback == null) {
                return;
            }
            while (_queueBlocksHeld.Count > 0) {
                Vector2[] frames = _queueBlocksHeld.Peek();
                if (!_playback.PushBuffer(frames)) {
                    break;
                }
                _queueBlocksHeld.Dequeue();
                _countFramePushed += frames.Length;
            }
        }
        
        /// <inheritdoc />
        public void Play() {
            if (_flagDisposed) {
                return;
            }
            try {
                if (!_player.Playing) {
                    // Godot's Play restarts the stream, so it is only invoked on a stopped
                    // player. The playback object is created with the player and the empty
                    // ring buffer reveals its capacity in frames.
                    _player.Play(0f);
                    _playback = _player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                    if (_playback != null) {
                        _countFrameCapacity = _playback.GetFramesAvailable();
                    }
                }
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not start playback.", exception);
            }
            FlushHeldBlocks();
        }
        
        /// <inheritdoc />
        public void Stop() {
            if (_flagDisposed) {
                return;
            }
            try {
                _player.Stop();
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not stop playback.", exception);
            }
            _queueFrameCounts.Clear();
            _queueBlocksHeld.Clear();
            _playback = null;
            _flagStarved = false;
        }
        
        /// <summary>
        /// Pushes any held blocks and counts one starvation episode when the playing device
        /// has consumed every submitted block. Called once per frame by the audio module.
        /// A stopped device reports an empty queue by definition, so it is not counted.
        /// </summary>
        internal void UpdateUnderrun() {
            if (_flagDisposed) {
                return;
            }
            FlushHeldBlocks();
            if (!_flagHasSubmitted) {
                return;
            }
            bool flagEmpty = _player.Playing && PendingBufferCount == 0;
            if (flagEmpty && !_flagStarved) {
                _countUnderrun += 1;
            }
            _flagStarved = flagEmpty;
        }
        
        /// <summary>
        /// Releases the device player. The operation is idempotent.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            _player.Stop();
            _player.QueueFree();
            _queueFrameCounts.Clear();
            _queueBlocksHeld.Clear();
            _playback = null;
            GC.SuppressFinalize(this);
        }
    }
}
