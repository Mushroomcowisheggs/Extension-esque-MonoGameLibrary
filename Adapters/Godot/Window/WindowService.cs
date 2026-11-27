using System;
using Godot;
using GodotWindow = global::Godot.Window;
using GodotInput = global::Godot.Input;
using MonoGameLibrary.Extensions.Window;

namespace MonoGameLibrary.Adapters.Godot.Window {
    /// <summary>
    /// Godot implementation of <see cref="IWindowService"/>.
    /// Runtime window resizing goes through the window's size property, which is
    /// the only supported way to change the client area of a running Godot window.
    /// The minimum size is re-applied from the update pass, but only when the client size
    /// actually changed, so a window the platform refuses to grow back cannot turn into a
    /// per-frame resize. Godot reports the window content size in a single coordinate
    /// space, so the client area and the render surface coincide here; both properties
    /// are kept because the contract names the two spaces separately.
    /// </summary>
    internal sealed class WindowService : IWindowService, IDisposable {
        private readonly object _lock = new object();
        private readonly GodotWindow _window;
        private int _widthClientMinimum;
        private int _heightClientMinimum;
        private int _widthClient;
        private int _heightClient;
        private int _widthPixel;
        private int _heightPixel;
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public event EventHandler<WindowBoundsChangedEventArgs> BoundsChangedHandler;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowService"/> class.
        /// </summary>
        /// <param name="window">The running game window.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="window"/> is null.</exception>
        public WindowService(GodotWindow window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }
            _window = window;
            _widthClient = _window.Size.X;
            _heightClient = _window.Size.Y;
            _widthPixel = _window.Size.X;
            _heightPixel = _window.Size.Y;
        }
        
        /// <inheritdoc />
        public int ClientWidth {
            get {
                return _window.Size.X;
            }
        }
        
        /// <inheritdoc />
        public int ClientHeight {
            get {
                return _window.Size.Y;
            }
        }
        
        /// <inheritdoc />
        public int PixelWidth {
            get {
                return _window.Size.X;
            }
        }
        
        /// <inheritdoc />
        public int PixelHeight {
            get {
                return _window.Size.Y;
            }
        }
        
        /// <inheritdoc />
        public int X {
            get {
                return _window.Position.X;
            }
        }
        
        /// <inheritdoc />
        public int Y {
            get {
                return _window.Position.Y;
            }
        }
        
