using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

using System;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>
    /// Manages a stack of game screens.
    /// </summary>
    public interface IScreenService : IDisposable {
        /// <summary>
        /// Gets the current active screen.
        /// </summary>
        Screen CurrentScreen { get; }
        
        /// <summary>
        /// Pushes a new screen onto the stack.
        /// </summary>
        void Push(Screen screen);
        
        /// <summary>
        /// Pops the current screen from the stack.
        /// </summary>
        void Pop();
        
        /// <summary>
        /// Replaces the entire stack with a single screen.
        /// </summary>
        void Change(Screen screen);
        
        /// <summary>
        /// Updates all active screens.
        /// </summary>
        void Update(FrameTime timeFrame);
        
        /// <summary>
        /// Draws all visible screens.
        /// </summary>
        void Draw(FrameTime timeFrame);
    }
}
