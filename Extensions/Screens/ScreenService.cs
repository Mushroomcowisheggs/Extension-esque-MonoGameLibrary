using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>Owns and drives a stack of game screens.</summary>
    public sealed class ScreenService : IScreenService {
        private readonly List<Screen> _screens = new List<Screen>();
        private readonly Queue<Action> _queueOperation = new Queue<Action>();
        private bool _flagIsProcessing;
        private bool _flagDisposed;
        
        public ScreenService() {
        }
        
        public Screen CurrentScreen {
            get { return _screens.Count > 0 ? _screens[_screens.Count - 1] : null; }
        }
        
        public void Push(Screen screen) {
            if (screen == null) {
                throw new ArgumentNullException(nameof(screen));
            }
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
        
        public void Pop() {
            ThrowIfDisposed();
            QueueOrExecute(delegate {
                if (_screens.Count > 0) {
                    Screen top = _screens[_screens.Count - 1];
                    UnsubscribeScreen(top);
                    top.Exit();
                    _screens.RemoveAt(_screens.Count - 1);
                    top.Dispose();
                }
                if (_screens.Count > 0) {
                    _screens[_screens.Count - 1].Enter();
                }
            });
        }
        
        public void Change(Screen screen) {
            if (screen == null) {
                throw new ArgumentNullException(nameof(screen));
            }
            ThrowIfDisposed();
            QueueOrExecute(delegate {
                PrepareScreen(screen);
                while (_screens.Count > 0) {
                    Screen top = _screens[_screens.Count - 1];
                    UnsubscribeScreen(top);
                    top.Exit();
                    _screens.RemoveAt(_screens.Count - 1);
                    top.Dispose();
                }
                _screens.Add(screen);
                screen.Enter();
            });
        }
        
        public void Update(FrameTime timeFrame) {
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
        
        public void Draw(FrameTime timeFrame) {
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
        
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            _queueOperation.Clear();
            for (int index = _screens.Count - 1; index >= 0; index -= 1) {
                Screen screen = _screens[index];
                UnsubscribeScreen(screen);
                screen.Exit();
                screen.Dispose();
            }
            _screens.Clear();
            GC.SuppressFinalize(this);
        }
        
        private void PrepareScreen(Screen screen) {
            try {
                screen.LoadContent();
                screen.Initialize();
                SubscribeScreen(screen);
            }
            catch {
                screen.Dispose();
                throw;
            }
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
