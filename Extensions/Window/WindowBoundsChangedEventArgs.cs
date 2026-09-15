using System;

namespace MonoGameLibrary.Extensions.Window {
    /// <summary>
    /// Reports the new window metrics after the client area or the render surface changed.
    /// Both sizes are provided because on high DPI displays the client area measured in
    /// window units differs from the render surface measured in pixels.
    /// </summary>
    public sealed class WindowBoundsChangedEventArgs : EventArgs {
        private readonly int _widthClient;
        private readonly int _heightClient;
        private readonly int _widthPixel;
        private readonly int _heightPixel;
        
        /// <summary>Gets the client area width in window units.</summary>
        public int ClientWidth {
            get {
                return _widthClient;
            }
        }
        
        /// <summary>Gets the client area height in window units.</summary>
        public int ClientHeight {
            get {
                return _heightClient;
            }
        }
        
        /// <summary>Gets the render surface width in pixels.</summary>
        public int PixelWidth {
            get {
                return _widthPixel;
            }
        }
        
        /// <summary>Gets the render surface height in pixels.</summary>
        public int PixelHeight {
            get {
                return _heightPixel;
            }
        }
        
        /// <summary>Initializes a new instance with the supplied metrics.</summary>
        /// <param name="widthClient">The client area width in window units.</param>
        /// <param name="heightClient">The client area height in window units.</param>
        /// <param name="widthPixel">The render surface width in pixels.</param>
        /// <param name="heightPixel">The render surface height in pixels.</param>
        public WindowBoundsChangedEventArgs(int widthClient, int heightClient, int widthPixel, int heightPixel) {
            _widthClient = widthClient;
            _heightClient = heightClient;
            _widthPixel = widthPixel;
            _heightPixel = heightPixel;
        }
    }
}