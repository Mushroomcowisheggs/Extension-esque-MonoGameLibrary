using System;
using GodotWindow = global::Godot.Window;

namespace MonoGameLibrary.Adapters.Godot.Input {
    /// <summary>
    /// Reads window focus from a running Godot window.
    /// The window has no focus property to poll cheaply, so the focus signals are
    /// observed and the last reported state is cached. The initial state is read
    /// once from the window so that a window created while focused is reported
    /// correctly before any signal has fired.
    /// </summary>
    public sealed class GameWindowFocusSource : IWindowFocusSource, IDisposable {
        private readonly object _lock = new object();
        private readonly GodotWindow _window;
        private bool _flagActive;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a source over the given window.
        /// </summary>
        /// <param name="window">The running game window. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="window"/> is null. </exception>
        public GameWindowFocusSource(GodotWindow window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }
            _window = window;
            _flagActive = _window.HasFocus();
            _window.FocusEntered += OnFocusEntered;
            _window.FocusExited += OnFocusExited;
        }
        
        /// <inheritdoc />
        public bool IsActive {
            get {
                lock (_lock) {
                    return _flagActive;
                }
            }
        }
        
        private void OnFocusEntered() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagActive = true;
            }
        }
        
        private void OnFocusExited() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagActive = false;
            }
        }
        
        /// <summary>
        /// Unsubscribes from the window focus signals.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            _window.FocusEntered -= OnFocusEntered;
            _window.FocusExited -= OnFocusExited;
            GC.SuppressFinalize(this);
        }
    }
}
