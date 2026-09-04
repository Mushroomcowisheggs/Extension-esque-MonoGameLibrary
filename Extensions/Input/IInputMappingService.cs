using System;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Provides a high-level input mapping service that decouples game actions
    /// from physical keys or buttons. Actions are defined by the game as enums.
    /// </summary>
    public interface IInputMappingService {
        /// <summary>
        /// Binds an action to a keyboard key.
        /// </summary>
        /// <typeparam name="T">Action enum type.</typeparam>
        /// <param name="action">The action to bind.</param>
        /// <param name="key">The key that triggers the action.</param>
        void BindKey<T>(T action, KeyCode code) where T : Enum;
        
        /// <summary>
        /// Binds an action to a gamepad button.
        /// </summary>
        /// <typeparam name="T">Action enum type.</typeparam>
        /// <param name="action">The action to bind.</param>
        /// <param name="indexPlayer">The player index.</param>
        /// <param name="button">The button that triggers the action.</param>
        void BindButton<T>(T action, PlayerIndex indexPlayer, GamePadButton button) where T : Enum;
        
        /// <summary>
        /// Checks whether the action was just pressed (edge-triggered) this frame.
        /// </summary>
        bool IsActionPressed<T>(T action) where T : Enum;
        
        /// <summary>
        /// Checks whether the action is currently being held down (continuous state).
        /// </summary>
        bool IsActionHeld<T>(T action) where T : Enum;
        
        /// <summary>
        /// Computes a direction vector from four directional actions.
        /// </summary>
        /// <param name="up">Action for up direction.</param>
        /// <param name="down">Action for down direction.</param>
        /// <param name="left">Action for left direction.</param>
        /// <param name="right">Action for right direction.</param>
        /// <returns>A normalized direction vector based on held actions.</returns>
        TwoDimensionalVector GetActionDirection<T>(T up, T down, T left, T right) where T : Enum;
        
        /// <summary>
        /// Updates the mapping service state. Called once per frame by the input module.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
    }
}
