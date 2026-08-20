using System;
using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum {
    /// <summary>
    /// Gum implementation of <see cref="IUserInterfaceService"/>.
    /// </summary>
    public sealed class GumService : IUserInterfaceService, IDisposable {
        private readonly Game _game;
        private readonly DefaultVisualsVersion _version;
        private readonly object _lock = new object();
        private bool _flagInitialized;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GumService"/> class. 
        /// </summary>
        /// <param name="game">The running MonoGame game instance. </param>
        /// <param name="version">The Gum visual version. </param>
        /// <param name="keysTabForward">Keys to navigate forward (default: Tab).</param>
        /// <param name="keysTabReverse">Keys to navigate backward (default: Shift+Tab).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null. </exception>
        public GumService(
            Game game, 
            DefaultVisualsVersion version, 
            IEnumerable<Keys> keysTabForward = null, 
            IEnumerable<Keys> keysTabReverse = null
        ) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            _game = game;
            _version = version;
            
            // Apply tab navigation keys if provided
            if (keysTabForward != null) {
                foreach (var key in keysTabForward) {
                    FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = key });
                }
            }
            if (keysTabReverse != null) {
                foreach (var key in keysTabReverse) {
                    FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = key });
                }
            }
        }
        
        /// <inheritdoc />
        public void Initialize() {
            lock (_lock) {
                if (_flagInitialized) {
                    return;
                }
                
                // Initialize the global GumService instance with the MonoGame host.
                global::MonoGameGum.GumService.Default.Initialize(_game, _version);
                
                _flagInitialized = true;
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            EnsureInitialized();
            GameTime timeGame = new GameTime(timeFrame.TotalTimeSpan, timeFrame.DeltaTimeSpan);
            global::MonoGameGum.GumService.Default.Update(timeGame);
        }
        
        /// <inheritdoc />
        public void Draw() {
            EnsureInitialized();
            global::MonoGameGum.GumService.Default.Draw();
        }
        
        /// <inheritdoc />
        public void ClearRoot() {
            EnsureInitialized();
            global::MonoGameGum.GumService.Default.Root.Children.Clear();
        }
        
        /// <inheritdoc />
        public void AddToRoot(object element) {
            if (element == null) {
                throw new ArgumentNullException(nameof(element));
            }
            EnsureInitialized();
            GraphicalUiElement gue = element as GraphicalUiElement;
            if (gue == null) {
                throw new ArgumentException("Element must be a GraphicalUiElement.", nameof(element));
            }
            global::MonoGameGum.GumService.Default.Root.Children.Add(gue);
        }
        
        /// <inheritdoc />
        public void SetCanvas(float width, float height, float zoom) {
            EnsureInitialized();
            global::MonoGameGum.GumService.Default.CanvasWidth = width;
            global::MonoGameGum.GumService.Default.CanvasHeight = height;
            global::MonoGameGum.GumService.Default.Renderer.Camera.Zoom = zoom;
        }
        
        /// <inheritdoc />
        public void ConfigureInput(bool flagEnableKeyboard = true, bool flagEnableGamepad = true) {
            EnsureInitialized();
            if (flagEnableKeyboard) {
                FrameworkElement.KeyboardsForUiControl.Add(global::MonoGameGum.GumService.Default.Keyboard);
            }
            if (flagEnableGamepad) {
                FrameworkElement.GamePadsForUiControl.AddRange(global::MonoGameGum.GumService.Default.Gamepads);
            }
        }
        
        private void EnsureInitialized() {
            if (!_flagInitialized) {
                throw new InvalidOperationException("GumService must be initialized before use.");
            }
        }
        
        /// <summary>
        /// Disposes the service (no unmanaged resources to release).
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            // If the global GumService supports IDisposable, dispose it here.
            // Otherwise, simply clear references.
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}