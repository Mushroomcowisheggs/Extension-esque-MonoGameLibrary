using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.MonoGame.Audio {
    /// <summary>
    /// Platform-specific module that registers the MonoGame audio service. 
    /// Implements <see cref="IModule"/> for automatic discovery and <see cref="IUpdateable"/>
    /// to forward per-frame updates to the audio service. 
    /// </summary>
    [ModuleRegistration(-200)]
    public sealed class AudioModule : IModule, IUpdateable, IDisposable {
        private IAudioService _serviceAudio;
        private readonly object _lock = new object();
        private bool _flagEnabled = true;
        private bool _flagDisposed = false;
        
        /// <summary>
        /// Gets the update order. Default is 0.
        /// </summary>
        public int Order { get; } = 0;
        
        /// <summary>
        /// Gets or sets whether the module updates.
        /// </summary>
        public bool Enabled {
            get { lock (_lock) { return _flagEnabled; } }
            set { lock (_lock) _flagEnabled = value; }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioModule"/> class. 
        /// </summary>
        public AudioModule() {
        }
        
        /// <inheritdoc />
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            IContentBackend content = builder.GetService<IContentBackend>();
            if (content == null) {
                throw new InvalidOperationException("Content backend service is not registered.");
            }
            content.RegisterScoped<IClipAudio>(AudioLoaders.Load<IClipAudio>);
            content.RegisterScoped<ITrackAudio>(AudioLoaders.Load<ITrackAudio>);
            
            _serviceAudio = new AudioService();
            builder.RegisterService<IAudioService>(_serviceAudio);
            builder.AddModule(this);
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            bool flagShouldUpdate;
            lock (_lock) {
                flagShouldUpdate = _flagEnabled && !_flagDisposed && _serviceAudio != null;
            }
            if (!flagShouldUpdate) {
                return;
            }
            
            _serviceAudio.Update(timeFrame);
        }
        
        /// <summary>
        /// Disposes the module (no unmanaged resources). 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            IDisposable disposable = _serviceAudio as IDisposable;
            if (disposable != null) {
                disposable.Dispose();
            }
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
