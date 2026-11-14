using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Service for querying keyboard and gamepad input states.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IInputService {
        /// <summary>
        /// Checks whether the specified key is currently held down.
        /// </summary>
        /// <param name="codeKey">The key to check.</param>
        /// <returns>True if the key is down; otherwise false.</returns>
        bool IsKeyDown(KeyCode codeKey);
        
        /// <summary>
        /// Checks whether the specified key is currently up (not pressed).
        /// </summary>
        /// <param name="codeKey">The key to check.</param>
        /// <returns>True if the key is up; otherwise false.</returns>
        bool IsKeyUp(KeyCode codeKey);
        
        /// <summary>
        /// Returns true if the key was pressed this frame and was not pressed in the previous frame.
        /// </summary>
        /// <param name="codeKey">The key to check.</param>
        /// <returns>True if the key was just pressed; otherwise false.</returns>
        bool WasKeyJustPressed(KeyCode codeKey);
        
        /// <summary>
        /// Returns true if the key was released this frame and was pressed in the previous frame.
        /// </summary>
        /// <param name="codeKey">The key to check.</param>
        /// <returns>True if the key was just released; otherwise false.</returns>
        bool WasKeyJustReleased(KeyCode codeKey);
        
        /// <summary>
        /// Checks whether the specified gamepad button is currently held down.
        /// </summary>
        /// <param name="indexPlayer">The player index (which gamepad).</param>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button is down; otherwise false.</returns>
        bool IsButtonDown(PlayerIndex indexPlayer, GamePadButton button);
        
        /// <summary>
        /// Returns true if the button was pressed this frame and was not pressed previously.
        /// </summary>
        /// <param name="indexPlayer">The player index.</param>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button was just pressed; otherwise false.</returns>
        bool WasButtonJustPressed(PlayerIndex indexPlayer, GamePadButton button);
        
        /// <summary>
        /// Returns true if the button was released this frame and was pressed previously.
        /// </summary>
        /// <param name="indexPlayer">The player index.</param>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button was just released; otherwise false.</returns>
        bool WasButtonJustReleased(PlayerIndex indexPlayer, GamePadButton button);
        
        /// <summary>
        /// Updates the input state for the current frame. Must be called once per frame.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
    }
}