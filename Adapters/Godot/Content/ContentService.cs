using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using MonoGameLibrary.Core.Content;
using FileAccess = global::Godot.FileAccess;

namespace MonoGameLibrary.Adapters.Godot.Content {
    /// <summary>
    /// A Godot implementation of <see cref="IContentService"/> backed by
    /// <see cref="ResourceLoader"/>. Asset names resolve relative to the configured
    /// content root unless they already carry a <c>res://</c> or <c>user://</c> prefix; the
    /// name must carry the resource extension, because Godot resolves through its import
    /// pipeline rather than compiled assets.
    /// </summary>
    public sealed class ContentService : IContentBackend {
        private readonly string _directoryRoot;
        private readonly Dictionary<Type, object> _loaders;
        private readonly HashSet<Resource> _resourcesLoaded;
        private bool _flagDisposed;
        
        /// <summary>
        /// Creates a content service backed by Godot's resource loader.
        /// </summary>
        /// <param name="directoryRoot">The content root, e.g. <c>"res://Content"</c>.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="directoryRoot"/> is null or whitespace.</exception>
        public ContentService(string directoryRoot) {
            if (string.IsNullOrWhiteSpace(directoryRoot)) {
                throw new ArgumentException("Root directory cannot be empty.", nameof(directoryRoot));
            }
            _directoryRoot = directoryRoot;
            _loaders = new Dictionary<Type, object>();
            _resourcesLoaded = new HashSet<Resource>();
        }
        
        /// <summary>
        /// Gets the content root directory this service resolves relative asset names against.
        /// </summary>
        public string DirectoryRoot {
            get { return _directoryRoot; }
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
            string pathResolved = ResolvePath(nameAsset);
            Resource resource = ResourceLoader.Load(pathResolved);
            if (resource == null) {
                throw new ContentLoadException(
                    $"Asset '{pathResolved}' of type {typeof(TNative).FullName} could not be loaded."
                );
            }
            TNative native = resource as TNative;
            if (native == null) {
                throw new ContentLoadException(
                    $"Asset '{pathResolved}' is of type {resource.GetType().FullName}, " +
                    $"not {typeof(TNative).FullName}."
                );
            }
            _resourcesLoaded.Add(resource);
            return native;
        }
        
        /// <inheritdoc />
        public Stream OpenStream(string pathFile) {
            if (string.IsNullOrWhiteSpace(pathFile)) {
                throw new ArgumentException("File path cannot be empty.", nameof(pathFile));
            }
            string pathResolved = ResolvePath(pathFile);
            FileAccess file = FileAccess.Open(pathResolved, FileAccess.ModeFlags.Read);
            if (file == null) {
                throw new ContentLoadException($"File '{pathResolved}' could not be opened for reading.");
            }
            return new GodotFileStream(file);
        }
        
        /// <summary>
        /// Resolves an asset name to a Godot resource path. Names that already carry a
        /// <c>res://</c> or <c>user://</c> prefix are used as-is; every other name is
        /// resolved relative to the configured content root.
        /// </summary>
        /// <param name="nameAsset">The asset name to resolve.</param>
        /// <returns>The resolved Godot resource path.</returns>
        internal string ResolvePath(string nameAsset) {
            if (string.IsNullOrWhiteSpace(nameAsset)) {
                throw new ArgumentException("Asset name cannot be empty.", nameof(nameAsset));
            }
            if (nameAsset.StartsWith("res://", StringComparison.Ordinal) ||
                nameAsset.StartsWith("user://", StringComparison.Ordinal)) {
                return nameAsset;
            }
            string rootTrimmed = _directoryRoot.TrimEnd('/');
            string nameTrimmed = nameAsset.TrimStart('/');
            return rootTrimmed + "/" + nameTrimmed;
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
            // Godot resources are reference counted: clearing the service-side ledger drops
            // this service's references so the engine can reclaim unreferenced resources.
            // Resources still held by asset wrappers stay alive until those wrappers are
            // released; unloading here is a reference drop, not an imperative destroy.
            _resourcesLoaded.Clear();
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            Unload();
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
