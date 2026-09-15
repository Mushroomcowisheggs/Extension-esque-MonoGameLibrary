using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.MonoGame.Audio {
    /// <summary>
    /// MonoGame implementation of <see cref="IPcmAudioOutputFactory"/>.
    /// It tracks the outputs it created so that the audio module can advance their
    /// starvation counters once per frame and release any that were not disposed by
    /// their owner when the game shuts down.
    /// </summary>
    internal sealed class PcmAudioOutputFactory : IPcmAudioOutputFactory, IDisposable {
        private readonly object _lock = new object();
        private readonly List<PcmAudioOutput> _listOutputs = new List<PcmAudioOutput>();
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public IPcmAudioOutput Create(int sampleRate, int channelCount) {
            if (sampleRate <= 0) {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }
            if (channelCount <= 0) {
                throw new ArgumentOutOfRangeException(nameof(channelCount));
            }
            AudioChannels channels;
            if (channelCount == 1) {
                channels = AudioChannels.Mono;
            } else if (channelCount == 2) {
                channels = AudioChannels.Stereo;
            } else {
                throw new NotSupportedException("Only one or two channel PCM output is supported.");
            }
            
            DynamicSoundEffectInstance instance;
            try {
                instance = new DynamicSoundEffectInstance(sampleRate, channels);
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not be opened for streaming playback.", exception);
            }
            
            PcmAudioOutput output = new PcmAudioOutput(instance, sampleRate, channelCount);
            lock (_lock) {
                if (_flagDisposed) {
                    // A concurrent shutdown already released every tracked output, so this
                    // one would never be released if it were added to the list now.
                    output.Dispose();
                    throw new ObjectDisposedException(nameof(PcmAudioOutputFactory));
                }
                _listOutputs.Add(output);
            }
            return output;
        }
        
        /// <summary>
        /// Advances the starvation counter of every live output. Called once per frame
        /// by the audio module.
        /// </summary>
        internal void UpdateUnderruns() {
            PcmAudioOutput[] arrayOutputs;
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                arrayOutputs = _listOutputs.ToArray();
            }
            for (int index = 0; index < arrayOutputs.Length; index += 1) {
                arrayOutputs[index].UpdateUnderrun();
            }
        }
        
        /// <summary>
        /// Releases every output that is still tracked. Outputs already disposed by their
        /// owner are skipped because disposal is idempotent.
        /// </summary>
        public void Dispose() {
            PcmAudioOutput[] arrayOutputs;
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                arrayOutputs = _listOutputs.ToArray();
                _listOutputs.Clear();
            }
            for (int index = 0; index < arrayOutputs.Length; index += 1) {
                arrayOutputs[index].Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}