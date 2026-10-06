using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>Owns and drives the active scene.</summary>
    public sealed class SceneService : ISceneService {
        private readonly object _lock = new object();
        private Scene _sceneCurrent;
        private Scene _scenePending;
        private bool _flagDisposed;
        
        /// <summary>Initializes the service with no scene and no pending change.</summary>
        public SceneService() {
        }
        
        /// <summary>Gets the scene currently published, or null before the first change is applied.</summary>
        public Scene CurrentScene {
            get { lock (_lock) { return _sceneCurrent; } }
        }
        
        /// <summary>Requests a scene change. The new scene is loaded and initialised during the next <see cref="Update"/>; any previously pending scene is disposed immediately.</summary>
        /// <param name="scene">The scene to activate.</param>
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
        
        /// <summary>Prepares and publishes a pending scene, then updates the current one.</summary>
        /// <param name="timeFrame">The frame time to pass to the scene.</param>
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
                catch (Exception) {
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
        
        /// <summary>Draws the current scene when it is visible.</summary>
        /// <param name="timeFrame">The frame time to pass to the scene.</param>
        public void Draw(FrameTime timeFrame) {
            if (_flagDisposed) {
                return;
            }
            Scene sceneCurrent = CurrentScene;
            if (sceneCurrent != null && sceneCurrent.Visible) {
                sceneCurrent.Draw(timeFrame);
            }
        }
        
        /// <summary>Releases the current and any pending scene. Safe to call more than once.</summary>
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
