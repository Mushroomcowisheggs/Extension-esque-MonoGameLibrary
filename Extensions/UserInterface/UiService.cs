using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Platform-neutral <see cref="IUserInterfaceService"/> over <see cref="UiElement"/>
    /// widgets. The service owns the root list, the canvas size and zoom, navigation
    /// between focusable elements (buttons), and draws the tree through
    /// <see cref="IUiRenderer"/>.
    /// </summary>
    /// <remarks>
    /// Navigation: the registered forward and reverse keys move the selection between
    /// focusable elements in tree order through <see cref="IUiNavigator"/>, and the
    /// confirm action activates the selected button. The engine-specific Gum
    /// implementation of this interface remains available for games that prefer it.
    /// </remarks>
    public sealed class UiService : IUserInterfaceService, IDisposable {
        private readonly IUiRenderer _renderer;
        private readonly IUiNavigator _navigatorUi;
        private readonly List<UiElement> _listRoots = new List<UiElement>();
        private readonly List<NavigationKey> _listKeysForward = new List<NavigationKey>();
        private readonly List<NavigationKey> _listKeysReverse = new List<NavigationKey>();
        private readonly object _lock = new object();
        private float _widthCanvas;
        private float _heightCanvas;
        private float _zoom = 1f;
        private bool _flagKeyboard = true;
        private bool _flagGamepad = true;
        private int _indexSelected;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes the service without input navigation (drawing only).
        /// </summary>
        /// <param name="renderer">The renderer the widgets draw through.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="renderer"/> is null.</exception>
        public UiService(IUiRenderer renderer) {
            if (renderer == null) {
                throw new ArgumentNullException(nameof(renderer));
            }
            _renderer = renderer;
            _navigatorUi = null;
        }
        
        /// <summary>
        /// Initializes the service with a navigation seam.
        /// </summary>
        /// <param name="renderer">The renderer the widgets draw through.</param>
        /// <param name="navigatorUi">The navigation seam polled for selection movement and confirmation.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public UiService(IUiRenderer renderer, IUiNavigator navigatorUi) {
            if (renderer == null) {
                throw new ArgumentNullException(nameof(renderer));
            }
            if (navigatorUi == null) {
                throw new ArgumentNullException(nameof(navigatorUi));
            }
            _renderer = renderer;
            _navigatorUi = navigatorUi;
        }
        
        /// <inheritdoc />
        public void Initialize() {
            // Nothing to set up: the renderer and navigation seam are constructor-injected.
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                List<UiElement> listFocusable = CollectFocusable();
                UpdateSelection(listFocusable);
                foreach (UiElement root in _listRoots) {
                    if (root.Visible) {
                        root.Update(timeFrame);
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public void Draw() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _renderer.Begin();
                try {
                    foreach (UiElement root in _listRoots) {
                        if (root.Visible) {
                            root.Draw(_renderer, _zoom);
                        }
                    }
                } finally {
                    _renderer.End();
                }
            }
        }
        
        /// <inheritdoc />
        public void ClearRoot() {
            lock (_lock) {
                _listRoots.Clear();
                _indexSelected = 0;
            }
        }
        
        /// <inheritdoc />
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="element"/> is not a <see cref="UiElement"/>.
        /// </exception>
        public void AddToRoot(object element) {
            UiElement elementUi = element as UiElement;
            if (elementUi == null) {
                throw new ArgumentException(
                    "The element must be a UiElement for this implementation.",
                    nameof(element)
                );
            }
            lock (_lock) {
                _listRoots.Add(elementUi);
            }
        }
        
        /// <inheritdoc />
        /// <returns>True when the element was found and removed.</returns>
        public bool RemoveFromRoot(object element) {
            UiElement elementUi = element as UiElement;
            if (elementUi == null) {
                return false;
            }
            lock (_lock) {
                return _listRoots.Remove(elementUi);
            }
        }
        
        /// <inheritdoc />
        public void SetCanvas(float width, float height, float zoom) {
            if (width <= 0f || height <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (zoom <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(zoom));
            }
            lock (_lock) {
                _widthCanvas = width;
                _heightCanvas = height;
                _zoom = zoom;
            }
        }
        
        /// <inheritdoc />
        public void ConfigureInput(bool flagEnableKeyboard = true, bool flagEnableGamepad = true) {
            lock (_lock) {
                _flagKeyboard = flagEnableKeyboard;
                _flagGamepad = flagEnableGamepad;
            }
        }
        
        /// <inheritdoc />
        public void AddNavigationForwardKey(NavigationKey key) {
            lock (_lock) {
                if (!_listKeysForward.Contains(key)) {
                    _listKeysForward.Add(key);
                }
            }
        }
        
        /// <inheritdoc />
        public void AddNavigationReverseKey(NavigationKey key) {
            lock (_lock) {
                if (!_listKeysReverse.Contains(key)) {
                    _listKeysReverse.Add(key);
                }
            }
        }
        
        /// <inheritdoc />
        public void RemoveNavigationForwardKey(NavigationKey key) {
            lock (_lock) {
                _listKeysForward.Remove(key);
            }
        }
        
        /// <inheritdoc />
        public void RemoveNavigationReverseKey(NavigationKey key) {
            lock (_lock) {
                _listKeysReverse.Remove(key);
            }
        }
        
        /// <summary>
        /// Releases the service and disposes the renderer when it is disposable. The
        /// elements are released by their owner.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                _listRoots.Clear();
            }
            IDisposable disposableRenderer = _renderer as IDisposable;
            if (disposableRenderer != null) {
                disposableRenderer.Dispose();
            }
            GC.SuppressFinalize(this);
        }
        
        private List<UiElement> CollectFocusable() {
            List<UiElement> listFocusable = new List<UiElement>();
            foreach (UiElement root in _listRoots) {
                CollectFocusable(root, listFocusable);
            }
            return listFocusable;
        }
        
        private static void CollectFocusable(UiElement element, List<UiElement> listFocusable) {
            if (!element.Visible || !element.Enabled) {
                return;
            }
            if (element is UiButton) {
                listFocusable.Add(element);
            }
            UiPanel panel = element as UiPanel;
            if (panel == null) {
                return;
            }
            foreach (UiElement child in panel.Children) {
                CollectFocusable(child, listFocusable);
            }
        }
        
        private void UpdateSelection(List<UiElement> listFocusable) {
            if (listFocusable.Count == 0) {
                foreach (UiElement root in _listRoots) {
                    root.FlagSelected = false;
                }
                _indexSelected = 0;
                return;
            }
            if (_indexSelected >= listFocusable.Count) {
                _indexSelected = 0;
            }
            int direction = ReadNavigationDirection();
            if (direction > 0) {
                _indexSelected = (_indexSelected + 1) % listFocusable.Count;
            } else if (direction < 0) {
                _indexSelected = (_indexSelected - 1 + listFocusable.Count) % listFocusable.Count;
            }
            for (int index = 0; index < listFocusable.Count; index += 1) {
                listFocusable[index].FlagSelected = index == _indexSelected;
            }
            if (ReadConfirmPressed()) {
                UiButton button = listFocusable[_indexSelected] as UiButton;
                if (button != null) {
                    button.Activate();
                }
            }
        }
        
        private int ReadNavigationDirection() {
            if (_navigatorUi == null) {
                return 0;
            }
            if (!_flagKeyboard && !_flagGamepad) {
                return 0;
            }
            int direction = 0;
            foreach (NavigationKey key in _listKeysForward) {
                if (_navigatorUi.IsPressed(key)) {
                    direction = 1;
                }
            }
            foreach (NavigationKey key in _listKeysReverse) {
                if (_navigatorUi.IsPressed(key)) {
                    direction = -1;
                }
            }
            return direction;
        }
        
        private bool ReadConfirmPressed() {
            if (_navigatorUi == null) {
                return false;
            }
            if (!_flagKeyboard && !_flagGamepad) {
                return false;
            }
            return _navigatorUi.IsConfirmPressed();
        }
    }
}
