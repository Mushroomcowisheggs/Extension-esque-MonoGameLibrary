using System;
using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Base contract for all texture resources.
    /// Ownership follows the creator: a texture created by <see cref="ITextureFactory"/>
    /// or <see cref="IRenderTargetFactory"/> is owned by the caller, which must dispose it.
    /// A texture obtained from <see cref="IContentService"/> stays owned by the content
    /// service, which releases it on unload; disposing such a texture is harmless but
    /// does not release the shared asset.
    /// </summary>
    public interface ITexture : IAsset, IDisposable {
        /// <summary>Gets the width of the texture in pixels.</summary>
        int Width { get; }
        
        /// <summary>Gets the height of the texture in pixels.</summary>
        int Height { get; }
    }
}