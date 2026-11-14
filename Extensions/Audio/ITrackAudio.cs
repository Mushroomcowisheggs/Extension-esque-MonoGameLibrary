using System;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.Audio {
    /// <summary>
    /// Represents a long-form audio track that can be played. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Caller)]
    public interface ITrackAudio : IAsset {
        /// <summary>Duration of the audio track. </summary>
        TimeSpan Duration { get; }
    }
}