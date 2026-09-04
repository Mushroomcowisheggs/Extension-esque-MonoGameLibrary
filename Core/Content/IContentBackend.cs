using System;
using System.IO;

namespace MonoGameLibrary.Core.Content {
    /// <summary>
    /// Adapter-facing content operations. Feature adapters register reusable
    /// loaders against this Core contract instead of referencing a sibling
    /// content-adapter implementation.
    /// </summary>
    public interface IContentBackend : IContentService {
        /// <summary>
        /// Registers a loader that receives the content backend on which the
        /// asset is requested. Such loaders can be copied safely to child
        /// content services.
        /// </summary>
        void RegisterScoped<T>(Func<IContentBackend, string, T> loader)
        where T : class, IAsset;
        
        /// <summary>Loads a backend-native asset without exposing its platform in Core.</summary>
        TNative LoadNative<TNative>(string nameAsset) where TNative : class;

        /// <summary>Opens a stream relative to the configured content root.</summary>
        Stream OpenStream(string pathFile);
    }
}
