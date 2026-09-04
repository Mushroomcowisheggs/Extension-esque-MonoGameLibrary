using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>Owns and drives the active scene.</summary>
    public sealed class SceneService : ISceneService {
        private readonly object _lock = new object();
        private Scene _sceneCurrent;
        private Scene _scenePending;
        private bool _flagDisposed;
        
        public SceneService() {
        }
        
        public Scene CurrentScene {
            get { lock (_lock) { return _sceneCurrent; } }
        }
        
        public void ChangeScene(Scene scene) {
            if (scene == null) {
                throw new ArgumentNullException(nameof(scene));
            }
            lock (_lock) {
                ThrowIfDisposed();
                if (_scenePending != null) {
                    _scenePending.Dispose();
                }
                _scenePending = scene;
            }
        }
        
        public void Update(FrameTime timeFrame) {
            Scene sceneToActivate = null;
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                if (_scenePending != null) {
                    sceneToActivate = _scenePending;
                    _scenePending = null;
                }
            }
            
            if (sceneToActivate != null) {
                try {
                    sceneToActivate.LoadContent();
                    sceneToActivate.Initialize();
                }
                catch {
                    sceneToActivate.Dispose();
                    throw;
                }
                
                Scene scenePrevious;
                lock (_lock) {
                    scenePrevious = _sceneCurrent;
                    _sceneCurrent = sceneToActivate;
                }
                if (scenePrevious != null) {
                    scenePrevious.Dispose();
                }
            }
            
            Scene sceneCurrent = CurrentScene;
            if (sceneCurrent != null && sceneCurrent.Enabled) {
                sceneCurrent.Update(timeFrame);
            }
        }
        
        public void Draw(FrameTime timeFrame) {
            if (_flagDisposed) {
                return;
            }
            Scene sceneCurrent = CurrentScene;
            if (sceneCurrent != null && sceneCurrent.Visible) {
                sceneCurrent.Draw(timeFrame);
            }
        }
        
        public void Dispose() {
            Scene sceneCurrent;
            Scene scenePending;
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                sceneCurrent = _sceneCurrent;
                scenePending = _scenePending;
                _sceneCurrent = null;
                _scenePending = null;
            }
            if (sceneCurrent != null) {
                sceneCurrent.Dispose();
            }
            if (scenePending != null) {
                scenePending.Dispose();
            }
            GC.SuppressFinalize(this);
        }
        
        private void ThrowIfDisposed() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(SceneService));
            }
        }
    }
}
