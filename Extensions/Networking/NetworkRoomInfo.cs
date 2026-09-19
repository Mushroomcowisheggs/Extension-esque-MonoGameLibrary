using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Describes a joinable room. The same object is used to open a room, to answer a connection
    /// handshake, and to advertise the room on the local network, so that a browsing player sees exactly
    /// the information a connecting player receives.
    /// </summary>
    public sealed class NetworkRoomInfo {
        /// <summary>
        /// Gets or sets the display name of the room.
        /// </summary>
        public string RoomName { get; set; } = "Room";
        
        /// <summary>
        /// Gets or sets the display name of the hosting player.
        /// </summary>
        public string HostName { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the free-form description shown next to the room name, equivalent to a server
        /// message of the day.
        /// </summary>
        public string Description { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the port at which the room accepts connections. It is what a joined player and a
        /// browsing player both need in order to reach the room.
        /// </summary>
        public int Port { get; set; } = NetworkDefaults.GamePort;
        
        /// <summary>
        /// Gets or sets the number of players currently in the room, including the host.
        /// </summary>
        public int PlayerCount { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the maximum number of players the room accepts.
        /// </summary>
        public int MaxPlayers { get; set; } = 8;
        
        /// <summary>
        /// Gets or sets the protocol version the room speaks.
        /// </summary>
        public int ProtocolVersion { get; set; } = NetworkDefaults.ProtocolVersion;
        
        /// <summary>
        /// Gets or sets the name of the application that opened the room.
        /// </summary>
        public string ApplicationName { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the version of the application that opened the room.
        /// </summary>
        public string ApplicationVersion { get; set; } = "";
        
        /// <summary>
        /// Creates an independent copy of this description.
        /// </summary>
        /// <returns>A new instance with the same values.</returns>
        public NetworkRoomInfo Clone() {
            NetworkRoomInfo copy = new NetworkRoomInfo();
            copy.RoomName = RoomName;
            copy.HostName = HostName;
            copy.Description = Description;
            copy.Port = Port;
            copy.PlayerCount = PlayerCount;
            copy.MaxPlayers = MaxPlayers;
            copy.ProtocolVersion = ProtocolVersion;
            copy.ApplicationName = ApplicationName;
            copy.ApplicationVersion = ApplicationVersion;
            return copy;
        }
        
        /// <summary>
        /// Verifies that the description can be hosted and advertised.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if a text field is null or empty where a value is required.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if a numeric field is outside its allowed range.</exception>
        public void Validate() {
            if (string.IsNullOrWhiteSpace(RoomName)) {
                throw new ArgumentException("A room name is required.", nameof(RoomName));
            }
            if (MaxPlayers < 1) {
                throw new ArgumentOutOfRangeException(nameof(MaxPlayers), "A room must accept at least one player.");
            }
            if (PlayerCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(PlayerCount), "The player count cannot be negative.");
            }
            if (Port < 0 || Port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(Port), "A port must be between 0 and 65535. Zero asks the operating system to choose one.");
            }
            if (ProtocolVersion < 1) {
                throw new ArgumentOutOfRangeException(nameof(ProtocolVersion), "The protocol version must be positive.");
            }
        }
    }
}
