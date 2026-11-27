using System;
using Godot;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.Audio;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    public static class AudioLoaders {
        /// <summary>
        /// Creates a loader for the specified audio asset type.
        /// Supported types: IClipAudio, ITrackAudio.
        /// Both load a Godot <see cref="AudioStream"/>; the loaded resource must be an audio
        /// stream or loading fails with <see cref="ContentLoadException"/>, and every stream
        /// subtype (WAV, MP3, Ogg Vorbis) is accepted.
        /// </summary>
        public static T Load<T>(IContentBackendService serviceContent, string nameAsset)
        where T : class, IAsset {
            if (serviceContent == null) {
                throw new ArgumentNullException(nameof(serviceContent));
            }
            if (typeof(T) == typeof(IClipAudio)) {
                return new ClipAudio(serviceContent.LoadNative<AudioStream>(nameAsset)) as T;
            }
            if (typeof(T) == typeof(ITrackAudio)) {
                return new TrackAudio(serviceContent.LoadNative<AudioStream>(nameAsset)) as T;
            }
            throw new NotSupportedException($"Audio loader does not support asset type {typeof(T).FullName}.");
        }
    }
}
