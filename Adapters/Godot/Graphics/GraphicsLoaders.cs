using System;
using System.IO;
using Godot;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Godot.Graphics {
    /// <summary>
    /// Provides the loader delegates that the graphics module registers against
    /// <see cref="IContentBackendService"/> for every graphics asset type it owns.
    /// </summary>
    public static class GraphicsLoaders {
        /// <summary>
        /// Creates a loader for the specified graphics asset type.
        /// Supported types: ITwoDimensionalTexture, IFont, ISpriteFont, IEffect,
        /// TextureAtlas and Tilemap.
        /// Textures produced here are owned by the content service, so the returned
        /// wrapper borrows the native texture instead of owning it.
        /// An effect asset may be a <see cref="ShaderMaterial"/> or a bare <see cref="Shader"/>,
        /// which is wrapped into a fresh material so its parameters can be set.
        /// </summary>
        /// <typeparam name="T">The asset contract to load.</typeparam>
        /// <param name="serviceContent">The content backend that supplies the raw asset.</param>
        /// <param name="nameAsset">The asset name relative to the content root.</param>
        /// <returns>The loaded asset.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceContent"/> is null.</exception>
        /// <exception cref="NotSupportedException">Thrown for an asset type this loader does not own.</exception>
        public static T Load<T>(IContentBackendService serviceContent, string nameAsset)
        where T : class, IAsset {
            if (serviceContent == null) {
                throw new ArgumentNullException(nameof(serviceContent));
            }
            if (typeof(T) == typeof(ITwoDimensionalTexture)) {
                return new TwoDimensionalTexture(serviceContent.LoadNative<Texture2D>(nameAsset), false) as T;
            }
            if (typeof(T) == typeof(IFont) || typeof(T) == typeof(ISpriteFont)) {
                return new SpriteFont(
                    serviceContent.LoadNative<Font>(nameAsset)
                ) as T;
            }
            if (typeof(T) == typeof(IEffect)) {
                return new Effect(
                    LoadMaterial(serviceContent, nameAsset)
                ) as T;
            }
            if (typeof(T) == typeof(TextureAtlas)) {
                using (Stream stream = serviceContent.OpenStream(nameAsset)) {
                    return TextureAtlas.FromStream(serviceContent, stream) as T;
                }
            }
            if (typeof(T) == typeof(Tilemap)) {
                using (Stream stream = serviceContent.OpenStream(nameAsset)) {
                    return Tilemap.FromStream(serviceContent, stream) as T;
                }
            }
            throw new NotSupportedException($"Graphics loader does not support asset type {typeof(T).FullName}.");
        }
        
        private static ShaderMaterial LoadMaterial(IContentBackendService serviceContent, string nameAsset) {
            Resource resource = serviceContent.LoadNative<Resource>(nameAsset);
            ShaderMaterial material = resource as ShaderMaterial;
            if (material != null) {
                return material;
            }
            Shader shader = resource as Shader;
            if (shader == null) {
                throw new ContentLoadException(
                    $"Asset '{nameAsset}' is of type {resource.GetType().FullName}, " +
                    $"neither {typeof(ShaderMaterial).FullName} nor {typeof(Shader).FullName}."
                );
            }
            material = new ShaderMaterial();
            material.Shader = shader;
            return material;
        }
    }
}
