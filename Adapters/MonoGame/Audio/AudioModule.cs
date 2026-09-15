using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.MonoGame.Audio {
    /// <summary>
    /// Platform-specific module that registers the MonoGame audio services. 
    /// Implements <see cref="IModule"/> for automatic discovery and <see cref="IUpdateable"/>
    /// to forward per-frame updates to the clip service and to advance the starvation
    /// counters of every streaming PCM output. 
    /// </summary>
    [ModuleRegistration(-200)]
    public sealed class AudioModule : IModule, IUpdateable, IDisposable {
        private IAudioService _serviceAudio;
        private PcmAudioOutputFactory _factoryPcm;
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
            _factoryPcm = new PcmAudioOutputFactory();
            builder.RegisterService<IAudioService>(_serviceAudio);
            builder.RegisterService<IPcmAudioOutputFactory>(_factoryPcm);
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
            if (_factoryPcm != null) {
                _factoryPcm.UpdateUnderruns();
            }
        }
        
        /// <summary>
        /// Disposes the services it created, including any streaming output the game did not release. 
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            if (_factoryPcm != null) {
                _factoryPcm.Dispose();
                _factoryPcm = null;
            }
            IDisposable disposable = _serviceAudio as IDisposable;
            if (disposable != null) {
                disposable.Dispose();
            }
            _serviceAudio = null;
            GC.SuppressFinalize(this);
        }
    }
}