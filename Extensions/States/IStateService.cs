using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.States {
    /// <summary>
    /// Manages a stack of behavior states (<see cref="IState"/>). 
    /// Supports push, pop, and replace operations. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IStateService {
        /// <summary>
        /// Gets the current active state (top of the stack). 
        /// Returns null if the stack is empty. 
        /// </summary>
        IState CurrentState { get; }
        
        /// <summary>
        /// Pushes a new state onto the stack. The new state becomes active, 
        /// and the previous state is suspended (its <see cref="IState.Exit"/> is called). 
        /// </summary>
        void Push(IState state);
        
        /// <summary>
        /// Pops the current state from the stack. The previous state (if any) 
        /// becomes active again (its <see cref="IState.Enter"/> is called). 
        /// </summary>
        void Pop();
        
        /// <summary>
        /// Replaces the entire stack with a single new state. 
        /// All existing states are exited and discarded. 
        /// </summary>
        void Change(IState state);
        
        /// <summary>
        /// Updates the current state (and optionally transparent states below). 
        /// </summary>
        void Update(FrameTime timeFrame);
    }
}