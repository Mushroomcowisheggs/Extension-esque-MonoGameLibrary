using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Loads a platform-neutral mesh asset through the content backend.
    /// </summary>
    public interface IMeshLoader {
        /// <summary>
        /// Loads a mesh by asset name.
        /// </summary>
        /// <param name="backendContent">The content backend.</param>
        /// <param name="nameAsset">The content asset name.</param>
        /// <returns>The loaded mesh.</returns>
        IMesh Load(IContentBackend backendContent, string nameAsset);
    }
}
