using System;

namespace MonoGameLibrary.Extensions.Window {
    /// <summary>
    /// Controls the platform window at runtime: its two size spaces, its position,
    /// its border mode, and the minimum size the user may shrink it to.
    /// The client area and the render surface are named separately on purpose.
    /// On a high DPI display the client area is measured in window units while the
    /// render surface is measured in pixels, so a single "size" would be ambiguous.
    /// </summary>
    public interface IWindowService {
        /// <summary>Gets the client area width in window units.</summary>
        int ClientWidth { get; }
        
        /// <summary>Gets the client area height in window units.</summary>
        int ClientHeight { get; }
        
        /// <summary>Gets the render surface width in pixels.</summary>
        int PixelWidth { get; }
        
        /// <summary>Gets the render surface height in pixels.</summary>
        int PixelHeight { get; }
        
        /// <summary>Gets the window X position on the desktop.</summary>
        int X { get; }
        
        /// <summary>Gets the window Y position on the desktop.</summary>
        int Y { get; }
        
        /// <summary>Gets the width of the display the window currently occupies.</summary>
        int DisplayWidth { get; }
        
        /// <summary>Gets the height of the display the window currently occupies.</summary>
        int DisplayHeight { get; }
        
        /// <summary>Gets a value indicating whether the window is drawn without a border.</summary>
        bool IsBorderless { get; }
        
        /// <summary>Gets a value indicating whether the window currently has input focus.</summary>
        bool IsFocused { get; }
        
        /// <summary>Gets or sets a value indicating whether the user may resize the window.</summary>
        bool IsResizable { get; set; }
        
        /// <summary>Gets or sets a value indicating whether the pointer is drawn over the window.</summary>
        bool IsMouseVisible { get; set; }
        
        /// <summary>
        /// Gets or sets the smallest client area width the user may resize the window to.
        /// Zero disables the minimum.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is negative.</exception>
        int MinimumClientWidth { get; set; }
        
        /// <summary>
        /// Gets or sets the smallest client area height the user may resize the window to.
        /// Zero disables the minimum.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is negative.</exception>
        int MinimumClientHeight { get; set; }
        
        /// <summary>
        /// Raised during the update pass whenever the client area or the render surface
        /// changed since the previous frame.
        /// </summary>
        event EventHandler<WindowBoundsChangedEventArgs> BoundsChanged;
        
        /// <summary>
        /// Requests a new client area size. The request is clamped to the configured
        /// minimum; the resulting metrics are reported through <see cref="BoundsChanged"/>
        /// on the next update pass. A window the user shrank below the minimum is
        /// restored once per size change rather than once per frame.
        /// </summary>
        /// <param name="width">The requested client width in window units.</param>
        /// <param name="height">The requested client height in window units.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="width"/> or <paramref name="height"/> is not positive.
        /// </exception>
        void SetClientSize(int width, int height);
        
        /// <summary>Moves the window to the supplied desktop position.</summary>
        /// <param name="x">The requested X position on the desktop.</param>
        /// <param name="y">The requested Y position on the desktop.</param>
        void SetPosition(int x, int y);
        
        /// <summary>Switches the window between bordered and borderless presentation.</summary>
        /// <param name="flagBorderless">True to remove the window border; false to restore it.</param>
        void SetBorderless(bool flagBorderless);
    }
}