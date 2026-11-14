using System;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Extensions.Scenes {
    /// <summary>
    /// Provides scene switching and lifecycle orchestration for the active scene. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface ISceneService : IDisposable {
        /// <summary>
        /// Gets the currently active scene. 
        /// </summary>
        Scene CurrentScene { get; }

        /// <summary>
        /// Switches to the specified scene. 
        /// </summary>
        /// <param name="scene">The scene instance to switch to. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scene"/> is null. </exception>
        void ChangeScene(Scene scene);
        
        /// <summary>
        /// Updates the active scene's logic. Called by the module wrapper each frame. 
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
        
        /// <summary>
        /// Draws the active scene. Called by the module wrapper each frame. 
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Draw(FrameTime timeFrame);
    }
}
