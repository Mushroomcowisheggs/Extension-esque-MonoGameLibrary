using System;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.Godot.Content {
    /// <summary>
    /// Godot implementation of <see cref="IContentServiceFactory"/>.
    /// Each call to <see cref="Create"/> returns a new <see cref="ContentService"/>
    /// sharing the same content root and loader set.
    /// </summary>
    public sealed class ContentServiceFactory : IContentServiceFactory {
        private readonly ContentService _serviceContentTemplate;
        
        /// <summary>
        /// Initializes a new factory instance.
        /// </summary>
        /// <param name="serviceTemplate">The service whose root and loaders are copied to each created service.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceTemplate"/> is null.</exception>
        public ContentServiceFactory(ContentService serviceTemplate) {
            if (serviceTemplate == null) {
                throw new ArgumentNullException(nameof(serviceTemplate));
            }
            _serviceContentTemplate = serviceTemplate;
        }
        
        /// <inheritdoc />
        public IContentService Create() {
            ContentService serviceContent = new ContentService(_serviceContentTemplate.DirectoryRoot);
            _serviceContentTemplate.CopyLoadersTo(serviceContent);
            return serviceContent;
        }
    }
}
