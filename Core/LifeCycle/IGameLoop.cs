using System;
using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Core.Lifecycle {
    /// <summary>
    /// Defines a custom game loop that replaces any framework-specific Game loop. 
    /// Implementations manage window creation, graphics device initialization,
    /// and frame-driven update/draw cycles.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Caller)]
    public interface IGameLoop : IDisposable {
        /// <summary>
        /// Starts the game loop. This call blocks until the game exits. 
        /// </summary>
        void Run();
        
        /// <summary>
        /// Requests the game loop to exit gracefully. 
        /// </summary>
        void Exit();
        
        /// <summary>
        /// Requests the game loop to exit gracefully and report the supplied
        /// process exit code on shutdown.
        /// </summary>
        /// <param name="codeExit">The process exit code to report.</param>
        void Exit(int codeExit);
    }
}