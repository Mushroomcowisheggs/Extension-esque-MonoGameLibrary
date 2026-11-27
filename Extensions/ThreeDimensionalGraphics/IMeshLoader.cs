using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Loads a platform-neutral mesh asset through the content backend.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IMeshLoader {
        /// <summary>
        /// Loads a mesh by asset name.
        /// </summary>
        /// <param name="serviceContent">The content backend.</param>
        /// <param name="nameAsset">The content asset name.</param>
        /// <returns>The loaded mesh.</returns>
        IMesh Load(IContentBackendService serviceContent, string nameAsset);
    }
}
