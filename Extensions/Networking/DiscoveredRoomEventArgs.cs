using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Reports that a room appeared, changed or disappeared in the browsed list.
    /// </summary>
    public sealed class DiscoveredRoomEventArgs : EventArgs {
        private readonly DiscoveredRoom _roomDiscovered;
        
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="roomDiscovered">The room the event is about.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="roomDiscovered"/> is null.</exception>
        public DiscoveredRoomEventArgs(DiscoveredRoom roomDiscovered) {
            if (roomDiscovered == null) {
                throw new ArgumentNullException(nameof(roomDiscovered));
            }
            _roomDiscovered = roomDiscovered;
        }
        
        /// <summary>
        /// Gets the room the event is about.
        /// </summary>
        public DiscoveredRoom Room {
            get {
                return _roomDiscovered;
            }
        }
    }
}
