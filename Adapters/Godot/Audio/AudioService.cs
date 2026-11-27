using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Godot implementation of <see cref="IAudioService"/>.
    /// Manages playback of <see cref="ClipAudio"/> and <see cref="TrackAudio"/>.
    /// Device failures are translated into <see cref="AudioDeviceException"/> so that
    /// game code can degrade gracefully without referencing the platform library.
    /// </summary>
    /// <remarks>
    /// Thread safety: playback tracking and the mute flag are guarded by one lock, and the
    /// Godot audio server state behind <see cref="SongVolume"/> and
    /// <see cref="SoundEffectVolume"/> is process global, shared by every user of the audio server.
    /// Godot server and scene tree calls are only valid on the main thread, so in practice the
    /// service must be driven from the thread that owns the scene tree.
    /// Playback nodes: every sound needs an <see cref="AudioStreamPlayer"/> inside the scene
    /// tree, so the service parents its players under the anchor node supplied at construction
    /// and reuses the players of finished clips from a pool. Music and sound effect volumes
    /// live on dedicated audio server buses that send to the master bus.
    /// </remarks>
    public sealed class AudioService : IAudioService, IDisposable {
        /// <summary>The audio server bus carrying music playback.</summary>
        internal const string MusicBusName = "MGL Music";
        
        /// <summary>The audio server bus carrying sound effect and PCM playback.</summary>
        internal const string SoundEffectsBusName = "MGL Sound Effects";
        
        /// <summary>The audio server bus every service bus sends to.</summary>
        internal const string MasterBusName = "Master";
        
        private readonly object _lock = new object();
        private readonly Node _nodeParent;
        private readonly AudioStreamPlayer _playerTrack;
        private readonly List<ClipPlayer> _listActiveClipPlayers;
        private readonly Stack<ClipPlayer> _stackFreeClipPlayers;
        private int _countClipPlayersCreated;
        private float _volumePreviousSong;
        private float _volumePreviousSoundEffect;
        private bool _flagDisposed;
        private bool _flagIsMuted;
        private bool _flagRepeatTrack;
        
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
                    return GetBusVolume(MusicBusName);
                }
            } set {
                lock (_lock) {
                    if (IsMuted) {
                        return;
                    }
                    SetBusVolume(MusicBusName, Math.Clamp(value, 0f, 1f));
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
                    return GetBusVolume(SoundEffectsBusName);
                }
            } set {
                lock (_lock) {
                    if (IsMuted) {
                        return;
                    }
                    SetBusVolume(SoundEffectsBusName, Math.Clamp(value, 0f, 1f));
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioService"/> class.
        /// </summary>
        /// <param name="nodeParent">The scene tree node under which playback nodes are added.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="nodeParent"/> is null.</exception>
        public AudioService(Node nodeParent) {
            if (nodeParent == null) {
                throw new ArgumentNullException(nameof(nodeParent));
            }
            _nodeParent = nodeParent;
            _listActiveClipPlayers = new List<ClipPlayer>();
            _stackFreeClipPlayers = new Stack<ClipPlayer>();
            EnsureBus(MusicBusName, MasterBusName);
            EnsureBus(SoundEffectsBusName, MasterBusName);
            _playerTrack = new AudioStreamPlayer();
            _playerTrack.Bus = MusicBusName;
            _playerTrack.Finished += HandleTrackFinished;
            _nodeParent.AddChild(_playerTrack);
        }
        
        /// <summary>
        /// Resolves a bus of this adapter on the Godot audio server, creating it under the
        /// supplied send bus when it does not exist yet. Godot routes every player through
        /// server buses, which is where the platform's global volume state lives.
        /// </summary>
        /// <param name="nameBus">The bus name to resolve.</param>
        /// <param name="nameSend">The bus a newly created bus sends to.</param>
        internal static void EnsureBus(string nameBus, string nameSend) {
            if (AudioServer.GetBusIndex(nameBus) >= 0) {
                return;
            }
            AudioServer.AddBus();
            int index = AudioServer.GetBusCount() - 1;
            AudioServer.SetBusName(index, nameBus);
            AudioServer.SetBusSend(index, nameSend);
        }
        
        /// <summary>
        /// Reads the linear volume of a service bus.
        /// </summary>
        /// <param name="nameBus">The bus name.</param>
        /// <returns>The linear volume, or zero when the bus does not exist.</returns>
        private static float GetBusVolume(string nameBus) {
            int index = AudioServer.GetBusIndex(nameBus);
            if (index < 0) {
                return 0f;
            }
            return AudioServer.GetBusVolumeLinear(index);
        }
        
        /// <summary>
        /// Sets the linear volume of a service bus.
        /// </summary>
        /// <param name="nameBus">The bus name.</param>
        /// <param name="volumeLinear">The linear volume.</param>
        private static void SetBusVolume(string nameBus, float volumeLinear) {
            int index = AudioServer.GetBusIndex(nameBus);
            if (index < 0) {
                return;
            }
            AudioServer.SetBusVolumeLinear(index, volumeLinear);
        }
        
        /// <inheritdoc />
        /// <param name="timeFrame">The frame timing information (unused).</param>
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                for (int i = _listActiveClipPlayers.Count - 1; i >= 0; i -= 1) {
                    ClipPlayer playerClip = _listActiveClipPlayers[i];
                    // A looping player restarts through its finished signal, so the brief
                    // gap between finish and restart must not be mistaken for a free player.
                    if (!playerClip.Player.Playing && !playerClip.LoopFlag) {
                        playerClip.Reset();
                        _listActiveClipPlayers.RemoveAt(i);
                        _stackFreeClipPlayers.Push(playerClip);
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
            ClipPlayer playerClip = StartPlayback(audioNativeClip, volume, pitch, pan, flagLoop);
            return new ClipPlayback(playerClip, playerClip.IdPlayback);
        }
        
        /// <summary>
        /// Creates or reuses one pooled clip player and starts a playback. Both playback
        /// entry points run through here, so a disposed service rejects the request with
        /// an <see cref="ObjectDisposedException"/> on the void and the handle path
        /// alike: no silent return and no dead handle is ever produced.
        /// </summary>
        /// <param name="audioNativeClip">The clip to play.</param>
        /// <param name="volume">The volume (0.0 to 1.0).</param>
        /// <param name="pitch">The pitch adjustment (-1.0 to 1.0).</param>
        /// <param name="pan">The panning (-1.0 left to 1.0 right).</param>
        /// <param name="flagLoop">Whether the clip loops.</param>
        /// <returns>The pooled clip player running the playback.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="audioNativeClip"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the clip is not a ClipAudio.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        /// <exception cref="AudioDeviceException">Thrown when the audio device fails.</exception>
        private ClipPlayer StartPlayback(IClipAudio audioNativeClip, float volume, float pitch, float pan, bool flagLoop) {
            if (audioNativeClip == null) {
                throw new ArgumentNullException(nameof(audioNativeClip));
            }
            
            ClipAudio audioClip = audioNativeClip as ClipAudio;
            if (audioClip == null) {
                throw new ArgumentException("Clip must be a ClipAudio.", nameof(audioNativeClip));
            }
            
            ClipPlayer playerClip;
            try {
                lock (_lock) {
                    if (_flagDisposed) {
                        throw new ObjectDisposedException(nameof(AudioService));
                    }
                    if (_stackFreeClipPlayers.Count > 0) {
                        playerClip = _stackFreeClipPlayers.Pop();
                    } else {
                        _countClipPlayersCreated += 1;
                        playerClip = new ClipPlayer(
                            _nodeParent,
                            "MGL Clip Pan " + _countClipPlayersCreated,
                            SoundEffectsBusName
                        );
                    }
                    playerClip.InvalidatePlayback();
                }
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not create a playback instance.", exception);
            }
            
            try {
                playerClip.Player.Stream = audioClip.Stream;
                playerClip.Player.VolumeLinear = Math.Clamp(volume, 0f, 1f);
                playerClip.Player.PitchScale = MathF.Pow(2f, Math.Clamp(pitch, -1f, 1f));
                playerClip.Panner.Pan = Math.Clamp(pan, -1f, 1f);
                playerClip.LoopFlag = flagLoop;
                playerClip.Player.Play(0f);
            } catch (Exception exception) {
                // The player never reached the tracking list, so it must be released
                // here. A device failure is translated; anything else is rethrown as is.
                playerClip.Release();
                if (AudioFailure.IsDeviceFailure(exception)) {
                    throw new AudioDeviceException("The audio device could not play a sound effect.", exception);
                }
                throw;
            }
            
            lock (_lock) {
                if (_flagDisposed) {
                    playerClip.Release();
                    throw new ObjectDisposedException(nameof(AudioService));
                }
                _listActiveClipPlayers.Add(playerClip);
            }
            return playerClip;
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
                _playerTrack.Stop();
                _playerTrack.Stream = audioTrack.Stream;
                _flagRepeatTrack = flagRepeat;
                _playerTrack.Play(0f);
            } catch (Exception exception) when (AudioFailure.IsDeviceFailure(exception)) {
                throw new AudioDeviceException("The audio device could not play a music track.", exception);
            }
        }
        
        /// <summary>
        /// Restarts the track player when the stream ends and repetition is on. This is the
        /// Godot stand-in for the repeating playback flag of the platform player.
        /// </summary>
        private void HandleTrackFinished() {
            if (_flagRepeatTrack && !_flagDisposed) {
                _playerTrack.Play(0f);
            }
        }
        
        /// <inheritdoc />
        public void ToggleMute() {
            lock (_lock) {
                _flagIsMuted = !_flagIsMuted;
                if (_flagIsMuted) {
                    _volumePreviousSong = GetBusVolume(MusicBusName);
                    _volumePreviousSoundEffect = GetBusVolume(SoundEffectsBusName);
                    SetBusVolume(MusicBusName, 0f);
                    SetBusVolume(SoundEffectsBusName, 0f);
                } else {
                    SetBusVolume(MusicBusName, _volumePreviousSong);
                    SetBusVolume(SoundEffectsBusName, _volumePreviousSoundEffect);
                }
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            lock (_lock) {
                foreach (ClipPlayer playerClip in _listActiveClipPlayers) {
                    playerClip.Release();
                }
                _listActiveClipPlayers.Clear();
                foreach (ClipPlayer playerClip in _stackFreeClipPlayers) {
                    playerClip.Release();
                }
                _stackFreeClipPlayers.Clear();
                _playerTrack.Finished -= HandleTrackFinished;
                _playerTrack.Stop();
                _playerTrack.QueueFree();
                _flagDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// One pooled clip player: an <see cref="AudioStreamPlayer"/> inside the scene tree,
        /// routed through a per-player pan bus so that the panning of a clip survives pooling.
        /// </summary>
        private sealed class ClipPlayer {
            private readonly AudioStreamPlayer _player;
            private readonly AudioEffectPanner _panner;
            private readonly string _nameBusPan;
            private bool _flagLoop;
            private bool _flagReleased;
            private int _idPlayback;
            
            internal ClipPlayer(Node nodeParent, string nameBusPan, string nameBusSend) {
                _nameBusPan = nameBusPan;
                EnsureBus(nameBusPan, nameBusSend);
                int index = AudioServer.GetBusIndex(nameBusPan);
                _panner = new AudioEffectPanner();
                AudioServer.AddBusEffect(index, _panner);
                _player = new AudioStreamPlayer();
                _player.Bus = nameBusPan;
                _player.Finished += HandleFinished;
                nodeParent.AddChild(_player);
            }
            
            internal AudioStreamPlayer Player {
                get {
                    return _player;
                }
            }
            
            internal AudioEffectPanner Panner {
                get {
                    return _panner;
                }
            }
            
            internal bool LoopFlag {
                get {
                    return _flagLoop;
                }
                set {
                    _flagLoop = value;
                }
            }
            
            /// <summary>
            /// Gets the identifier of the current playback. It changes whenever the slot
            /// starts a new playback or is reset or released, which turns handles of
            /// earlier playbacks mute.
            /// </summary>
            internal int IdPlayback {
                get {
                    return _idPlayback;
                }
            }
            
            /// <summary>
            /// Starts a new playback generation, invalidating handles of earlier ones.
            /// </summary>
            internal void InvalidatePlayback() {
                _idPlayback += 1;
            }
            
            /// <summary>
            /// Returns true when the supplied playback identifier still identifies the
            /// playback this slot is running.
            /// </summary>
            internal bool MatchesPlayback(int idPlayback) {
                return !_flagReleased && _idPlayback == idPlayback;
            }
            
            /// <summary>
            /// Restarts the player when a looping clip has finished.
            /// </summary>
            private void HandleFinished() {
                if (_flagLoop) {
                    _player.Play(0f);
                }
            }
            
            /// <summary>
            /// Returns the player to its idle state for reuse by a later clip.
            /// </summary>
            internal void Reset() {
                InvalidatePlayback();
                _flagLoop = false;
                _player.Stop();
                _player.Stream = null;
                _player.VolumeLinear = 1f;
                _player.PitchScale = 1f;
                _panner.Pan = 0f;
            }
            
            /// <summary>
            /// Stops and frees the player and its pan bus. The operation is idempotent.
            /// </summary>
            internal void Release() {
                if (_flagReleased) {
                    return;
                }
                _flagReleased = true;
                InvalidatePlayback();
                _player.Finished -= HandleFinished;
                _player.Stop();
                _player.Stream = null;
                _player.QueueFree();
                int index = AudioServer.GetBusIndex(_nameBusPan);
                if (index >= 0) {
                    AudioServer.RemoveBus(index);
                }
            }
        }
        
        /// <summary>
        /// One playback handle over a pooled clip slot. The handle turns mute once the
        /// slot is reused or released, matching the <see cref="IAudioPlayback"/> contract.
        /// </summary>
        private sealed class ClipPlayback : IAudioPlayback {
            private readonly ClipPlayer _playerClip;
            private readonly int _idPlayback;
            
            internal ClipPlayback(ClipPlayer playerClip, int idPlayback) {
                _playerClip = playerClip;
                _idPlayback = idPlayback;
            }
            
            private AudioStreamPlayer GetPlayer() {
                ClipPlayer playerClip = _playerClip;
                if (playerClip == null || !playerClip.MatchesPlayback(_idPlayback)) {
                    return null;
                }
                return playerClip.Player;
            }
            
            /// <inheritdoc />
            public bool IsPlaying {
                get {
                    AudioStreamPlayer player = GetPlayer();
                    if (player == null) {
                        return false;
                    }
                    return player.Playing;
                }
            }
            
            /// <inheritdoc />
            public float Volume {
                get {
                    AudioStreamPlayer player = GetPlayer();
                    if (player == null) {
                        return 0f;
                    }
                    return player.VolumeLinear;
                }
            }
            
            /// <inheritdoc />
            public bool TrySetVolume(float volume) {
                AudioStreamPlayer player = GetPlayer();
                if (player == null) {
                    return false;
                }
                player.VolumeLinear = Math.Clamp(volume, 0f, 1f);
                return true;
            }
            
            /// <inheritdoc />
            public float Pitch {
                get {
                    AudioStreamPlayer player = GetPlayer();
                    if (player == null) {
                        return 0f;
                    }
                    return MathF.Log2(player.PitchScale);
                }
            }
            
            /// <inheritdoc />
            public bool TrySetPitch(float pitch) {
                AudioStreamPlayer player = GetPlayer();
                if (player == null) {
                    return false;
                }
                player.PitchScale = MathF.Pow(2f, Math.Clamp(pitch, -1f, 1f));
                return true;
            }
            
            /// <inheritdoc />
            public float Pan {
                get {
                    ClipPlayer playerClip = _playerClip;
                    if (playerClip == null || !playerClip.MatchesPlayback(_idPlayback)) {
                        return 0f;
                    }
                    return playerClip.Panner.Pan;
                }
            }
            
            /// <inheritdoc />
            public bool TrySetPan(float pan) {
                ClipPlayer playerClip = _playerClip;
                if (playerClip == null || !playerClip.MatchesPlayback(_idPlayback)) {
                    return false;
                }
                playerClip.Panner.Pan = Math.Clamp(pan, -1f, 1f);
                return true;
            }
            
            /// <inheritdoc />
            public void Stop() {
                AudioStreamPlayer player = GetPlayer();
                if (player == null) {
                    return;
                }
                player.Stop();
            }
        }
    }
}
