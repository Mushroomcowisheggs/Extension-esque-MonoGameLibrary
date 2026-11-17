using System;
using Godot;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;
using GodotMesh = global::Godot.Mesh;

namespace MonoGameLibrary.Adapters.Godot.ThreeDimensionalGraphics {
    /// <summary>
    /// Provides Godot loaders for three-dimensional content assets.
    /// </summary>
    public static class ThreeDimensionalGraphicsLoaders {
        /// <summary>
        /// Loads a supported three-dimensional asset. A Godot model is either a
        /// <see cref="PackedScene"/> (a hierarchy of mesh instances) or a bare
        /// <see cref="GodotMesh"/> resource; both are wrapped into an adapter model.
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
                Resource resource = backendContent.LoadNative<Resource>(nameAsset);
                PackedScene scene = resource as PackedScene;
                if (scene != null) {
                    return new AdapterModel(scene) as T;
                }
                GodotMesh mesh = resource as GodotMesh;
                if (mesh != null) {
                    return new AdapterModel(mesh) as T;
                }
                throw new NotSupportedException(
                    $"Asset '{nameAsset}' is a {resource.GetType().FullName}, not a PackedScene or Mesh."
                );
            }
            throw new NotSupportedException(
                $"Three-dimensional graphics loader does not support asset type {typeof(T).FullName}."
            );
        }
    }
}
