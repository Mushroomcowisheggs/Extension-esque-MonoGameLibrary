using System;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;

namespace MonoGameLibrary.Adapters.Godot.Content {
    /// <summary>
    /// Registers Godot content services without coordinating any sibling
    /// adapter component.
    /// </summary>
    [ModuleRegistration(-500)]
    public sealed class ContentModule : IModule, IDisposable {
        /// <summary>The default content root used when none is supplied.</summary>
        public const string DirectoryRootDefault = "res://Content";
        
        private readonly string _directoryRoot;
        private ContentService _serviceContent;
        private bool _flagDisposed;
        
        /// <summary>Registers the module with the default content root.</summary>
        public ContentModule() : this(DirectoryRootDefault) {
        }
        
        /// <summary>Registers the module with an explicit content root.</summary>
        /// <param name="directoryRoot">The content root, e.g. <c>"res://Content"</c>.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="directoryRoot"/> is null or whitespace.</exception>
        public ContentModule(string directoryRoot) {
            if (string.IsNullOrWhiteSpace(directoryRoot)) {
                throw new ArgumentException("Root directory cannot be empty.", nameof(directoryRoot));
            }
            _directoryRoot = directoryRoot;
        }
        
        /// <summary>Registers the content service, its backend and its factory, then adds this module to the host.</summary>
        /// <param name="builder">The builder performing the composition fold.</param>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            _serviceContent = new ContentService(_directoryRoot);
            builder.RegisterService<IContentService>(_serviceContent);
            builder.RegisterService<IContentBackend>(_serviceContent);
            builder.RegisterService<IContentServiceFactory>(
                new ContentServiceFactory(_serviceContent)
            );
            builder.AddModule(this);
        }
        
        /// <summary>Releases the content service. Safe to call more than once.</summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            if (_serviceContent != null) {
                _serviceContent.Dispose();
                _serviceContent = null;
            }
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