        /// <inheritdoc />
        public int DisplayWidth {
            get {
                return DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen(_window.GetWindowId())).X;
            }
        }
        
        /// <inheritdoc />
        public int DisplayHeight {
            get {
                return DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen(_window.GetWindowId())).Y;
            }
        }
        
        /// <inheritdoc />
        public bool IsBorderless {
            get {
                return _window.Borderless;
            }
        }
        
        /// <inheritdoc />
        public bool IsFullScreen {
            get {
                return _window.Mode == GodotWindow.ModeEnum.Fullscreen
                    || _window.Mode == GodotWindow.ModeEnum.ExclusiveFullscreen;
            }
        }
        
        /// <inheritdoc />
        public bool IsFocused {
            get {
                return _window.HasFocus();
            }
        }
        
        /// <inheritdoc />
        public bool IsResizable {
            get {
                return !_window.Unresizable;
            } set {
                _window.Unresizable = !value;
            }
        }
        
        /// <inheritdoc />
        public bool IsMouseVisible {
            get {
                return GodotInput.MouseMode == GodotInput.MouseModeEnum.Visible
                    || GodotInput.MouseMode == GodotInput.MouseModeEnum.Confined;
            } set {
                GodotInput.MouseMode = value
                    ? GodotInput.MouseModeEnum.Visible
                    : GodotInput.MouseModeEnum.Hidden;
            }
        }
        
        /// <inheritdoc />
        public int MinimumClientWidth {
            get {
                lock (_lock) {
                    return _widthClientMinimum;
                }
            } set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock (_lock) {
                    _widthClientMinimum = value;
                }
            }
        }
        
        /// <inheritdoc />
        public int MinimumClientHeight {
            get {
                lock (_lock) {
                    return _heightClientMinimum;
                }
            } set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock (_lock) {
                    _heightClientMinimum = value;
                }
            }
        }
        
        /// <inheritdoc />
        public void SetClientSize(int width, int height) {
            if (width <= 0) {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (height <= 0) {
                throw new ArgumentOutOfRangeException(nameof(height));
            }
            int widthTarget = width;
            int heightTarget = height;
            lock (_lock) {
                if (_widthClientMinimum > widthTarget) {
                    widthTarget = _widthClientMinimum;
                }
                if (_heightClientMinimum > heightTarget) {
                    heightTarget = _heightClientMinimum;
                }
            }
            _window.Size = new Vector2I(widthTarget, heightTarget);
        }
        
        /// <inheritdoc />
        public void SetPosition(int x, int y) {
            _window.Position = new Vector2I(x, y);
        }
        
        /// <inheritdoc />
        public void SetBorderless(bool flagBorderless) {
            _window.Borderless = flagBorderless;
        }
        
        /// <inheritdoc />
        public void SetFullscreen(bool flagFullScreen) {
            _window.Mode = flagFullScreen
                ? GodotWindow.ModeEnum.Fullscreen
                : GodotWindow.ModeEnum.Windowed;
        }
        
        /// <summary>
        /// Restores the minimum size once per observed size change, records the current
        /// metrics, and raises <see cref="BoundsChangedHandler"/> when they differ from the
        /// previous frame. The event is raised outside the lock so that a handler may
        /// call back into this service.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        public void Update(MonoGameLibrary.Core.Time.FrameTime timeFrame) {
            if (_flagDisposed) {
                return;
            }
            int widthClient = ClientWidth;
            int heightClient = ClientHeight;
            int widthPixel = PixelWidth;
            int heightPixel = PixelHeight;
            bool flagChanged;
            int widthMinimum;
            int heightMinimum;
            lock (_lock) {
                flagChanged = widthClient != _widthClient || heightClient != _heightClient;
                widthMinimum = _widthClientMinimum;
                heightMinimum = _heightClientMinimum;
            }
            
            if (flagChanged && !IsBorderless) {
                bool flagTooSmall = widthClient < widthMinimum || heightClient < heightMinimum;
                if (flagTooSmall && widthClient > 0 && heightClient > 0) {
                    int widthTarget = widthClient > widthMinimum ? widthClient : widthMinimum;
                    int heightTarget = heightClient > heightMinimum ? heightClient : heightMinimum;
                    if (widthTarget > 0 && heightTarget > 0) {
                        SetClientSize(widthTarget, heightTarget);
                        widthClient = ClientWidth;
                        heightClient = ClientHeight;
                        widthPixel = PixelWidth;
                        heightPixel = PixelHeight;
                    }
                }
            }
            
            WindowBoundsChangedEventArgs argsWindowBoundsChangedEvent;
            lock (_lock) {
                if (widthClient == _widthClient && heightClient == _heightClient && widthPixel == _widthPixel && heightPixel == _heightPixel) {
                    return;
                }
                _widthClient = widthClient;
                _heightClient = heightClient;
                _widthPixel = widthPixel;
                _heightPixel = heightPixel;
                argsWindowBoundsChangedEvent = new WindowBoundsChangedEventArgs(widthClient, heightClient, widthPixel, heightPixel);
            }
            EventHandler<WindowBoundsChangedEventArgs> handler = BoundsChangedHandler;
            if (handler == null) {
                return;
            }
            handler(this, argsWindowBoundsChangedEvent);
        }
        
        /// <summary>
        /// Releases the service. The window itself is owned by the game and is not destroyed here.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            BoundsChangedHandler = null;
            GC.SuppressFinalize(this);
        }
    }
}
