using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Service for querying pointer movement and button state.
    /// Positions are reported in window client coordinates, which is the same
    /// space the platform reports raw pointer positions in; mapping them onto a
    /// rendering surface is the responsibility of the caller that owns the layout.
    /// While the window is not focused the service reports no pressed buttons and
    /// suppresses the resulting edges, so a held button cannot produce a click
    /// when focus returns.
    /// </summary>
    public interface IPointerInputService {
        /// <summary>
        /// Gets a value indicating whether pointer state can be read from this service.
        /// Backends without a pointer device report false; the desktop backends report
        /// true for as long as the service is alive.
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Gets a value indicating whether the window currently has input focus.
        /// While focus is lost every button reads as released and no edge is reported,
        /// so callers can gate their own release handling on this flag.
        /// </summary>
        bool IsFocused { get; }
        
        /// <summary>Gets the current pointer X position in window client coordinates.</summary>
        int X { get; }
        
        /// <summary>Gets the current pointer Y position in window client coordinates.</summary>
        int Y { get; }
        
        /// <summary>Gets the vertical wheel movement accumulated since the previous frame.</summary>
        int ScrollDelta { get; }
        
        /// <summary>Gets a value indicating whether the left button is currently held.</summary>
        bool IsLeftButtonDown { get; }
        
        /// <summary>Gets a value indicating whether the right button is currently held.</summary>
        bool IsRightButtonDown { get; }
        
        /// <summary>Gets a value indicating whether the middle button is currently held.</summary>
        bool IsMiddleButtonDown { get; }
        
        /// <summary>Gets a value indicating whether the left button was pressed during this frame.</summary>
        bool WasLeftButtonJustPressed { get; }
        
        /// <summary>Gets a value indicating whether the left button was released during this frame.</summary>
        bool WasLeftButtonJustReleased { get; }
        
        /// <summary>Gets a value indicating whether the right button was pressed during this frame.</summary>
        bool WasRightButtonJustPressed { get; }
        
        /// <summary>Gets a value indicating whether the right button was released during this frame.</summary>
        bool WasRightButtonJustReleased { get; }
        
        /// <summary>Gets a value indicating whether the middle button was pressed during this frame.</summary>
        bool WasMiddleButtonJustPressed { get; }
        
        /// <summary>Gets a value indicating whether the middle button was released during this frame.</summary>
        bool WasMiddleButtonJustReleased { get; }
        
        /// <summary>
        /// Samples the pointer for the current frame. Must be called once per frame
        /// before any of the state properties are read.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
    }
}