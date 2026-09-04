using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>
    /// Contract for a game screen managed by <see cref="IScreenService"/>.
    /// </summary>
    public interface IScreen : IDisposable {
        /// <summary>Occurs when a screen change (push, pop, or change) is requested.</summary>
        event EventHandler<ScreenChangeEventArguments> ScreenChangeRequested;
        
        /// <summary>Called once to load content. </summary>
        void LoadContent();
        
        /// <summary>Called after <see cref="LoadContent"/> to perform setup that depends on loaded assets. </summary>
        void Initialize();
        
        /// <summary>Whether screens beneath this one should still be drawn. </summary>
        bool IsTransparent { get; }
        
        /// <summary>Whether screens beneath this one should receive input. </summary>
        bool IsBlocking { get; }
        
        /// <summary>Called when this screen becomes active. </summary>
        void Enter();
        
        /// <summary>Called when this screen is no longer active. </summary>
        void Exit();
        
        /// <summary>Updates the screen logic each frame. </summary>
        void Update(FrameTime timeFrame);
        
        /// <summary>Draws the screen each frame.</summary>
        void Draw(FrameTime timeFrame);
    }
}
