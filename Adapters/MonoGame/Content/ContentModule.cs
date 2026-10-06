using System;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;

namespace MonoGameLibrary.Adapters.MonoGame.Content {
    /// <summary>
    /// Registers MonoGame content services without coordinating any sibling
    /// adapter component.
    /// </summary>
    [ModuleRegistration(-500)]
    public sealed class ContentModule : IModule, IDisposable {
        private ContentService _serviceContent;
        private bool _flagDisposed;
        
        /// <summary>Registers the content service, its backend and its factory, then adds this module to the host. Requires a <c>ContentManager</c> to be registered already.</summary>
        /// <param name="builder">The builder performing the composition fold.</param>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            ContentManager managerContent = builder.GetService<ContentManager>();
            if (managerContent == null) {
                throw new InvalidOperationException("ContentManager service is not registered.");
            }
            
            _serviceContent = new ContentService(managerContent, false);
            builder.RegisterService<IContentService>(_serviceContent);
            builder.RegisterService<IContentBackend>(_serviceContent);
            builder.RegisterService<IContentServiceFactory>(
                new ContentServiceFactory(
                    _serviceContent,
                    managerContent.ServiceProvider,
                    managerContent.RootDirectory
                )
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
