using System;
using Godot;
using GodotWindow = global::Godot.Window;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Godot.Input {
    /// <summary>
    /// Godot implementation of <see cref="IPointerInputService"/>.
    /// Reads the pointer state the window reports through its input signal and converts
    /// it into edge aware queries. Wheel notches accumulate as 120 units each, which is
    /// the delta convention of the reference backend. While the window is not active
    /// every button is reported as released and the frame that regains focus only
    /// re-baselines the held buttons, so a button that was held across a focus change
    /// never produces a spurious press or release.
    /// </summary>
    public sealed class PointerInputService : IPointerInputService, IDisposable {
        private readonly object _lock = new object();
        private readonly GodotWindow _window;
        private int _x;
        private int _y;
        private int _deltaScroll;
        private int _deltaScrollPending;
        private bool _flagLeftDown;
        private bool _flagRightDown;
        private bool _flagMiddleDown;
        private bool _flagLeftDownPrevious;
        private bool _flagRightDownPrevious;
        private bool _flagMiddleDownPrevious;
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
        /// <param name="window">The running game window whose input signal supplies pointer events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="window"/> is null.</exception>
        public PointerInputService(GodotWindow window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }
            _window = window;
            _window.WindowInput += OnWindowInput;
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
        
        private void OnWindowInput(InputEvent eventInput) {
            InputEventMouseMotion eventMotion = eventInput as InputEventMouseMotion;
            if (eventMotion != null) {
                lock (_lock) {
                    if (_flagDisposed) {
                        return;
                    }
                    _x = (int)eventMotion.Position.X;
                    _y = (int)eventMotion.Position.Y;
                }
                return;
            }
            InputEventMouseButton eventButton = eventInput as InputEventMouseButton;
            if (eventButton != null) {
                lock (_lock) {
                    if (_flagDisposed) {
                        return;
                    }
                    switch (eventButton.ButtonIndex) {
                        case MouseButton.Left:
                        _flagLeftDown = eventButton.Pressed;
                        break;
                        case MouseButton.Right:
                        _flagRightDown = eventButton.Pressed;
                        break;
                        case MouseButton.Middle:
                        _flagMiddleDown = eventButton.Pressed;
                        break;
                        case MouseButton.WheelUp:
                        if (eventButton.Pressed) {
                            _deltaScrollPending += (int)Math.Round(120f * eventButton.Factor);
                        }
                        break;
                        case MouseButton.WheelDown:
                        if (eventButton.Pressed) {
                            _deltaScrollPending -= (int)Math.Round(120f * eventButton.Factor);
                        }
                        break;
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagFocused = _window.HasFocus();
                if (!_flagFocused) {
                    // Freeze every button as released so that a press that happened
                    // before focus was lost cannot be observed as still held.
                    _flagLeftDown = false;
                    _flagRightDown = false;
                    _flagMiddleDown = false;
                    ClearEdges();
                    _deltaScroll = 0;
                    _deltaScrollPending = 0;
                    _flagSuppressEdges = true;
                    return;
                }
                if (_flagSuppressEdges) {
                    // First frame after focus returned: adopt the current physical state
                    // without emitting edges, and restart wheel accounting from here.
                    _flagSuppressEdges = false;
                    ClearEdges();
                    _deltaScroll = 0;
                    _deltaScrollPending = 0;
                    _flagLeftDownPrevious = _flagLeftDown;
                    _flagRightDownPrevious = _flagRightDown;
                    _flagMiddleDownPrevious = _flagMiddleDown;
                    return;
                }
                _flagLeftPressed = _flagLeftDown && !_flagLeftDownPrevious;
                _flagLeftReleased = !_flagLeftDown && _flagLeftDownPrevious;
                _flagRightPressed = _flagRightDown && !_flagRightDownPrevious;
                _flagRightReleased = !_flagRightDown && _flagRightDownPrevious;
                _flagMiddlePressed = _flagMiddleDown && !_flagMiddleDownPrevious;
                _flagMiddleReleased = !_flagMiddleDown && _flagMiddleDownPrevious;
                _deltaScroll = _deltaScrollPending;
                _deltaScrollPending = 0;
                _flagLeftDownPrevious = _flagLeftDown;
                _flagRightDownPrevious = _flagRightDown;
                _flagMiddleDownPrevious = _flagMiddleDown;
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
        
        /// <summary>
        /// Releases the service and unsubscribes from the window input signal.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            _window.WindowInput -= OnWindowInput;
            GC.SuppressFinalize(this);
        }
    }
}
