using System;
using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>
    /// Gum implementation of <see cref="IUserInterfaceService"/>.
    /// </summary>
    /// <remarks>
    /// Lifetime: this service wraps the process-wide <c>MonoGameGum.GumService.Default</c> 
    /// singleton and does not own it. <see cref="Dispose"/> only retires the wrapper; the global 
    /// Gum service and its root hierarchy survive for the lifetime of the process, so at most one 
    /// host may use this service per process (a second host would share the same global root). 
    /// After <see cref="Dispose"/> every member throws <see cref="ObjectDisposedException"/>. 
    /// </remarks>
    public sealed class GumService : IUserInterfaceService, IDisposable {
        private readonly Game _game;
        private readonly DefaultVisualsVersion _version;
        private readonly GumBridgesService _serviceBridges;
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
            IGumTextureBridge<
                NineSliceRuntime,
                ColoredRectangleRuntime,
                TextRuntime,
                ITwoDimensionalTexture,
                TextureRegion,
                AnimationFrame
            > bridgeTexture,
            IGumInputBridge<KeyEventArgs, KeyCode> bridgeInput,
            IEnumerable<Keys> keysTabForward = null, 
            IEnumerable<Keys> keysTabReverse = null
        ) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            if (bridgeTexture == null) {
                throw new ArgumentNullException(nameof(bridgeTexture));
            }
            if (bridgeInput == null) {
                throw new ArgumentNullException(nameof(bridgeInput));
            }
            _game = game;
            _version = version;
            _serviceBridges = new GumBridgesService(bridgeTexture, bridgeInput);
            
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
        
        /// <summary>Gets the bridges this service resolved, which consumers use to reach Gum's runtime types.</summary>
        public GumBridgesService Bridges {
            get { return _serviceBridges; }
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
        
        /// <summary>Adds a key that moves focus forward through Gum's tab order.</summary>
        /// <param name="key">The key to add.</param>
        public void AddNavigationForwardKey(NavigationKey key) {
            FrameworkElement.TabKeyCombos.Add(
                new KeyCombo { PushedKey = ToMonoGameKey(key) }
            );
        }
        
        /// <summary>Adds a key that moves focus backward through Gum's tab order.</summary>
        /// <param name="key">The key to add.</param>
        public void AddNavigationReverseKey(NavigationKey key) {
            FrameworkElement.TabReverseKeyCombos.Add(
                new KeyCombo { PushedKey = ToMonoGameKey(key) }
            );
        }
        
        /// <summary>Removes a previously added forward-navigation key. Removing a key that was never added is a no-op.</summary>
        /// <param name="key">The key to remove.</param>
        public void RemoveNavigationForwardKey(NavigationKey key) {
            Keys keyMonoGame = ToMonoGameKey(key);
            FrameworkElement.TabKeyCombos.RemoveAll(delegate(KeyCombo combo) {
                return combo.PushedKey == keyMonoGame;
            });
        }
        
        /// <summary>Removes a previously added reverse-navigation key. Removing a key that was never added is a no-op.</summary>
        /// <param name="key">The key to remove.</param>
        public void RemoveNavigationReverseKey(NavigationKey key) {
            Keys keyMonoGame = ToMonoGameKey(key);
            FrameworkElement.TabReverseKeyCombos.RemoveAll(delegate(KeyCombo combo) {
                return combo.PushedKey == keyMonoGame;
            });
        }
        
        private static Keys ToMonoGameKey(NavigationKey key) {
            switch (key) {
                case NavigationKey.Tab: return Keys.Tab;
                case NavigationKey.Up: return Keys.Up;
                case NavigationKey.Down: return Keys.Down;
                case NavigationKey.Left: return Keys.Left;
                case NavigationKey.Right: return Keys.Right;
                case NavigationKey.Enter: return Keys.Enter;
                case NavigationKey.Escape: return Keys.Escape;
                case NavigationKey.Space: return Keys.Space;
                default: return Keys.None;
            }
        }
        
        private void EnsureInitialized() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(
                    nameof(GumService), 
                    "The GumService has been disposed. "
                );
            }
            if (!_flagInitialized) {
                throw new InvalidOperationException("GumService must be initialized before use.");
            }
        }
        
        /// <summary>
        /// Disposes the service wrapper (no unmanaged resources to release).
        /// The process-wide <c>MonoGameGum.GumService.Default</c> singleton is not disposed — it 
        /// is owned by Gum and shared across the process — only this wrapper retires. Subsequent 
        /// calls throw <see cref="ObjectDisposedException"/>. 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            // The global MonoGameGum.GumService.Default is process-wide state owned by Gum and is
            // deliberately left untouched; only the wrapper is retired here.
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
