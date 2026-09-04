using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Service for managing a user interface tree (rendering and interaction). 
    /// </summary>
    public interface IUserInterfaceService : IDisposable {
        /// <summary>
        /// Initializes the UI system. Must be called once before any UI operations.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Updates the UI system each frame. 
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
        
        /// <summary>
        /// Draws the entire UI tree.
        /// </summary>
        void Draw();
        
        /// <summary>
        /// Removes all UI elements from the root container. 
        /// </summary>
        void ClearRoot();
        
        /// <summary>
        /// Adds a UI element to the root container.
        /// </summary>
        /// <param name="element">The element to add. Must be a platform-specific UI element.</param>
        void AddToRoot(object element);
        
        /// <summary>
        /// Sets the canvas size and zoom factor.
        /// </summary>
        /// <param name="width">Canvas width in pixels.</param>
        /// <param name="height">Canvas height in pixels.</param>
        /// <param name="zoom">Zoom multiplier.</param>
        void SetCanvas(float width, float height, float zoom);
        
        /// <summary>
        /// Enables or disables keyboard and/or gamepad input for UI navigation.
        /// </summary>
        /// <param name="flagEnableKeyboard">Whether keyboard input should be enabled. Default true. </param>
        /// <param name="flagEnableGamepad">Whether gamepad input should be enabled. Default true. </param>
        void ConfigureInput(bool flagEnableKeyboard = true, bool flagEnableGamepad = true);
        
        void AddNavigationForwardKey(NavigationKey key);
        void AddNavigationReverseKey(NavigationKey key);
        void RemoveNavigationForwardKey(NavigationKey key);
        void RemoveNavigationReverseKey(NavigationKey key);
    }
}
