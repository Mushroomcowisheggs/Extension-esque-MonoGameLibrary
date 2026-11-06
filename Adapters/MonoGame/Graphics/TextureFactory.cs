using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame implementation of <see cref="ITextureFactory"/>.
    /// Textures are created directly on the graphics device rather than through the
    /// compiled content pipeline, which is what lets a game load a PNG that was not
    /// processed by the content builder or synthesize a single pixel at runtime.
    /// Backend failures are surfaced as <see cref="ContentLoadException"/>.
    /// </summary>
    public sealed class TextureFactory : ITextureFactory {
        private readonly GraphicsDevice _device;
        
        /// <summary>Initializes a new instance of the <see cref="TextureFactory"/> class.</summary>
        /// <param name="device">The graphics device that owns every created texture.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="device"/> is null.</exception>
        public TextureFactory(GraphicsDevice device) {
            if (device == null) { throw new ArgumentNullException(nameof(device)); }
            _device = device;
        }
        
        /// <inheritdoc />
        public ITwoDimensionalTexture CreateFromPixels(int width, int height, Color[] arrayPixelColors) {
            if (width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if (height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
            if (arrayPixelColors == null) { throw new ArgumentNullException(nameof(arrayPixelColors)); }
            if (arrayPixelColors.Length != width * height) {
                throw new ArgumentException("Pixel data length must equal width multiplied by height.", nameof(arrayPixelColors));
            }
            Texture2D texture = new Texture2D(_device, width, height);
            try {
                Microsoft.Xna.Framework.Color[] arrayColor = new Microsoft.Xna.Framework.Color[arrayPixelColors.Length];
                for (int index = 0; index < arrayPixelColors.Length; index += 1) {
                    arrayColor[index] = new Microsoft.Xna.Framework.Color(
                        arrayPixelColors[index].R, arrayPixelColors[index].G, arrayPixelColors[index].B, arrayPixelColors[index].A
                    );
                }
                texture.SetData(arrayColor);
            } catch (Exception) {
                // The texture never became the caller's property, so release it here.
                texture.Dispose();
                throw;
            }
            return new TwoDimensionalTexture(texture);
        }
        
        /// <inheritdoc />
        public ITwoDimensionalTexture CreateFromStream(Stream streamData) {
            if (streamData == null) { throw new ArgumentNullException(nameof(streamData)); }
            try {
                Texture2D texture = Texture2D.FromStream(_device, streamData);
                return new TwoDimensionalTexture(texture);
            } catch (Exception exception) when (IsLoadFailure(exception)) {
                throw new ContentLoadException("The image stream could not be decoded into a texture.", exception);
            }
        }
        
        private static bool IsLoadFailure(Exception exception) {
            if (exception is InvalidOperationException) { return true; }
            if (exception is ArgumentException) { return true; }
            if (exception is IOException) { return true; }
            if (exception is NotSupportedException) { return true; }
            return false;
        }
    }
}