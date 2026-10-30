using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.MonoGame.Audio {
    public static class AudioLoaders {
        /// <summary>
        /// Creates a loader for the specified audio asset type.
        /// Supported types: IClipAudio, ITrackAudio.
        /// </summary>
        public static T Load<T>(IContentBackend backendContent, string nameAsset)
            where T : class, IAsset {
            if (backendContent == null) {
                throw new ArgumentNullException(nameof(backendContent));
            }
            if (typeof(T) == typeof(IClipAudio)) {
                return new ClipAudio(backendContent.LoadNative<SoundEffect>(nameAsset)) as T;
            }
            if (typeof(T) == typeof(ITrackAudio)) {
                return new TrackAudio(backendContent.LoadNative<Song>(nameAsset)) as T;
            }
            throw new NotSupportedException($"Audio loader does not support asset type {typeof(T).FullName}.");
        }
    }
}
