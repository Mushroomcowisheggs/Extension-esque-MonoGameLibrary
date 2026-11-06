using System;
using System.Threading;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Registers and drives one event-sheet service.
    /// </summary>
    internal sealed class SheetModule : IModule, ILoadable, IUpdateable, IDisposable {
        private readonly SheetService _service;
        private readonly SheetOptions _options;
        private volatile bool _flagEnabled = true;
        private int _markDisposed;
        
        public SheetModule(
            ISheetStore store,
            SheetOptions options,
            ISheetVocabulary vocabulary,
            ILogger logger
        ) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (vocabulary == null) {
                throw new ArgumentNullException(nameof(vocabulary));
            }
            if (logger == null) {
                throw new ArgumentNullException(nameof(logger));
            }
            _options = options;
            _service = new SheetService(store, options, vocabulary, logger);
        }
        
        public bool Enabled {
            get { return _flagEnabled; }
            set { _flagEnabled = value; }
        }
        
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            builder.RegisterService<ISheetService>(_service);
            builder.AddModule(
                this,
                new ScheduleOptions(orderUpdate: _options.OrderUpdate)
            );
        }
        
        public void LoadContent() {
            if (_options.FlagAutoLoad) {
                _service.LoadConfiguredSheet();
            }
        }
        
        public void Update(FrameTime timeFrame) {
            if (_flagEnabled) {
                _service.Update(timeFrame);
            }
        }
        
        public void Dispose() {
            if (Interlocked.Exchange(ref _markDisposed, 1) != 0) {
                return;
            }
            _service.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
