using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Adapters.MonoGame.Content {
    /// <summary>
    /// A MonoGame implementation of <see cref="IContentService"/>. 
    /// </summary>
    public sealed class ContentService : IContentBackend {
        private readonly ContentManager _managerContent;
        private readonly bool _flagOwnsContentManager;
        private readonly Dictionary<Type, object> _loaders;
        private bool _flagDisposed;
        
        /// <summary>
        /// Creates a content service backed by a MonoGame content manager. 
        /// </summary>
        /// <param name="managerContent">The MonoGame ContentManager to use for loading raw assets.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="managerContent"/> is null.</exception>
        public ContentService(ContentManager managerContent, bool flagOwnsContentManager = true) {
            if (managerContent == null) { throw new ArgumentNullException(nameof(managerContent)); }
            _managerContent = managerContent;
            _flagOwnsContentManager = flagOwnsContentManager;
            _loaders = new Dictionary<Type, object>();
        }
        
        /// <summary>
        /// Gets the underlying MonoGame ContentManager used by this service.
        /// </summary>
        public ContentManager ContentManager {
            get { return _managerContent; }
        }
        
        /// <inheritdoc />
        public void Register<T>(Func<string, T> loader) where T : class, IAsset {
            if (loader == null) { throw new ArgumentNullException(nameof(loader)); }
            RegisterScoped<T>(delegate(IContentBackend backend, string name) {
                return loader(name);
            });
        }
        
        /// <inheritdoc />
        public void RegisterScoped<T>(Func<IContentBackend, string, T> loader)
        where T : class, IAsset {
            if (loader == null) { throw new ArgumentNullException(nameof(loader)); }
            _loaders.Add(typeof(T), loader);
        }
        
        /// <inheritdoc />
        public T Load<T>(string nameAsset) where T : class, IAsset {
            if (nameAsset == null) { throw new ArgumentNullException(nameof(nameAsset)); }
            if (_loaders.TryGetValue(typeof(T), out var stored)) {
                if (stored is Func<IContentBackend, string, T> typedLoader) {
                    return typedLoader(this, nameAsset);
                }
            }
            throw new NotSupportedException(
                $"Asset type {typeof(T)} is not registered. " +
                "Call Register<T> before attempting to load assets of this type."
            );
        }
        
        /// <inheritdoc />
        public TNative LoadNative<TNative>(string nameAsset) where TNative : class {
            if (string.IsNullOrWhiteSpace(nameAsset)) {
                throw new ArgumentException("Asset name cannot be empty.", nameof(nameAsset));
            }
            try {
                return _managerContent.Load<TNative>(nameAsset);
            } catch (Exception exception) when (IsLoadFailure(exception)) {
                throw new MonoGameLibrary.Core.Content.ContentLoadException(
                    $"Asset '{nameAsset}' of type {typeof(TNative).FullName} could not be loaded.", exception
                );
            }
        }
        
        /// <summary>
        /// Recognizes the backend failures that mean "this asset could not be loaded".
        /// Programming errors such as an unregistered reader are intentionally not wrapped.
        /// </summary>
        private static bool IsLoadFailure(Exception exception) {
            if (exception is Microsoft.Xna.Framework.Content.ContentLoadException) { return true; }
            if (exception is FileNotFoundException) { return true; }
            if (exception is DirectoryNotFoundException) { return true; }
            return false;
        }
        
        /// <inheritdoc />
        public Stream OpenStream(string pathFile) {
            if (string.IsNullOrWhiteSpace(pathFile)) {
                throw new ArgumentException("File path cannot be empty.", nameof(pathFile));
            }
            string pathFull = Path.Combine(_managerContent.RootDirectory, pathFile);
            return TitleContainer.OpenStream(pathFull);
        }
        
        internal void CopyLoadersTo(ContentService target) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            foreach (KeyValuePair<Type, object> pair in _loaders) {
                target._loaders.Add(pair.Key, pair.Value);
            }
        }
        
        /// <inheritdoc />
        public void Unload() {
            _managerContent.Unload();
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            
            _flagDisposed = true;
            if (_flagOwnsContentManager) {
                try {
                    Unload();
                } finally {
                    _managerContent.Dispose();
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
