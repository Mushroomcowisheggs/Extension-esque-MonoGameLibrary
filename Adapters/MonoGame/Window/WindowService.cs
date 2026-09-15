using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Extensions.Window;

namespace MonoGameLibrary.Adapters.MonoGame.Window {
    /// <summary>
    /// MonoGame implementation of <see cref="IWindowService"/>.
    /// Runtime window resizing goes through <see cref="GraphicsDeviceManager"/>, which is
    /// the only supported way to change the client area of a running MonoGame window.
    /// The minimum size is re-applied from the update pass, but only when the client size
    /// actually changed, so a window the platform refuses to grow back cannot turn into a
    /// per-frame device reset.
    /// </summary>
    internal sealed class WindowService : IWindowService, IDisposable {
        private readonly object _lock = new object();
        private readonly Game _game;
        private readonly GraphicsDeviceManager _managerGraphics;
        private int _widthClientMinimum;
        private int _heightClientMinimum;
        private int _widthClient;
        private int _heightClient;
        private int _widthPixel;
        private int _heightPixel;
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public event EventHandler<WindowBoundsChangedEventArgs> BoundsChanged;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowService"/> class.
        /// </summary>
        /// <param name="game">The running game that owns the window.</param>
        /// <param name="managerGraphics">The graphics device manager used to resize the client area.</param>
        /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
        public WindowService(Game game, GraphicsDeviceManager managerGraphics) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            if (managerGraphics == null) {
                throw new ArgumentNullException(nameof(managerGraphics));
            }
            _game = game;
            _managerGraphics = managerGraphics;
            _widthClient = game.Window.ClientBounds.Width;
            _heightClient = game.Window.ClientBounds.Height;
            _widthPixel = game.GraphicsDevice.PresentationParameters.BackBufferWidth;
            _heightPixel = game.GraphicsDevice.PresentationParameters.BackBufferHeight;
        }
        
        /// <inheritdoc />
        public int ClientWidth {
            get {
                return _game.Window.ClientBounds.Width;
            }
        }
        
        /// <inheritdoc />
        public int ClientHeight {
            get {
                return _game.Window.ClientBounds.Height;
            }
        }
        
        /// <inheritdoc />
        public int PixelWidth {
            get {
                return _game.GraphicsDevice.PresentationParameters.BackBufferWidth;
            }
        }
        
        /// <inheritdoc />
        public int PixelHeight {
            get {
                return _game.GraphicsDevice.PresentationParameters.BackBufferHeight;
            }
        }
        
        /// <inheritdoc />
        public int X {
            get {
                return _game.Window.Position.X;
            }
        }
        
        /// <inheritdoc />
        public int Y {
            get {
                return _game.Window.Position.Y;
            }
        }
        
        /// <inheritdoc />
        public int DisplayWidth {
            get {
                return _game.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            }
        }
        
        /// <inheritdoc />
        public int DisplayHeight {
            get {
                return _game.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
            }
        }
        
        /// <inheritdoc />
        public bool IsBorderless {
            get {
                return _game.Window.IsBorderless;
            }
        }
        
        /// <inheritdoc />
        public bool IsFocused {
            get {
                return _game.IsActive;
            }
        }
        
        /// <inheritdoc />
        public bool IsResizable {
            get {
                return _game.Window.AllowUserResizing;
            } set {
                _game.Window.AllowUserResizing = value;
            }
        }
        
        /// <inheritdoc />
        public bool IsMouseVisible {
            get {
                return _game.IsMouseVisible;
            } set {
                _game.IsMouseVisible = value;
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
            _managerGraphics.PreferredBackBufferWidth = widthTarget;
            _managerGraphics.PreferredBackBufferHeight = heightTarget;
            _managerGraphics.ApplyChanges();
        }
        
        /// <inheritdoc />
        public void SetPosition(int x, int y) {
            _game.Window.Position = new Microsoft.Xna.Framework.Point(x, y);
        }
        
        /// <inheritdoc />
        public void SetBorderless(bool flagBorderless) {
            _game.Window.IsBorderless = flagBorderless;
        }
        
        /// <summary>
        /// Restores the minimum size once per observed size change, records the current
        /// metrics, and raises <see cref="BoundsChanged"/> when they differ from the
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
            
            WindowBoundsChangedEventArgs argumentsEvent;
            lock (_lock) {
                if (widthClient == _widthClient && heightClient == _heightClient && widthPixel == _widthPixel && heightPixel == _heightPixel) {
                    return;
                }
                _widthClient = widthClient;
                _heightClient = heightClient;
                _widthPixel = widthPixel;
                _heightPixel = heightPixel;
                argumentsEvent = new WindowBoundsChangedEventArgs(widthClient, heightClient, widthPixel, heightPixel);
            }
            EventHandler<WindowBoundsChangedEventArgs> handler = BoundsChanged;
            if (handler == null) {
                return;
            }
            handler(this, argumentsEvent);
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
            BoundsChanged = null;
            GC.SuppressFinalize(this);
        }
    }
}