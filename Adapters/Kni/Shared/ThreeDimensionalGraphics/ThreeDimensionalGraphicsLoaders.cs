using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Kni.ThreeDimensionalGraphics {
    /// <summary>
    /// Provides KNI loaders for three-dimensional content assets.
    /// </summary>
    public static class ThreeDimensionalGraphicsLoaders {
        /// <summary>
        /// Loads a supported three-dimensional asset.
        /// </summary>
        /// <typeparam name="T">The asset contract.</typeparam>
        /// <param name="serviceContent">The content backend.</param>
        /// <param name="nameAsset">The content asset name.</param>
        /// <returns>The loaded asset wrapper.</returns>
        public static T Load<T>(IContentBackendService serviceContent, string nameAsset)
        where T : class, IAsset {
            if (serviceContent == null) {
                throw new ArgumentNullException(nameof(serviceContent));
            }
            if (typeof(T) == typeof(IModel)) {
                Model model = serviceContent.LoadNative<Model>(nameAsset);
                return new AdapterModel(model) as T;
            }
            throw new NotSupportedException(
                $"Three-dimensional graphics loader does not support asset type {typeof(T).FullName}."
            );
        }
    }
}
