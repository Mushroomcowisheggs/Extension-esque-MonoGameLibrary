using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.MonoGame.ThreeDimensionalGraphics {
    /// <summary>
    /// Provides MonoGame loaders for three-dimensional content assets.
    /// </summary>
    public static class ThreeDimensionalGraphicsLoaders {
        /// <summary>
        /// Loads a supported three-dimensional asset.
        /// </summary>
        /// <typeparam name="T">The asset contract.</typeparam>
        /// <param name="backendContent">The content backend.</param>
        /// <param name="nameAsset">The content asset name.</param>
        /// <returns>The loaded asset wrapper.</returns>
        public static T Load<T>(IContentBackend backendContent, string nameAsset)
        where T : class, IAsset {
            if (backendContent == null) {
                throw new ArgumentNullException(nameof(backendContent));
            }
            if (typeof(T) == typeof(IModel)) {
                Model model = backendContent.LoadNative<Model>(nameAsset);
                return new ModelAdapter(model) as T;
            }
            throw new NotSupportedException(
                $"Three-dimensional graphics loader does not support asset type {typeof(T).FullName}."
            );
        }
    }
}
