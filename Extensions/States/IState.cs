using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.States {
    /// <summary>
    /// Represents a single behavior state in a finite state machine. 
    /// Used to model entity behaviors such as idle, moving, attacking, etc. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Caller)]
    public interface IState {
        /// <summary>Called when this state becomes active. </summary>
        void Enter();
        /// <summary>Called when this state is deactivated. </summary>
        void Exit();
        /// <summary>Called every frame while this state is active. </summary>
        void Update(FrameTime timeFrame);
    }
}