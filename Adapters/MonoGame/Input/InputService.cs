using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// MonoGame implementation of <see cref="IInputService"/>. 
    /// </summary>
    public sealed class InputService : IInputService, IDisposable {
        private readonly object _lock = new object();
        private readonly Dictionary<Microsoft.Xna.Framework.PlayerIndex, GamePadState> _dictionaryCurrentGamePadStates;
        private readonly Dictionary<Microsoft.Xna.Framework.PlayerIndex, GamePadState> _dictionaryPreviousGamePadStates;
        private int _countFrame;
        private bool _flagDisposed = false;
        private readonly Game _game;
        private bool _flagFocused;
        private KeyboardState _stateKeyboardCurrent;
        private KeyboardState _stateKeyboardPrevious;
        private Vector2 _directionLeftThumbstickPrevious;
        private Vector2 _directionLeftThumbstickCurrent;
        private Vector2 _directionRightThumbstickPrevious;
        private Vector2 _directionRightThumbstickCurrent;
        private const float DEAD_ZONE = 0.24f;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InputService"/> class.
        /// </summary>
        public InputService() : this(null) {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InputService"/> class that observes
        /// window focus so that a key held across a focus change cannot stay latched.
        /// </summary>
        /// <param name="game">The running game, or null to keep every key accepted.</param>
        public InputService(Game game) {
            _game = game;
            _dictionaryCurrentGamePadStates = new Dictionary<Microsoft.Xna.Framework.PlayerIndex, GamePadState>();
            _dictionaryPreviousGamePadStates = new Dictionary<Microsoft.Xna.Framework.PlayerIndex, GamePadState>();
            
            // Pre-populate with initial states for all players
            foreach (Microsoft.Xna.Framework.PlayerIndex indexPlayer in Enum.GetValues(typeof(Microsoft.Xna.Framework.PlayerIndex))) {
                GamePadState state = GamePad.GetState(indexPlayer);
                _dictionaryCurrentGamePadStates[indexPlayer] = state;
                _dictionaryPreviousGamePadStates[indexPlayer] = state;
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
            bool flagFocused = true;
            if (_game != null) {
                flagFocused = _game.IsActive;
            }
            if (!flagFocused || !_flagFocused) {
                // Losing focus must not leave a key latched down, and the frame that regains
                // focus only re-baselines, so neither a press nor a release edge is reported.
                _stateKeyboardPrevious = default;
                _stateKeyboardCurrent = default;
                if (flagFocused) {
                    _stateKeyboardCurrent = Keyboard.GetState();
                    _stateKeyboardPrevious = _stateKeyboardCurrent;
                }
            } else {
                _stateKeyboardPrevious = _stateKeyboardCurrent;
                _stateKeyboardCurrent = Keyboard.GetState();
            }
            _flagFocused = flagFocused;
            _directionLeftThumbstickPrevious = _directionLeftThumbstickCurrent;
            _directionRightThumbstickPrevious = _directionRightThumbstickCurrent;
            _directionLeftThumbstickCurrent = GetLeftThumbstick(Microsoft.Xna.Framework.PlayerIndex.One);
            _directionRightThumbstickCurrent = GetRightThumbstick(Microsoft.Xna.Framework.PlayerIndex.One);
            
            foreach (Microsoft.Xna.Framework.PlayerIndex indexPlayer in Enum.GetValues(typeof(Microsoft.Xna.Framework.PlayerIndex))) {
                _dictionaryPreviousGamePadStates[indexPlayer] = _dictionaryCurrentGamePadStates[indexPlayer];
                _dictionaryCurrentGamePadStates[indexPlayer] = GamePad.GetState(indexPlayer);
            }
            
            lock (_lock) {
                _countFrame += 1;
            }
        }
        
        /// <inheritdoc />
        public bool IsKeyDown(KeyCode codeKey) {
            Keys key = KeyCodeConverter.ToMonoGameKey(codeKey);
            return _stateKeyboardCurrent.IsKeyDown(key);
        }
        
        /// <inheritdoc />
        public bool IsKeyUp(KeyCode codeKey) {
            Keys key = KeyCodeConverter.ToMonoGameKey(codeKey);
            return _stateKeyboardCurrent.IsKeyUp(key);
        }
        
        /// <inheritdoc />
        public bool WasKeyJustPressed(KeyCode codeKey) {
            Keys key = KeyCodeConverter.ToMonoGameKey(codeKey);
            return _stateKeyboardCurrent.IsKeyDown(key) && _stateKeyboardPrevious.IsKeyUp(key);
        }
        
        /// <inheritdoc />
        public bool WasKeyJustReleased(KeyCode codeKey) {
            Keys key = KeyCodeConverter.ToMonoGameKey(codeKey);
            return _stateKeyboardCurrent.IsKeyUp(key) && _stateKeyboardPrevious.IsKeyDown(key);
        }
        
        /// <summary>
        /// Converts a platform-independent <see cref="GamePadButton"/> to a MonoGame <see cref="Buttons"/>. 
        /// </summary>
        /// <param name="button">The platform-independent button. </param>
        /// <returns>The corresponding MonoGame button. </returns>
        private static Buttons ConvertButton(GamePadButton button) {
            switch (button) {
                case GamePadButton.A: return Buttons.A;
                case GamePadButton.B: return Buttons.B;
                case GamePadButton.X: return Buttons.X;
                case GamePadButton.Y: return Buttons.Y;
                case GamePadButton.Start: return Buttons.Start;
                case GamePadButton.Back: return Buttons.Back;
                case GamePadButton.LeftStick: return Buttons.LeftStick;
                case GamePadButton.RightStick: return Buttons.RightStick;
                case GamePadButton.LeftShoulder: return Buttons.LeftShoulder;
                case GamePadButton.RightShoulder: return Buttons.RightShoulder;
                case GamePadButton.DPadUp: return Buttons.DPadUp;
                case GamePadButton.DPadDown: return Buttons.DPadDown;
                case GamePadButton.DPadLeft: return Buttons.DPadLeft;
                case GamePadButton.DPadRight: return Buttons.DPadRight;
                default: return Buttons.A;
            }
        }
        
        /// <summary>
        /// Converts a platform-independent <see cref="PlayerIndex"/> to a MonoGame <see cref="Microsoft.Xna.Framework.PlayerIndex"/>.
        /// </summary>
        /// <param name="indexPlayer">The platform-independent player index.</param>
        /// <returns>The corresponding MonoGame player index.</returns>
        private static Microsoft.Xna.Framework.PlayerIndex ConvertPlayerIndex(MonoGameLibrary.Extensions.Input.PlayerIndex indexPlayer) {
            switch (indexPlayer) {
                case MonoGameLibrary.Extensions.Input.PlayerIndex.One: return Microsoft.Xna.Framework.PlayerIndex.One;
                case MonoGameLibrary.Extensions.Input.PlayerIndex.Two: return Microsoft.Xna.Framework.PlayerIndex.Two;
                case MonoGameLibrary.Extensions.Input.PlayerIndex.Three: return Microsoft.Xna.Framework.PlayerIndex.Three;
                case MonoGameLibrary.Extensions.Input.PlayerIndex.Four: return Microsoft.Xna.Framework.PlayerIndex.Four;
                default: return Microsoft.Xna.Framework.PlayerIndex.One;
            }
        }
        
        /// <inheritdoc />
        public bool IsButtonDown(MonoGameLibrary.Extensions.Input.PlayerIndex indexPlayer, GamePadButton button) {
            Buttons buttonMono = ConvertButton(button);
            Microsoft.Xna.Framework.PlayerIndex indexMono = ConvertPlayerIndex(indexPlayer);
            GamePadState state = GetGamePadState(indexMono);
            return state.IsButtonDown(buttonMono);
        }
        
        /// <inheritdoc />
        public bool WasButtonJustPressed(MonoGameLibrary.Extensions.Input.PlayerIndex indexPlayer, GamePadButton button) {
            Buttons buttonMono = ConvertButton(button);
            Microsoft.Xna.Framework.PlayerIndex indexMono = ConvertPlayerIndex(indexPlayer);
            GamePadState stateCurrent = GetGamePadState(indexMono);
            
            GamePadState statePrevious;
            if (!_dictionaryPreviousGamePadStates.TryGetValue(indexMono, out statePrevious)) {
                statePrevious = stateCurrent;
            }
            
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
                return stateCurrent.IsButtonDown(buttonMono) && statePrevious.IsButtonUp(buttonMono);
                
                case GamePadButton.LeftThumbstickUp:
                return WasThumbstickDirectionJustPressed(_directionLeftThumbstickPrevious, _directionLeftThumbstickCurrent, DEAD_ZONE, AxisDirection.Up);
                case GamePadButton.LeftThumbstickDown:
                return WasThumbstickDirectionJustPressed(_directionLeftThumbstickPrevious, _directionLeftThumbstickCurrent, DEAD_ZONE, AxisDirection.Down);
                case GamePadButton.LeftThumbstickLeft:
                return WasThumbstickDirectionJustPressed(_directionLeftThumbstickPrevious, _directionLeftThumbstickCurrent, DEAD_ZONE, AxisDirection.Left);
                case GamePadButton.LeftThumbstickRight:
                return WasThumbstickDirectionJustPressed(_directionLeftThumbstickPrevious, _directionLeftThumbstickCurrent, DEAD_ZONE, AxisDirection.Right);
                
                case GamePadButton.RightThumbstickUp:
                return WasThumbstickDirectionJustPressed(_directionRightThumbstickPrevious, _directionRightThumbstickCurrent, DEAD_ZONE, AxisDirection.Up);
                case GamePadButton.RightThumbstickDown:
                return WasThumbstickDirectionJustPressed(_directionRightThumbstickPrevious, _directionRightThumbstickCurrent, DEAD_ZONE, AxisDirection.Down);
                case GamePadButton.RightThumbstickLeft:
                return WasThumbstickDirectionJustPressed(_directionRightThumbstickPrevious, _directionRightThumbstickCurrent, DEAD_ZONE, AxisDirection.Left);
                case GamePadButton.RightThumbstickRight:
                return WasThumbstickDirectionJustPressed(_directionRightThumbstickPrevious, _directionRightThumbstickCurrent, DEAD_ZONE, AxisDirection.Right);
                
                default:
                return false;
            }
        }
        
        /// <inheritdoc />
        public bool WasButtonJustReleased(MonoGameLibrary.Extensions.Input.PlayerIndex indexPlayer, GamePadButton button) {
            Buttons buttonMono = ConvertButton(button);
            Microsoft.Xna.Framework.PlayerIndex indexMono = ConvertPlayerIndex(indexPlayer);
            GamePadState stateCurrent = GetGamePadState(indexMono);
            
            GamePadState statePrevious;
            if (!_dictionaryPreviousGamePadStates.TryGetValue(indexMono, out statePrevious)) {
                statePrevious = stateCurrent;
            }
            
            return stateCurrent.IsButtonUp(buttonMono) && statePrevious.IsButtonDown(buttonMono);
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
        
        private Vector2 GetLeftThumbstick(Microsoft.Xna.Framework.PlayerIndex indexPlayer) {
            return GetGamePadState(indexPlayer).ThumbSticks.Left;
        }
        
        private Vector2 GetRightThumbstick(Microsoft.Xna.Framework.PlayerIndex indexPlayer) {
            return GetGamePadState(indexPlayer).ThumbSticks.Right;
        }
        
        /// <summary>
        /// Defines cardinal directions for thumbstick axis detection.
        /// </summary>
        private enum AxisDirection { Up, Down, Left, Right }
        
        /// <summary>
        /// Retrieves the current gamepad state for the given MonoGame player index.
        /// </summary>
        /// <param name="indexPlayer">The MonoGame player index.</param>
        /// <returns>The current gamepad state.</returns>
        private GamePadState GetGamePadState(Microsoft.Xna.Framework.PlayerIndex indexPlayer) {
            if (_dictionaryCurrentGamePadStates.TryGetValue(indexPlayer, out var state)) {
                return state;
            }
            return GamePad.GetState(indexPlayer);
        }
        
        /// <summary>
        /// Disposes the service (no unmanaged resources).
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}