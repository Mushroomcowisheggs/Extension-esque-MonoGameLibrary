using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// Provides the loader delegates that the graphics module registers against
    /// <see cref="IContentBackend"/> for every graphics asset type it owns.
    /// </summary>
    public static class GraphicsLoaders {
        /// <summary>
        /// Creates a loader for the specified graphics asset type.
        /// Supported types: ITwoDimensionalTexture, IFont, ISpriteFont, IEffect,
        /// TextureAtlas and Tilemap.
        /// Textures produced here are owned by the content manager, so the returned
        /// wrapper borrows the native texture instead of owning it.
        /// </summary>
        /// <typeparam name="T">The asset contract to load.</typeparam>
        /// <param name="content">The content backend that supplies the raw asset.</param>
        /// <param name="nameAsset">The asset name relative to the content root.</param>
        /// <returns>The loaded asset.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> is null.</exception>
        /// <exception cref="NotSupportedException">Thrown for an asset type this loader does not own.</exception>
        public static T Load<T>(IContentBackend content, string nameAsset)
        where T : class, IAsset {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (typeof(T) == typeof(ITwoDimensionalTexture)) {
                return new TwoDimensionalTexture(content.LoadNative<Texture2D>(nameAsset), false) as T;
            }
            if (typeof(T) == typeof(IFont) || typeof(T) == typeof(ISpriteFont)) {
                return new SpriteFont(
                    content.LoadNative<Microsoft.Xna.Framework.Graphics.SpriteFont>(nameAsset)
                ) as T;
            }
            if (typeof(T) == typeof(IEffect)) {
                return new Effect(
                    content.LoadNative<Microsoft.Xna.Framework.Graphics.Effect>(nameAsset)
                ) as T;
            }
            if (typeof(T) == typeof(TextureAtlas)) {
                using (Stream stream = content.OpenStream(nameAsset)) {
                    return TextureAtlas.FromStream(content, stream) as T;
                }
            }
            if (typeof(T) == typeof(Tilemap)) {
                using (Stream stream = content.OpenStream(nameAsset)) {
                    return Tilemap.FromStream(content, stream) as T;
                }
            }
            throw new NotSupportedException($"Graphics loader does not support asset type {typeof(T).FullName}.");
        }
    }
}