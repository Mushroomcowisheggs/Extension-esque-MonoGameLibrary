using System;
using System.IO;
using Godot;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Godot.Graphics {
    /// <summary>
    /// Godot implementation of <see cref="ITextureFactory"/>.
    /// Textures are created directly from raw pixels or decoded image bytes through
    /// <see cref="Image"/> and <see cref="ImageTexture"/> rather than through Godot's
    /// resource import pipeline, which is what lets a game synthesize a single pixel at
    /// runtime or decode a PNG that was never imported as a Godot resource.
    /// Backend failures are surfaced as <see cref="ContentLoadException"/>.
    /// </summary>
    public sealed class TextureFactory : ITextureFactory {
        /// <inheritdoc />
        public ITwoDimensionalTexture CreateFromPixels(int width, int height, MonoGameLibrary.Core.Primitives.Color[] arrayPixelColors) {
            if (width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if (height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
            if (arrayPixelColors == null) { throw new ArgumentNullException(nameof(arrayPixelColors)); }
            if (arrayPixelColors.Length != width * height) {
                throw new ArgumentException("Pixel data length must equal width multiplied by height.", nameof(arrayPixelColors));
            }
            byte[] bufferPixels = new byte[arrayPixelColors.Length * 4];
            for (int index = 0; index < arrayPixelColors.Length; index += 1) {
                bufferPixels[index * 4 + 0] = arrayPixelColors[index].R;
                bufferPixels[index * 4 + 1] = arrayPixelColors[index].G;
                bufferPixels[index * 4 + 2] = arrayPixelColors[index].B;
                bufferPixels[index * 4 + 3] = arrayPixelColors[index].A;
            }
            Image image = null;
            try {
                image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, bufferPixels);
                ImageTexture texture = ImageTexture.CreateFromImage(image);
                return new TwoDimensionalTexture(texture);
            } catch (Exception) {
                // The texture never became the caller's property, so release it here.
                if (image != null) {
                    image.Dispose();
                }
                throw;
            }
        }
        
        /// <inheritdoc />
        public ITwoDimensionalTexture CreateFromStream(Stream streamData) {
            if (streamData == null) { throw new ArgumentNullException(nameof(streamData)); }
            try {
                byte[] bufferImage = ReadAllBytes(streamData);
                Image image = DecodeImage(bufferImage);
                ImageTexture texture = ImageTexture.CreateFromImage(image);
                return new TwoDimensionalTexture(texture);
            } catch (Exception exception) when (IsLoadFailure(exception)) {
                throw new ContentLoadException("The image stream could not be decoded into a texture.", exception);
            }
        }
        
        private static byte[] ReadAllBytes(Stream streamData) {
            using (MemoryStream streamBuffer = new MemoryStream()) {
                streamData.CopyTo(streamBuffer);
                return streamBuffer.ToArray();
            }
        }
        
        private static Image DecodeImage(byte[] bufferImage) {
            Image image = new Image();
            Error error = image.LoadPngFromBuffer(bufferImage);
            if (error != Error.Ok) { error = image.LoadJpgFromBuffer(bufferImage); }
            if (error != Error.Ok) { error = image.LoadWebpFromBuffer(bufferImage); }
            if (error != Error.Ok) { error = image.LoadTgaFromBuffer(bufferImage); }
            if (error != Error.Ok) { error = image.LoadBmpFromBuffer(bufferImage); }
            if (error != Error.Ok) {
                image.Dispose();
                throw new InvalidOperationException(
                    $"The image bytes matched no supported decoder. Last error: {error}."
                );
            }
            return image;
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
