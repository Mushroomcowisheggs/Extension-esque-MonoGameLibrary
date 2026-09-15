using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// MonoGame implementation of <see cref="IPointerInputService"/>.
    /// Reads the runtime pointer state and converts it into edge aware queries.
    /// While the application is not active every button is reported as released and
    /// the frame that regains focus only re-baselines the held buttons, so a button
    /// that was held across a focus change never produces a spurious press or release.
    /// </summary>
    public sealed class PointerInputService : IPointerInputService, IDisposable {
        private readonly object _lock = new object();
        private readonly Game _game;
        private MouseState _stateMousePrevious;
        private int _x;
        private int _y;
        private int _deltaScroll;
        private bool _flagLeftDown;
        private bool _flagRightDown;
        private bool _flagMiddleDown;
        private bool _flagLeftPressed;
        private bool _flagLeftReleased;
        private bool _flagRightPressed;
        private bool _flagRightReleased;
        private bool _flagMiddlePressed;
        private bool _flagMiddleReleased;
        private bool _flagSuppressEdges;
        private bool _flagFocused;
        private bool _flagDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointerInputService"/> class.
        /// </summary>
        /// <param name="game">The running game, used to observe window focus.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
        public PointerInputService(Game game) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            _game = game;
            _stateMousePrevious = Mouse.GetState();
        }

        /// <inheritdoc />
        public bool IsAvailable {
            get {
                // The desktop backend always has a pointer, so the only state that makes
                // the service unusable is its own disposal.
                lock (_lock) {
                    return !_flagDisposed;
                }
            }
        }

        /// <inheritdoc />
        public bool IsFocused {
            get {
                lock (_lock) {
                    return _flagFocused;
                }
            }
        }
        
        /// <inheritdoc />
        public int X {
            get {
                lock (_lock) {
                    return _x;
                }
            }
        }

        /// <inheritdoc />
        public int Y {
            get {
                lock (_lock) {
                    return _y;
                }
            }
        }

        /// <inheritdoc />
        public int ScrollDelta {
            get {
                lock (_lock) {
                    return _deltaScroll;
                }
            }
        }

        /// <inheritdoc />
        public bool IsLeftButtonDown {
            get {
                lock (_lock) {
                    return _flagLeftDown;
                }
            }
        }

        /// <inheritdoc />
        public bool IsRightButtonDown {
            get {
                lock (_lock) {
                    return _flagRightDown;
                }
            }
        }

        /// <inheritdoc />
        public bool IsMiddleButtonDown {
            get {
                lock (_lock) {
                    return _flagMiddleDown;
                }
            }
        }

        /// <inheritdoc />
        public bool WasLeftButtonJustPressed {
            get {
                lock (_lock) {
                    return _flagLeftPressed;
                }
            }
        }

        /// <inheritdoc />
        public bool WasLeftButtonJustReleased {
            get {
                lock (_lock) {
                    return _flagLeftReleased;
                }
            }
        }

        /// <inheritdoc />
        public bool WasRightButtonJustPressed {
            get {
                lock (_lock) {
                    return _flagRightPressed;
                }
            }
        }

        /// <inheritdoc />
        public bool WasRightButtonJustReleased {
            get {
                lock (_lock) {
                    return _flagRightReleased;
                }
            }
        }

        /// <inheritdoc />
        public bool WasMiddleButtonJustPressed {
            get {
                lock (_lock) {
                    return _flagMiddlePressed;
                }
            }
        }

        /// <inheritdoc />
        public bool WasMiddleButtonJustReleased {
            get {
                lock (_lock) {
                    return _flagMiddleReleased;
                }
            }
        }

        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                MouseState stateCurrent = Mouse.GetState();
                _x = stateCurrent.X;
                _y = stateCurrent.Y;
                _flagFocused = _game.IsActive;
                if (!_flagFocused) {
                    // Freeze every button as released so that a press that happened
                    // before focus was lost cannot be observed as still held.
                    _flagLeftDown = false;
                    _flagRightDown = false;
                    _flagMiddleDown = false;
                    ClearEdges();
                    _deltaScroll = 0;
                    _flagSuppressEdges = true;
                    _stateMousePrevious = stateCurrent;
                    return;
                }
                _flagLeftDown = IsPressed(stateCurrent.LeftButton);
                _flagRightDown = IsPressed(stateCurrent.RightButton);
                _flagMiddleDown = IsPressed(stateCurrent.MiddleButton);
                if (_flagSuppressEdges) {
                    // First frame after focus returned: adopt the current physical state
                    // without emitting edges, and restart wheel accounting from here.
                    _flagSuppressEdges = false;
                    ClearEdges();
                    _deltaScroll = 0;
                    _stateMousePrevious = stateCurrent;
                    return;
                }
                _flagLeftPressed = _flagLeftDown && !IsPressed(_stateMousePrevious.LeftButton);
                _flagLeftReleased = !_flagLeftDown && IsPressed(_stateMousePrevious.LeftButton);
                _flagRightPressed = _flagRightDown && !IsPressed(_stateMousePrevious.RightButton);
                _flagRightReleased = !_flagRightDown && IsPressed(_stateMousePrevious.RightButton);
                _flagMiddlePressed = _flagMiddleDown && !IsPressed(_stateMousePrevious.MiddleButton);
                _flagMiddleReleased = !_flagMiddleDown && IsPressed(_stateMousePrevious.MiddleButton);
                _deltaScroll = stateCurrent.ScrollWheelValue - _stateMousePrevious.ScrollWheelValue;
                _stateMousePrevious = stateCurrent;
            }
        }

        private void ClearEdges() {
            _flagLeftPressed = false;
            _flagLeftReleased = false;
            _flagRightPressed = false;
            _flagRightReleased = false;
            _flagMiddlePressed = false;
            _flagMiddleReleased = false;
        }

        private static bool IsPressed(ButtonState stateButton) {
            return stateButton == ButtonState.Pressed;
        }

        /// <summary>
        /// Releases the service. The pointer backend holds no resource owned by this instance.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}