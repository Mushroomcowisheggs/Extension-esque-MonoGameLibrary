using System;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.MonoGame.Content {
    /// <summary>
    /// MonoGame implementation of <see cref="IContentServiceFactory"/>. 
    /// Each call to <see cref="Create"/> returns a new <see cref="ContentService"/> 
    /// backed by a fresh <see cref="ContentManager"/>. 
    /// </summary>
    public sealed class ContentServiceFactory : IContentServiceFactory {
        private readonly ContentService _template;
        private readonly IServiceProvider _providerService;
        private readonly string _directoryRoot;
        
        /// <summary>
        /// Initializes a new factory instance. 
        /// </summary>
        /// <param name="serviceProvider">The service provider used by the ContentManager. </param>
        /// <param name="rootDirectory">The root directory for content (e.g., "Content"). </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is null. </exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="rootDirectory"/> is null or whitespace. </exception>
        public ContentServiceFactory(
            ContentService template,
            IServiceProvider providerService,
            string directoryRoot
        ) {
            if (template == null) {
                throw new ArgumentNullException(nameof(template));
            }
            if (providerService == null) {
                throw new ArgumentNullException(nameof(providerService));
            }
            if (string.IsNullOrWhiteSpace(directoryRoot)) {
                throw new ArgumentException("Root directory cannot be empty.", nameof(directoryRoot));
            }
            
            _template = template;
            _providerService = providerService;
            _directoryRoot = directoryRoot;
        }
        
        /// <inheritdoc />
        public IContentService Create() {
            var managerContent = new ContentManager(_providerService) {
                RootDirectory = _directoryRoot
            };
            ContentService serviceContent = new ContentService(managerContent);
            _template.CopyLoadersTo(serviceContent);
            return serviceContent;
        }
    }
}
