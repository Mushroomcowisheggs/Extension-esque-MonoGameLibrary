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
        public static T Load<T>(IContentBackendService serviceContent, string nameAsset)
        where T : class, IAsset {
            if (serviceContent == null) {
                throw new ArgumentNullException(nameof(serviceContent));
            }
            if (typeof(T) == typeof(IClipAudio)) {
                return new ClipAudio(serviceContent.LoadNative<SoundEffect>(nameAsset)) as T;
            }
            if (typeof(T) == typeof(ITrackAudio)) {
                return new TrackAudio(serviceContent.LoadNative<Song>(nameAsset)) as T;
            }
            throw new NotSupportedException($"Audio loader does not support asset type {typeof(T).FullName}.");
        }
    }
}
