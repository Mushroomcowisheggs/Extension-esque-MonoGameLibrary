using System;
using System.Collections.Generic;
using Godot;
using GodotInput = global::Godot.Input;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Godot.Input {
    /// <summary>
    /// Godot implementation of <see cref="IInputService"/>. 
    /// </summary>
    /// <remarks>
    /// Thread safety: <see cref="Update"/> samples the hardware and publishes one consistent
    /// input snapshot; every query reads that snapshot under the same lock, so queries may run
    /// on a different thread than <see cref="Update"/> (for example from a concurrent Draw)
    /// without seeing torn keyboard, gamepad or thumbstick state. The sampling itself calls
    /// into the Godot input singleton and therefore has to run on the frame thread.
    /// </remarks>
    public sealed class InputService : IInputService, IDisposable {
        private readonly object _lock = new object();
        private readonly Dictionary<PlayerIndex, GamePadSnapshot> _dictionaryCurrentGamePadStates;
        private readonly Dictionary<PlayerIndex, GamePadSnapshot> _dictionaryPreviousGamePadStates;
        private readonly Dictionary<PlayerIndex, int> _dictionaryPlayerDevices;
        private int _countFrame;
        private bool _flagDisposed = false;
        private readonly IWindowFocusSource _sourceFocus;
        private bool _flagFocused;
        private readonly HashSet<Key> _setKeyboardCurrent = new HashSet<Key>();
        private readonly HashSet<Key> _setKeyboardPrevious = new HashSet<Key>();
        private const float DEAD_ZONE = 0.24f;
        
        /// <summary>
        /// The keys sampled every frame. The set is derived from the key codes the
        /// translation table can produce, so a key no query can ask for is never sampled.
        /// </summary>
        private static readonly Key[] KEYS_SAMPLED = BuildSampledKeys();
        
        /// <summary>
        /// The physical gamepad buttons sampled every frame; the thumbstick directions
        /// are derived from the sampled axes instead.
        /// </summary>
        private static readonly JoyButton[] BUTTONS_PHYSICAL = new JoyButton[] {
            JoyButton.A,
            JoyButton.B,
            JoyButton.X,
            JoyButton.Y,
            JoyButton.Start,
            JoyButton.Back,
            JoyButton.LeftStick,
            JoyButton.RightStick,
            JoyButton.LeftShoulder,
            JoyButton.RightShoulder,
            JoyButton.DpadUp,
            JoyButton.DpadDown,
            JoyButton.DpadLeft,
            JoyButton.DpadRight
        };
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InputService"/> class.
        /// </summary>
        public InputService() : this(new AlwaysFocusedWindowFocusSource()) {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InputService"/> class over a focus source.
        /// </summary>
        /// <param name="sourceFocus">The focus source consulted before a key is accepted. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceFocus"/> is null. </exception>
        public InputService(IWindowFocusSource sourceFocus) {
            if (sourceFocus == null) {
                throw new ArgumentNullException(nameof(sourceFocus));
            }
            _sourceFocus = sourceFocus;
            _dictionaryCurrentGamePadStates = new Dictionary<PlayerIndex, GamePadSnapshot>();
            _dictionaryPreviousGamePadStates = new Dictionary<PlayerIndex, GamePadSnapshot>();
            _dictionaryPlayerDevices = new Dictionary<PlayerIndex, int>();
            
            // Pre-populate with initial states for all players
            UpdatePlayerDevices();
            foreach (PlayerIndex indexPlayer in Enum.GetValues(typeof(PlayerIndex))) {
                GamePadSnapshot snapshotCurrent = new GamePadSnapshot();
                GamePadSnapshot snapshotPrevious = new GamePadSnapshot();
                SampleGamePad(indexPlayer, snapshotCurrent);
                snapshotPrevious.CopyFrom(snapshotCurrent);
                _dictionaryCurrentGamePadStates[indexPlayer] = snapshotCurrent;
                _dictionaryPreviousGamePadStates[indexPlayer] = snapshotPrevious;
            }
        }
        
        /// <summary>
        /// Gets the number of frames processed by the manager.
        /// </summary>
        public int FrameCount {
            get {
                lock (_lock) {
                    return _countFrame;
                }
            }
        }
        
        /// <summary>
        /// Updates the input state for the current frame. 
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame. </param>
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                bool flagFocused = _sourceFocus.IsActive;
                if (!flagFocused || !_flagFocused) {
                    // Losing focus must not leave a key latched down, and the frame that regains
                    // focus only re-baselines, so neither a press nor a release edge is reported.
                    _setKeyboardPrevious.Clear();
                    _setKeyboardCurrent.Clear();
                    if (flagFocused) {
                        SampleKeyboard(_setKeyboardCurrent);
                        CopyKeys(_setKeyboardCurrent, _setKeyboardPrevious);
                    }
                } else {
                    _setKeyboardPrevious.Clear();
                    CopyKeys(_setKeyboardCurrent, _setKeyboardPrevious);
                    _setKeyboardCurrent.Clear();
                    SampleKeyboard(_setKeyboardCurrent);
                }
                _flagFocused = flagFocused;
                
                UpdatePlayerDevices();
                foreach (PlayerIndex indexPlayer in Enum.GetValues(typeof(PlayerIndex))) {
                    GamePadSnapshot snapshotCurrent = _dictionaryCurrentGamePadStates[indexPlayer];
                    _dictionaryPreviousGamePadStates[indexPlayer].CopyFrom(snapshotCurrent);
                    SampleGamePad(indexPlayer, snapshotCurrent);
                }
                
                _countFrame += 1;
            }
        }
        
        /// <inheritdoc />
        public bool IsKeyDown(KeyCode codeKey) {
            Key key = KeyCodeConverter.ToGodotKey(codeKey);
            lock (_lock) {
                return _setKeyboardCurrent.Contains(key);
            }
        }
        
        /// <inheritdoc />
        public bool IsKeyUp(KeyCode codeKey) {
            Key key = KeyCodeConverter.ToGodotKey(codeKey);
            lock (_lock) {
                return !_setKeyboardCurrent.Contains(key);
            }
        }
        
        /// <inheritdoc />
        public bool WasKeyJustPressed(KeyCode codeKey) {
            Key key = KeyCodeConverter.ToGodotKey(codeKey);
            lock (_lock) {
                return _setKeyboardCurrent.Contains(key) && !_setKeyboardPrevious.Contains(key);
            }
        }
        
        /// <inheritdoc />
        public bool WasKeyJustReleased(KeyCode codeKey) {
            Key key = KeyCodeConverter.ToGodotKey(codeKey);
            lock (_lock) {
                return !_setKeyboardCurrent.Contains(key) && _setKeyboardPrevious.Contains(key);
            }
        }
        
        /// <summary>
        /// Converts a platform-independent <see cref="GamePadButton"/> to a Godot <see cref="JoyButton"/>. 
        /// The thumbstick directions have no button counterpart and are handled by the callers
        /// through the sampled axes; they fall through to the default case here.
        /// </summary>
        /// <param name="button">The platform-independent button. </param>
        /// <returns>The corresponding Godot button. </returns>
        private static JoyButton ConvertButton(GamePadButton button) {
            switch (button) {
                case GamePadButton.A: return JoyButton.A;
                case GamePadButton.B: return JoyButton.B;
                case GamePadButton.X: return JoyButton.X;
                case GamePadButton.Y: return JoyButton.Y;
                case GamePadButton.Start: return JoyButton.Start;
                case GamePadButton.Back: return JoyButton.Back;
                case GamePadButton.LeftStick: return JoyButton.LeftStick;
                case GamePadButton.RightStick: return JoyButton.RightStick;
                case GamePadButton.LeftShoulder: return JoyButton.LeftShoulder;
                case GamePadButton.RightShoulder: return JoyButton.RightShoulder;
                case GamePadButton.DPadUp: return JoyButton.DpadUp;
                case GamePadButton.DPadDown: return JoyButton.DpadDown;
                case GamePadButton.DPadLeft: return JoyButton.DpadLeft;
                case GamePadButton.DPadRight: return JoyButton.DpadRight;
                default: return JoyButton.A;
            }
        }
        
        /// <inheritdoc />
        public bool IsButtonDown(PlayerIndex indexPlayer, GamePadButton button) {
            lock (_lock) {
                switch (button) {
                    case GamePadButton.LeftThumbstickUp:
                    return IsThumbstickDirectionActive(GetLeftThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Up);
                    case GamePadButton.LeftThumbstickDown:
                    return IsThumbstickDirectionActive(GetLeftThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Down);
                    case GamePadButton.LeftThumbstickLeft:
                    return IsThumbstickDirectionActive(GetLeftThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Left);
                    case GamePadButton.LeftThumbstickRight:
                    return IsThumbstickDirectionActive(GetLeftThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Right);
                    
                    case GamePadButton.RightThumbstickUp:
                    return IsThumbstickDirectionActive(GetRightThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Up);
                    case GamePadButton.RightThumbstickDown:
                    return IsThumbstickDirectionActive(GetRightThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Down);
                    case GamePadButton.RightThumbstickLeft:
                    return IsThumbstickDirectionActive(GetRightThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Left);
                    case GamePadButton.RightThumbstickRight:
                    return IsThumbstickDirectionActive(GetRightThumbstick(indexPlayer), DEAD_ZONE, AxisDirection.Right);
                    
                    default:
                    JoyButton buttonJoy = ConvertButton(button);
                    return GetGamePadState(indexPlayer).IsButtonDown(buttonJoy);
                }
            }
        }
        
        /// <inheritdoc />
        public bool WasButtonJustPressed(PlayerIndex indexPlayer, GamePadButton button) {
            lock (_lock) {
                GamePadSnapshot snapshotCurrent = GetGamePadState(indexPlayer);
                GamePadSnapshot snapshotPrevious = GetPreviousGamePadState(indexPlayer);
                
                switch (button) {
                    case GamePadButton.A:
                    case GamePadButton.B:
                    case GamePadButton.X:
                    case GamePadButton.Y:
                    case GamePadButton.Start:
                    case GamePadButton.Back:
                    case GamePadButton.LeftStick:
                    case GamePadButton.RightStick:
                    case GamePadButton.LeftShoulder:
                    case GamePadButton.RightShoulder:
                    case GamePadButton.DPadUp:
                    case GamePadButton.DPadDown:
                    case GamePadButton.DPadLeft:
                    case GamePadButton.DPadRight:
                    JoyButton buttonJoy = ConvertButton(button);
                    return snapshotCurrent.IsButtonDown(buttonJoy) && !snapshotPrevious.IsButtonDown(buttonJoy);
                    
                    case GamePadButton.LeftThumbstickUp:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Up);
                    case GamePadButton.LeftThumbstickDown:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Down);
                    case GamePadButton.LeftThumbstickLeft:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Left);
                    case GamePadButton.LeftThumbstickRight:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Right);
                    
                    case GamePadButton.RightThumbstickUp:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Up);
                    case GamePadButton.RightThumbstickDown:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Down);
                    case GamePadButton.RightThumbstickLeft:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Left);
                    case GamePadButton.RightThumbstickRight:
                    return WasThumbstickDirectionJustPressed(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Right);
                    
                    default:
                    return false;
                }
            }
        }
        
        /// <inheritdoc />
        public bool WasButtonJustReleased(PlayerIndex indexPlayer, GamePadButton button) {
            lock (_lock) {
                GamePadSnapshot snapshotCurrent = GetGamePadState(indexPlayer);
                GamePadSnapshot snapshotPrevious = GetPreviousGamePadState(indexPlayer);
                
                switch (button) {
                    case GamePadButton.LeftThumbstickUp:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Up);
                    case GamePadButton.LeftThumbstickDown:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Down);
                    case GamePadButton.LeftThumbstickLeft:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Left);
                    case GamePadButton.LeftThumbstickRight:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.LeftThumbstick, snapshotCurrent.LeftThumbstick, DEAD_ZONE, AxisDirection.Right);
                    
                    case GamePadButton.RightThumbstickUp:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Up);
                    case GamePadButton.RightThumbstickDown:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Down);
                    case GamePadButton.RightThumbstickLeft:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Left);
                    case GamePadButton.RightThumbstickRight:
                    return WasThumbstickDirectionJustReleased(snapshotPrevious.RightThumbstick, snapshotCurrent.RightThumbstick, DEAD_ZONE, AxisDirection.Right);
                    
                    default:
                    JoyButton buttonJoy = ConvertButton(button);
                    return !snapshotCurrent.IsButtonDown(buttonJoy) && snapshotPrevious.IsButtonDown(buttonJoy);
                }
            }
        }
        
        /// <summary>
        /// Determines whether a thumbstick is currently pushed past the dead zone in a specific direction.
        /// </summary>
        /// <param name="directionThumbstick">The thumbstick position.</param>
        /// <param name="deadZone">The dead zone threshold.</param>
        /// <param name="direction">The direction to check (Up, Down, Left, Right).</param>
        /// <returns>True if the thumbstick is past the dead zone in the given direction; otherwise, false.</returns>
        private static bool IsThumbstickDirectionActive(Vector2 directionThumbstick, float deadZone, AxisDirection direction) {
            switch (direction) {
                case AxisDirection.Up:
                return directionThumbstick.Y > deadZone && Math.Abs(directionThumbstick.Y) > Math.Abs(directionThumbstick.X);
                case AxisDirection.Down:
                return directionThumbstick.Y < -deadZone && Math.Abs(directionThumbstick.Y) > Math.Abs(directionThumbstick.X);
                case AxisDirection.Left:
                return directionThumbstick.X < -deadZone && Math.Abs(directionThumbstick.X) > Math.Abs(directionThumbstick.Y);
                case AxisDirection.Right:
                return directionThumbstick.X > deadZone && Math.Abs(directionThumbstick.X) > Math.Abs(directionThumbstick.Y);
                default:
                return false;
            }
        }
        
        /// <summary>
        /// Determines whether a thumbstick was just pushed past the dead zone in a specific direction.
        /// </summary>
        /// <param name="previous">The thumbstick position from the previous frame.</param>
        /// <param name="current">The thumbstick position from the current frame.</param>
        /// <param name="deadZone">The dead zone threshold.</param>
        /// <param name="direction">The direction to check (Up, Down, Left, Right).</param>
        /// <returns>True if the thumbstick just crossed the dead zone in the specified direction; otherwise, false.</returns>
        private static bool WasThumbstickDirectionJustPressed(Vector2 previous, Vector2 current, float deadZone, AxisDirection direction) {
            switch (direction) {
                case AxisDirection.Up:
                return previous.Y <= deadZone && current.Y > deadZone && Math.Abs(current.Y) > Math.Abs(current.X);
                case AxisDirection.Down:
                return previous.Y >= -deadZone && current.Y < -deadZone && Math.Abs(current.Y) > Math.Abs(current.X);
                case AxisDirection.Left:
                return previous.X >= -deadZone && current.X < -deadZone && Math.Abs(current.X) > Math.Abs(current.Y);
                case AxisDirection.Right:
                return previous.X <= deadZone && current.X > deadZone && Math.Abs(current.X) > Math.Abs(current.Y);
                default:
                return false;
            }
        }
        
        /// <summary>
        /// Determines whether a thumbstick fell back inside the dead zone in a specific direction this frame.
        /// </summary>
        /// <param name="previous">The thumbstick position from the previous frame.</param>
        /// <param name="current">The thumbstick position from the current frame.</param>
        /// <param name="deadZone">The dead zone threshold.</param>
        /// <param name="direction">The direction to check (Up, Down, Left, Right).</param>
        /// <returns>True if the thumbstick just left the specified direction; otherwise, false.</returns>
        private static bool WasThumbstickDirectionJustReleased(Vector2 previous, Vector2 current, float deadZone, AxisDirection direction) {
            switch (direction) {
                case AxisDirection.Up:
                return previous.Y > deadZone && Math.Abs(previous.Y) > Math.Abs(previous.X) && current.Y <= deadZone;
                case AxisDirection.Down:
                return previous.Y < -deadZone && Math.Abs(previous.Y) > Math.Abs(previous.X) && current.Y >= -deadZone;
                case AxisDirection.Left:
                return previous.X < -deadZone && Math.Abs(previous.X) > Math.Abs(previous.Y) && current.X >= -deadZone;
                case AxisDirection.Right:
                return previous.X > deadZone && Math.Abs(previous.X) > Math.Abs(previous.Y) && current.X <= deadZone;
                default:
                return false;
            }
        }
        
        private Vector2 GetLeftThumbstick(PlayerIndex indexPlayer) {
            return GetGamePadState(indexPlayer).LeftThumbstick;
        }
        
        private Vector2 GetRightThumbstick(PlayerIndex indexPlayer) {
            return GetGamePadState(indexPlayer).RightThumbstick;
        }
        
        /// <summary>
        /// Defines cardinal directions for thumbstick axis detection.
        /// </summary>
        private enum AxisDirection { Up, Down, Left, Right }
        
        /// <summary>
        /// Builds the sampled key set from the keys the translation table can produce.
        /// </summary>
        /// <returns>The distinct Godot keys to sample every frame.</returns>
        private static Key[] BuildSampledKeys() {
            List<Key> listKeys = new List<Key>();
            foreach (KeyCode codeKey in Enum.GetValues(typeof(KeyCode))) {
                Key key = KeyCodeConverter.ToGodotKey(codeKey);
                if (key != Key.None && !listKeys.Contains(key)) {
                    listKeys.Add(key);
                }
            }
            return listKeys.ToArray();
        }
        
        /// <summary>
        /// Copies one keyboard snapshot into another.
        /// </summary>
        /// <param name="setSource">The snapshot to copy from.</param>
        /// <param name="setTarget">The snapshot to copy into.</param>
        private static void CopyKeys(HashSet<Key> setSource, HashSet<Key> setTarget) {
            foreach (Key key in setSource) {
                setTarget.Add(key);
            }
        }
        
        /// <summary>
        /// Samples the pressed keys into the given snapshot.
        /// </summary>
        /// <param name="setTarget">The snapshot to fill.</param>
        private static void SampleKeyboard(HashSet<Key> setTarget) {
            foreach (Key key in KEYS_SAMPLED) {
                if (GodotInput.IsKeyPressed(key)) {
                    setTarget.Add(key);
                }
            }
        }
        
        /// <summary>
        /// Rebuilds the player to joypad device mapping. Player N reads the Nth
        /// connected joypad, and a player without a connected joypad reads as
        /// fully released.
        /// </summary>
        private void UpdatePlayerDevices() {
            _dictionaryPlayerDevices.Clear();
            int indexDevice = 0;
            foreach (int device in GodotInput.GetConnectedJoypads()) {
                if (indexDevice > (int)PlayerIndex.Four) {
                    break;
                }
                _dictionaryPlayerDevices[(PlayerIndex)indexDevice] = device;
                indexDevice += 1;
            }
        }
        
        /// <summary>
        /// Samples the gamepad of the given player into the given snapshot.
        /// </summary>
        /// <param name="indexPlayer">The player whose joypad is sampled.</param>
        /// <param name="snapshot">The snapshot to fill.</param>
        private void SampleGamePad(PlayerIndex indexPlayer, GamePadSnapshot snapshot) {
            snapshot.Clear();
            int device;
            if (!_dictionaryPlayerDevices.TryGetValue(indexPlayer, out device)) {
                return;
            }
            snapshot.LeftThumbstick = new Vector2(
                GodotInput.GetJoyAxis(device, JoyAxis.LeftX),
                GodotInput.GetJoyAxis(device, JoyAxis.LeftY)
            );
            snapshot.RightThumbstick = new Vector2(
                GodotInput.GetJoyAxis(device, JoyAxis.RightX),
                GodotInput.GetJoyAxis(device, JoyAxis.RightY)
            );
            foreach (JoyButton buttonJoy in BUTTONS_PHYSICAL) {
                if (GodotInput.IsJoyButtonPressed(device, buttonJoy)) {
                    snapshot.AddButton(buttonJoy);
                }
            }
        }
        
        /// <summary>
        /// Retrieves the current gamepad snapshot for the given player.
        /// </summary>
        /// <param name="indexPlayer">The player index.</param>
        /// <returns>The current gamepad snapshot.</returns>
        private GamePadSnapshot GetGamePadState(PlayerIndex indexPlayer) {
            GamePadSnapshot snapshot;
            if (_dictionaryCurrentGamePadStates.TryGetValue(indexPlayer, out snapshot)) {
                return snapshot;
            }
            return new GamePadSnapshot();
        }
        
        /// <summary>
        /// Retrieves the previous gamepad snapshot for the given player.
        /// </summary>
        /// <param name="indexPlayer">The player index.</param>
        /// <returns>The previous gamepad snapshot.</returns>
        private GamePadSnapshot GetPreviousGamePadState(PlayerIndex indexPlayer) {
            GamePadSnapshot snapshot;
            if (_dictionaryPreviousGamePadStates.TryGetValue(indexPlayer, out snapshot)) {
                return snapshot;
            }
            return new GamePadSnapshot();
        }
        
        /// <summary>
        /// Disposes the service and releases the focus source it was given.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            IDisposable disposableFocus = _sourceFocus as IDisposable;
            if (disposableFocus != null) {
                disposableFocus.Dispose();
            }
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// One frame of sampled gamepad state: the pressed physical buttons and the
        /// two thumbstick positions. Thumbstick directions are derived from the axes.
        /// </summary>
        private sealed class GamePadSnapshot {
            private readonly HashSet<JoyButton> _buttonsPressed = new HashSet<JoyButton>();
            
            /// <summary>Gets or sets the left thumbstick position.</summary>
            public Vector2 LeftThumbstick { get; set; }
            
            /// <summary>Gets or sets the right thumbstick position.</summary>
            public Vector2 RightThumbstick { get; set; }
            
            /// <summary>Returns whether the given physical button is held in this snapshot.</summary>
            /// <param name="buttonJoy">The physical button.</param>
            public bool IsButtonDown(JoyButton buttonJoy) {
                return _buttonsPressed.Contains(buttonJoy);
            }
            
            /// <summary>Records the given physical button as held in this snapshot.</summary>
            /// <param name="buttonJoy">The physical button.</param>
            public void AddButton(JoyButton buttonJoy) {
                _buttonsPressed.Add(buttonJoy);
            }
            
            /// <summary>Releases every button and recenters both thumbsticks.</summary>
            public void Clear() {
                _buttonsPressed.Clear();
                LeftThumbstick = new Vector2(0f, 0f);
                RightThumbstick = new Vector2(0f, 0f);
            }
            
            /// <summary>Adopts the state of another snapshot.</summary>
            /// <param name="snapshot">The snapshot to copy from.</param>
            public void CopyFrom(GamePadSnapshot snapshot) {
                _buttonsPressed.Clear();
                foreach (JoyButton buttonJoy in snapshot._buttonsPressed) {
                    _buttonsPressed.Add(buttonJoy);
                }
                LeftThumbstick = snapshot.LeftThumbstick;
                RightThumbstick = snapshot.RightThumbstick;
            }
        }
    }
}
