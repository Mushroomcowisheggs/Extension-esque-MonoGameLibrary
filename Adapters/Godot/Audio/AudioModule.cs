using System;
using Godot;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Platform-specific module that registers the Godot audio services.
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
        /// Gets or sets whether the module updates.
        /// </summary>
        public bool Enabled {
            get { lock (_lock) { return _flagEnabled; } }
            set { lock (_lock) { _flagEnabled = value; } }
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
            
            IContentBackend backendContent = builder.GetService<IContentBackend>();
            if (backendContent == null) {
                throw new InvalidOperationException("Content backend service is not registered.");
            }
            backendContent.RegisterScoped<IClipAudio>(AudioLoaders.Load<IClipAudio>);
            backendContent.RegisterScoped<ITrackAudio>(AudioLoaders.Load<ITrackAudio>);
            
            // The Godot anchor node the hosting seam registered; every playback node must
            // be parented under a scene tree node to make a sound.
            Node nodeParent = builder.GetService<Node>();
            if (nodeParent == null) {
                throw new InvalidOperationException("Godot anchor node service is not registered.");
            }
            
            _serviceAudio = new AudioService(nodeParent);
            _factoryPcm = new PcmAudioOutputFactory(nodeParent);
            builder.RegisterService<IAudioService>(_serviceAudio);
            builder.RegisterService<IPcmAudioOutputFactory>(_factoryPcm);
            builder.AddModule(this, new ScheduleOptions(orderUpdate: 0));
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
