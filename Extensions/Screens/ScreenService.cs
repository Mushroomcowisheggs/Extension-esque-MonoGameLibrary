using System;
using System.Collections.Generic;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>Owns and drives a stack of game screens.</summary>
    /// <remarks>
    /// Thread safety: every member is guarded by a single lock, so screens may be pushed, popped
    /// and changed from any thread while <see cref="Update"/>/<see cref="Draw"/> run on another.
    /// Screen lifecycle callbacks (<c>Enter</c>, <c>Exit</c>, <c>Update</c>, <c>Draw</c>, disposal)
    /// run while that lock is held, so a callback must not block waiting on a thread that calls
    /// this service. Mutations requested during <see cref="Update"/> are deferred to the end of the
    /// call and applied in order, exactly as in earlier versions.
    /// <para>
    /// Cleanup is per screen. A screen that fails to unsubscribe, exit or dispose is reported to the
    /// configured <see cref="ILogger"/> and the remaining screens are still released, because a screen
    /// stack that stops at the first failure leaves the rest of its screens running after the service
    /// reports itself disposed.
    /// </para>
    /// </remarks>
    public sealed class ScreenService : IScreenService {
        private readonly object _lock = new object();
        private readonly List<Screen> _screens = new List<Screen>();
        private readonly Queue<Action> _queueOperation = new Queue<Action>();
        private readonly ILogger _logger;
        private bool _flagIsProcessing;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes the service with an empty screen stack.
        /// </summary>
        /// <param name="logger">Optional logger for a screen whose cleanup fails. </param>
        public ScreenService(Optional<ILogger> logger = default) {
            if (logger.HasValue) { _logger = logger.Value; } else { _logger = NullLogger.Instance; }
        }
        
        /// <summary>Gets the screen on top of the stack, or null when the stack is empty.</summary>
        public Screen CurrentScreen {
            get { lock (_lock) { return _screens.Count > 0 ? _screens[_screens.Count - 1] : null; } }
        }
        
        /// <summary>Pushes a screen, exiting the previous top. Deferred to the end of <see cref="Update"/> when called during a traversal.</summary>
        /// <param name="screen">The screen to push; it is loaded and initialised first.</param>
        public void Push(Screen screen) {
            if (screen == null) {
                throw new ArgumentNullException(nameof(screen));
            }
            lock (_lock) {
                ThrowIfDisposed();
                QueueOrExecute(delegate {
                    PrepareScreen(screen);
                    if (_screens.Count > 0) {
                        _screens[_screens.Count - 1].Exit();
                    }
                    _screens.Add(screen);
                    screen.Enter();
                });
            }
        }
        
        /// <summary>Removes and disposes the top screen, re-entering the one beneath. Does nothing when the stack is empty.</summary>
        public void Pop() {
            lock (_lock) {
                ThrowIfDisposed();
                QueueOrExecute(delegate {
                    if (_screens.Count > 0) {
                        Screen screenTop = _screens[_screens.Count - 1];
                        _screens.RemoveAt(_screens.Count - 1);
                        ReleaseScreen(screenTop);
                    }
                    if (_screens.Count > 0) {
                        _screens[_screens.Count - 1].Enter();
                    }
                });
            }
        }
        
        /// <summary>Replaces the whole stack with a single screen. Deferred to the end of <see cref="Update"/> when called during a traversal.</summary>
        /// <param name="screen">The screen to become the new stack.</param>
        public void Change(Screen screen) {
            if (screen == null) {
                throw new ArgumentNullException(nameof(screen));
            }
            lock (_lock) {
                ThrowIfDisposed();
                QueueOrExecute(delegate {
                    PrepareScreen(screen);
                    while (_screens.Count > 0) {
                        Screen screenTop = _screens[_screens.Count - 1];
                        _screens.RemoveAt(_screens.Count - 1);
                        ReleaseScreen(screenTop);
                    }
                    _screens.Add(screen);
                    screen.Enter();
                });
            }
        }
        
        /// <summary>Updates from the top of the stack downwards, stopping at the first blocking screen, then drains deferred mutations.</summary>
        /// <param name="timeFrame">The frame time to pass to each screen.</param>
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagIsProcessing = true;
                try {
                    for (int index = _screens.Count - 1; index >= 0; index -= 1) {
                        Screen screen = _screens[index];
                        if (screen.InputAction != null) {
                            screen.InputAction.Invoke(timeFrame);
                        }
                        screen.Update(timeFrame);
                        if (screen.IsBlocking) {
                            break;
                        }
                    }
                }
                finally {
                    _flagIsProcessing = false;
                    while (_queueOperation.Count > 0 && !_flagDisposed) {
                        _queueOperation.Dequeue().Invoke();
                    }
                }
            }
        }
        
        /// <summary>Draws from the lowest non-transparent screen upwards.</summary>
        /// <param name="timeFrame">The frame time to pass to each screen.</param>
        public void Draw(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed || _screens.Count == 0) {
                    return;
                }
                int indexFirst = 0;
                for (int index = _screens.Count - 1; index >= 0; index -= 1) {
                    if (!_screens[index].IsTransparent) {
                        indexFirst = index;
                        break;
                    }
                }
                for (int index = indexFirst; index < _screens.Count; index += 1) {
                    _screens[index].Draw(timeFrame);
                }
            }
        }
        
        /// <summary>Exits and disposes every screen, top first. Safe to call more than once.</summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                _queueOperation.Clear();
                while (_screens.Count > 0) {
                    Screen screen = _screens[_screens.Count - 1];
                    _screens.RemoveAt(_screens.Count - 1);
                    ReleaseScreen(screen);
                }
            }
            GC.SuppressFinalize(this);
        }
        
        private void PrepareScreen(Screen screen) {
            try {
                screen.LoadContent();
                screen.Initialize();
                SubscribeScreen(screen);
            }
            catch (Exception) {
                // The load failed, so the screen never joins the stack; its own partial state still has
                // to be released, and a failing release must not replace the failure that caused it.
                try {
                    screen.Dispose();
                } catch (Exception exception) {
                    ReportReleaseFailure("dispose", screen, exception);
                }
                throw;
            }
        }
        
        /// <summary>
        /// Releases one screen and reports a failing step without letting it stop the remaining steps or
        /// the remaining screens.
        /// </summary>
        /// <param name="screen">The screen to release; it has already left the stack. </param>
        private void ReleaseScreen(Screen screen) {
            try {
                UnsubscribeScreen(screen);
            } catch (Exception exception) {
                ReportReleaseFailure("unsubscribe", screen, exception);
            }
            try {
                screen.Exit();
            } catch (Exception exception) {
                ReportReleaseFailure("exit", screen, exception);
            }
            try {
                screen.Dispose();
            } catch (Exception exception) {
                ReportReleaseFailure("dispose", screen, exception);
            }
        }
        
        private void ReportReleaseFailure(string operation, Screen screen, Exception exception) {
            _logger.Error(
                $"Screen {operation} failed for {screen.GetType().Name}; the remaining screens are still released.", 
                exception
            );
        }
        
        private void QueueOrExecute(Action operation) {
            if (_flagIsProcessing) {
                _queueOperation.Enqueue(operation);
            }
            else {
                operation.Invoke();
            }
        }
        
        private void SubscribeScreen(Screen screen) {
            screen.ScreenChangeRequested += OnScreenChangeRequested;
        }
        
        private void UnsubscribeScreen(Screen screen) {
            screen.ScreenChangeRequested -= OnScreenChangeRequested;
        }
        
        private void OnScreenChangeRequested(object sender, ScreenChangeEventArguments arguments) {
            if (arguments.ChangeType == ScreenChangeType.Push) {
                Push(arguments.NewScreen);
            }
            else if (arguments.ChangeType == ScreenChangeType.Pop) {
                Pop();
            }
            else if (arguments.ChangeType == ScreenChangeType.Change) {
                Change(arguments.NewScreen);
            }
        }
        
        private void ThrowIfDisposed() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(ScreenService));
            }
        }
    }
}
