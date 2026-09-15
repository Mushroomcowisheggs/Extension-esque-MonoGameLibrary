using System.IO;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Creates textures outside of the compiled content pipeline, for example
    /// from a decoded image stream or from raw pixel data produced at runtime.
    /// Textures created here are owned by the caller and must be disposed by it.
    /// </summary>
    public interface ITextureFactory {
        /// <summary>
        /// Creates a texture filled with the supplied pixels.
        /// </summary>
        /// <param name="width">Texture width in pixels. Must be greater than zero.</param>
        /// <param name="height">Texture height in pixels. Must be greater than zero.</param>
        /// <param name="dataPixel">Row major pixel data. Must contain width * height entries.</param>
        /// <returns>The newly created texture.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="width"/> or <paramref name="height"/> is not positive.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dataPixel"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the pixel array does not contain exactly width multiplied by height entries.
        /// </exception>
        ITwoDimensionalTexture CreateFromPixels(int width, int height, Color[] dataPixel);
        
        /// <summary>
        /// Decodes an encoded image stream (PNG, JPEG, and other formats the
        /// backend supports) into a texture.
        /// </summary>
        /// <param name="streamData">A readable stream positioned at the start of the encoded image.</param>
        /// <returns>The newly created texture.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="streamData"/> is null.</exception>
        /// <exception cref="MonoGameLibrary.Core.Content.ContentLoadException">
        /// Thrown when the stream cannot be decoded into a texture.
        /// </exception>
        ITwoDimensionalTexture CreateFromStream(Stream streamData);
    }
}