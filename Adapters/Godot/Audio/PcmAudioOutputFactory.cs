using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Godot implementation of <see cref="IPcmAudioOutputFactory"/>.
    /// It tracks the outputs it created so that the audio module can advance their
    /// starvation counters once per frame and release any that were not disposed by
    /// their owner when the game shuts down. Every output owns a dedicated
    /// <see cref="AudioStreamPlayer"/> under the supplied parent node, because a Godot
    /// player only sounds while it is part of the scene tree.
    /// </summary>
    internal sealed class PcmAudioOutputFactory : IPcmAudioOutputFactory, IDisposable {
        private readonly object _lock = new object();
        private readonly List<PcmAudioOutput> _listOutputs = new List<PcmAudioOutput>();
        private readonly Node _nodeParent;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PcmAudioOutputFactory"/> class.
        /// </summary>
        /// <param name="nodeParent">The scene tree node under which output players are added.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="nodeParent"/> is null.</exception>
        public PcmAudioOutputFactory(Node nodeParent) {
            if (nodeParent == null) {
                throw new ArgumentNullException(nameof(nodeParent));
            }
            _nodeParent = nodeParent;
            AudioService.EnsureBus(AudioService.SoundEffectsBusName, AudioService.MasterBusName);
        }
        
        /// <inheritdoc />
        public IPcmAudioOutput Create(int sampleRate, int channelCount) {
            if (sampleRate <= 0) {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }
            if (channelCount <= 0) {
                throw new ArgumentOutOfRangeException(nameof(channelCount));
            }
            if (channelCount == 1) {
                // Mono blocks are duplicated to both generator channels by the output.
            } else if (channelCount == 2) {
                // Stereo blocks map one to one onto generator frames.
            } else {
                throw new NotSupportedException("Only one or two channel PCM output is supported.");
            }
            
            PcmAudioOutput output;
            try {
                AudioStreamGenerator generator = new AudioStreamGenerator();
                generator.MixRate = sampleRate;
                AudioStreamPlayer player = new AudioStreamPlayer();
                player.Stream = generator;
                player.Bus = AudioService.SoundEffectsBusName;
                _nodeParent.AddChild(player);
                output = new PcmAudioOutput(player, sampleRate, channelCount);
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not be opened for streaming playback.", exception);
            }
            
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
