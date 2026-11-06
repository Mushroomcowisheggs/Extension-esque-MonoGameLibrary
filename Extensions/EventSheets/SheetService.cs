using System;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Loads, compiles, and updates one event-sheet document.
    /// </summary>
    internal sealed class SheetService : ISheetService, IDisposable {
        private readonly object _lock = new object();
        private readonly ISheetStore _store;
        private readonly SheetOptions _options;
        private readonly SheetDocumentLoader _loader;
        private readonly SheetCompiler _compiler;
        private readonly ISheetVocabulary _vocabulary;
        private readonly ILogger _logger;
        private bool _flagDisposed;
        private SheetDocument _document;
        private SheetRuntime _runtime;
        
        internal SheetService(
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
            options.Validate();
            _store = store;
            _options = options;
            _loader = new SheetDocumentLoader();
            _compiler = new SheetCompiler(vocabulary);
            _vocabulary = vocabulary;
            _logger = logger;
        }
        
        public bool IsLoaded {
            get {
                lock (_lock) {
                    return _runtime != null;
                }
            }
        }
        
        public string LoadedSheetName {
            get {
                lock (_lock) {
                    if (_document == null) {
                        return string.Empty;
                    }
                    return _document.Name;
                }
            }
        }
        
        public SheetDocument CurrentDocument {
            get {
                lock (_lock) {
                    return _document;
                }
            }
        }
        
        public SheetContext CurrentContext {
            get {
                lock (_lock) {
                    if (_runtime == null) {
                        return null;
                    }
                    return _runtime.Context;
                }
            }
        }
        
        public void LoadConfiguredSheet() {
            ThrowIfDisposed();
            string json = _store.Read(_options.NameSheet);
            SheetDocument document = _loader.Load(json);
            Load(document);
        }
        
        public void Load(SheetDocument document) {
            if (document == null) {
                throw new ArgumentNullException(nameof(document));
            }
            ThrowIfDisposed();
            
            SheetProgram program = _compiler.Compile(document);
            SheetObjects objectsStore = new SheetObjects();
            for (int i = 0; i < document.Objects.Count; i += 1) {
                objectsStore.Add(document.Objects[i].Clone());
            }
            SheetContext context = new SheetContext(
                objectsStore,
                document.Variables.Clone(),
                _logger,
                new Random()
            );
            SheetRuntime runtime = new SheetRuntime(program, context);
            
            lock (_lock) {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(nameof(SheetService));
                }
                _document = document;
                _runtime = runtime;
            }
        }
        
        public void Reload() {
            LoadConfiguredSheet();
        }
        
        public string RenderMarkdown() {
            SheetDocument document;
            lock (_lock) {
                document = _document;
            }
            if (document == null) {
                throw new InvalidOperationException("No event sheet is loaded.");
            }
            return SheetDocumentRenderer.RenderMarkdown(document, _vocabulary);
        }
        
        public string RenderHtml() {
            SheetDocument document;
            lock (_lock) {
                document = _document;
            }
            if (document == null) {
                throw new InvalidOperationException("No event sheet is loaded.");
            }
            return SheetDocumentRenderer.RenderHtml(document, _vocabulary);
        }
        
        internal void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_flagDisposed || _runtime == null) {
                    return;
                }
                _runtime.Update(timeFrame);
            }
        }
        
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                _runtime = null;
                _document = null;
            }
            GC.SuppressFinalize(this);
        }
        
        private void ThrowIfDisposed() {
            lock (_lock) {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(nameof(SheetService));
                }
            }
        }
    }
}
