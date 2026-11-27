using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Kni.Audio {
    /// <summary>
    /// KNI implementation of <see cref="IAudioService"/>. 
    /// Manages playback of <see cref="ClipAudio"/> and <see cref="TrackAudio"/>. 
    /// Device failures are translated into <see cref="AudioDeviceException"/> so that
    /// game code can degrade gracefully without referencing the platform library.
    /// </summary>
    /// <remarks>
    /// Thread safety: playback tracking, the mute flag and the KNI global volume state
    /// (<c>MediaPlayer.Volume</c>, <c>SoundEffect.MasterVolume</c>) are all guarded by one lock,
    /// so volume access, <see cref="ToggleMute"/> and <see cref="Update"/> are safe to call from
    /// different threads.
    /// </remarks>
    public sealed class AudioService : IAudioService, IDisposable {
        private readonly object _lock = new object();
        private readonly List<SoundEffectInstance> _listActiveSoundEffectInstances;
        private float _volumePreviousSong;
        private float _volumePreviousSoundEffect;
        private bool _flagDisposed;
        private bool _flagIsMuted;
        
        /// <summary>
        /// Gets a value that indicates whether audio is muted.
        /// </summary>
        public bool IsMuted {
            get { lock (_lock) { return _flagIsMuted; } }
            private set { lock (_lock) { _flagIsMuted = value; } }
        }
        
        /// <summary>
        /// Gets or sets the global volume for music. 
        /// </summary>
        public float SongVolume {
            get {
                lock (_lock) {
                    if (IsMuted) {
                        return 0f;
                    }
                    return MediaPlayer.Volume;
                }
            } set {
                lock (_lock) {
                    if (IsMuted) {
                        return;
                    }
                    MediaPlayer.Volume = Math.Clamp(value, 0f, 1f);
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the global volume for sound effects (0.0 to 1.0). 
        /// Returns 0 when muted. 
        /// </summary>
        public float SoundEffectVolume {
            get {
                lock (_lock) {
                    if (IsMuted) {
                        return 0f;
                    }
                    return SoundEffect.MasterVolume;
                }
            } set {
                lock (_lock) {
                    if (IsMuted) {
                        return;
                    }
                    SoundEffect.MasterVolume = Math.Clamp(value, 0f, 1f);
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioService"/> class. 
        /// </summary>
        public AudioService(
        ) {
            _listActiveSoundEffectInstances = new List<SoundEffectInstance>();
        }
        
        /// <inheritdoc />
        /// <param name="timeFrame">The frame timing information (unused).</param>
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                for (int i = _listActiveSoundEffectInstances.Count - 1; i >= 0; i -= 1) {
                    var instance = _listActiveSoundEffectInstances[i];
                    if (instance.State == SoundState.Stopped) {
                        instance.Dispose();
                        _listActiveSoundEffectInstances.RemoveAt(i);
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public void PlayClipAudio(IClipAudio audioNativeClip, float volume = 1f, float pitch = 0f, float pan = 0f, bool flagLoop = false) {
            StartPlayback(audioNativeClip, volume, pitch, pan, flagLoop);
        }
        
        /// <inheritdoc />
        public IAudioPlayback PlayClipAudioWithHandle(IClipAudio audioNativeClip, float volume = 1f, float pitch = 0f, float pan = 0f, bool flagLoop = false) {
            SoundEffectInstance instance = StartPlayback(audioNativeClip, volume, pitch, pan, flagLoop);
            return new SoundEffectPlayback(instance);
        }
        
        /// <summary>
        /// Creates and starts one playback instance. Both playback entry points run
        /// through here, so a disposed service rejects the request with an
        /// <see cref="ObjectDisposedException"/> on the void and the handle path alike:
        /// no silent return and no dead handle is ever produced.
        /// </summary>
        /// <param name="audioNativeClip">The clip to play.</param>
        /// <param name="volume">The volume (0.0 to 1.0).</param>
        /// <param name="pitch">The pitch adjustment (-1.0 to 1.0).</param>
        /// <param name="pan">The panning (-1.0 left to 1.0 right).</param>
        /// <param name="flagLoop">Whether the clip loops.</param>
        /// <returns>The started native playback instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="audioNativeClip"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the clip is not a ClipAudio.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        /// <exception cref="AudioDeviceException">Thrown when the audio device fails.</exception>
        private SoundEffectInstance StartPlayback(IClipAudio audioNativeClip, float volume, float pitch, float pan, bool flagLoop) {
            if (audioNativeClip == null) {
                throw new ArgumentNullException(nameof(audioNativeClip));
            }
            
            ClipAudio audioClip = audioNativeClip as ClipAudio;
            if (audioClip == null) {
                throw new ArgumentException("Clip must be a ClipAudio.", nameof(audioNativeClip));
            }
            
            SoundEffectInstance instance;
            try {
                instance = audioClip.SoundEffect.CreateInstance();
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not create a playback instance.", exception);
            }
            try {
                instance.Volume = Math.Clamp(volume, 0f, 1f);
                instance.Pitch = Math.Clamp(pitch, -1f, 1f);
                instance.Pan = Math.Clamp(pan, -1f, 1f);
                instance.IsLooped = flagLoop;
                instance.Play();
            } catch (Exception exception) {
                // The instance never reached the tracking list, so it must be released
                // here. A device failure is translated; anything else is rethrown as is.
                instance.Dispose();
                if (AudioFailure.IsDeviceFailure(exception)) {
                    throw new AudioDeviceException("The audio device could not play a sound effect.", exception);
                }
                throw;
            }
            
            lock (_lock) {
                if (_flagDisposed) {
                    instance.Dispose();
                    throw new ObjectDisposedException(nameof(AudioService));
                }
                _listActiveSoundEffectInstances.Add(instance);
            }
            return instance;
        }
        
        /// <inheritdoc />
        public void PlayTrackAudio(ITrackAudio audioNativeTrack, bool flagRepeat = true) {
            if (audioNativeTrack == null) {
                throw new ArgumentNullException(nameof(audioNativeTrack));
            }
            
            TrackAudio audioTrack = audioNativeTrack as TrackAudio;
            if (audioTrack == null) {
                throw new ArgumentException("Track must be a TrackAudio.", nameof(audioNativeTrack));
            }
            
            try {
                MediaPlayer.Stop();
                MediaPlayer.Play(audioTrack.Song);
                MediaPlayer.IsRepeating = flagRepeat;
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not play a music track.", exception);
            }
        }
        
        /// <inheritdoc />
        public void ToggleMute() {
            lock (_lock) {
                _flagIsMuted = !_flagIsMuted;
                if (_flagIsMuted) {
                    _volumePreviousSong = MediaPlayer.Volume;
                    _volumePreviousSoundEffect = SoundEffect.MasterVolume;
                    MediaPlayer.Volume = 0f;
                    SoundEffect.MasterVolume = 0f;
                } else {
                    MediaPlayer.Volume = _volumePreviousSong;
                    SoundEffect.MasterVolume = _volumePreviousSoundEffect;
                }
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            lock (_lock) {
                foreach (var instance in _listActiveSoundEffectInstances) {
                    instance.Dispose();
                }
                _listActiveSoundEffectInstances.Clear();
                _flagDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// One playback handle over a native sound effect instance. The handle turns
        /// mute once the instance is disposed by the service (after it stopped or when
        /// the service shuts down), matching the <see cref="IAudioPlayback"/> contract.
        /// </summary>
        private sealed class SoundEffectPlayback : IAudioPlayback {
            private SoundEffectInstance _instance;
            
            internal SoundEffectPlayback(SoundEffectInstance instance) {
                _instance = instance;
            }
            
            /// <inheritdoc />
            public bool IsPlaying {
                get {
                    SoundEffectInstance instance = _instance;
                    if (instance == null) {
                        return false;
                    }
                    if (instance.IsDisposed) {
                        return false;
                    }
                    return instance.State == SoundState.Playing;
                }
            }
            
            /// <inheritdoc />
            public float Volume {
                get {
                    SoundEffectInstance instance = _instance;
                    if (instance == null) {
                        return 0f;
                    }
                    if (instance.IsDisposed) {
                        return 0f;
                    }
                    return instance.Volume;
                }
            }
            
            /// <inheritdoc />
            public bool TrySetVolume(float volume) {
                SoundEffectInstance instance = _instance;
                if (instance == null || instance.IsDisposed) {
                    return false;
                }
                instance.Volume = Math.Clamp(volume, 0f, 1f);
                return true;
            }
            
            /// <inheritdoc />
            public float Pitch {
                get {
                    SoundEffectInstance instance = _instance;
                    if (instance == null) {
                        return 0f;
                    }
                    if (instance.IsDisposed) {
                        return 0f;
                    }
                    return instance.Pitch;
                }
            }
            
            /// <inheritdoc />
            public bool TrySetPitch(float pitch) {
                SoundEffectInstance instance = _instance;
                if (instance == null || instance.IsDisposed) {
                    return false;
                }
                instance.Pitch = Math.Clamp(pitch, -1f, 1f);
                return true;
            }
            
            /// <inheritdoc />
            public float Pan {
                get {
                    SoundEffectInstance instance = _instance;
                    if (instance == null) {
                        return 0f;
                    }
                    if (instance.IsDisposed) {
                        return 0f;
                    }
                    return instance.Pan;
                }
            }
            
            /// <inheritdoc />
            public bool TrySetPan(float pan) {
                SoundEffectInstance instance = _instance;
                if (instance == null || instance.IsDisposed) {
                    return false;
                }
                instance.Pan = Math.Clamp(pan, -1f, 1f);
                return true;
            }
            
            /// <inheritdoc />
            public void Stop() {
                SoundEffectInstance instance = _instance;
                if (instance == null || instance.IsDisposed) {
                    return;
                }
                instance.Stop();
            }
        }
    }
}