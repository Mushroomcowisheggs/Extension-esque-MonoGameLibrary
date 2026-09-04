using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    public static class GraphicsLoaders {
        /// <summary>
        /// Creates a loader for the specified graphics asset type.
        /// Supported types: ITwoDimensionalTexture, IFont, ISpriteFont, and IEffect.
        /// </summary>
        public static T Load<T>(IContentBackend content, string nameAsset)
        where T : class, IAsset {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            if (typeof(T) == typeof(ITwoDimensionalTexture)) {
                return new TwoDimensionalTexture(content.LoadNative<Texture2D>(nameAsset)) as T;
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
