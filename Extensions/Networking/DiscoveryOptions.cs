using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Tunes the behaviour of <see cref="IDiscoveryService"/>.
    /// </summary>
    /// <remarks>
    /// The defaults reproduce the cadence players expect from a Minecraft-style LAN list: a room announces
    /// itself every one and a half seconds, a browser probes on the same rhythm, and an entry that misses
    /// three announcements disappears from the list.
    /// </remarks>
    public sealed class DiscoveryOptions {
        /// <summary>
        /// Gets or sets the UDP port on which rooms are announced and probed. Every host and every browser on
        /// the network uses the same value, which is what makes the list work without configuration.
        /// </summary>
        public int DiscoveryPort { get; set; } = NetworkDefaults.DiscoveryPort;
        
        /// <summary>
        /// Gets or sets how often a host repeats its advert.
        /// </summary>
        public int AnnouncementIntervalMilliseconds { get; set; } = 1500;
        
        /// <summary>
        /// Gets or sets how often a browser asks every room to identify itself.
        /// </summary>
        public int ProbeIntervalMilliseconds { get; set; } = 1500;
        
        /// <summary>
        /// Gets or sets how long an entry survives without a fresh advert. Three missed announcements is the
        /// shortest value that tolerates a single dropped datagram.
        /// </summary>
        public int RoomExpiryMilliseconds { get; set; } = 5000;
        
        /// <summary>
        /// Gets or sets a value indicating whether adverts are sent to the limited broadcast address
        /// <c>255.255.255.255</c>.
        /// </summary>
        public bool EnableLimitedBroadcast { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether adverts are sent to the directed broadcast address of every
        /// usable IPv4 interface. This is what reaches a room on a different subnet of the same local network
        /// and what keeps discovery working when a virtual adapter is enumerated first.
        /// </summary>
        public bool EnableSubnetBroadcast { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether the loopback interface is included in the broadcast
        /// targets, which lets two instances on one machine find each other.
        /// </summary>
        public bool EnableLoopbackInterface { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether rooms that announce a different application name are
        /// ignored. Turning this off makes a browser list the rooms of unrelated applications too.
        /// </summary>
        public bool EnableForeignApplicationFilter { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the local address the discovery socket binds to, or null to bind every address.
        /// Binding one address is how a host keeps discovery off a virtual adapter, which is the usual cause
        /// of a room that announces itself on a network nobody else is on.
        /// </summary>
        public System.Net.IPAddress BindAddress { get; set; }
        
        /// <summary>
        /// Gets or sets the largest number of room notifications a single update reports. The limit keeps a
        /// burst of adverts from stretching one frame.
        /// </summary>
        public int MaxDispatchPerUpdate { get; set; } = 64;
        
        /// <summary>
        /// Gets or sets the largest datagram a browser accepts.
        /// </summary>
        public int MaxPacketBytes { get; set; } = 4096;
        
        /// <summary>
        /// Gets or sets the largest number of rooms a browser keeps. Further rooms are ignored until an entry
        /// expires.
        /// </summary>
        public int MaxRooms { get; set; } = 64;
        
        /// <summary>
        /// Verifies that every value can be used to browse or to announce.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if a numeric value is outside its allowed range.</exception>
        public void Validate() {
            if (DiscoveryPort < 1 || DiscoveryPort > 65535) {
                throw new ArgumentOutOfRangeException(nameof(DiscoveryPort), "A port must be between 1 and 65535.");
            }
            if (AnnouncementIntervalMilliseconds < 100) {
                throw new ArgumentOutOfRangeException(nameof(AnnouncementIntervalMilliseconds), "The announcement interval must be at least 100 milliseconds.");
            }
            if (ProbeIntervalMilliseconds < 100) {
                throw new ArgumentOutOfRangeException(nameof(ProbeIntervalMilliseconds), "The probe interval must be at least 100 milliseconds.");
            }
            if (RoomExpiryMilliseconds <= AnnouncementIntervalMilliseconds) {
                throw new ArgumentOutOfRangeException(nameof(RoomExpiryMilliseconds), "A room entry must outlive at least one announcement interval.");
            }
            if (MaxPacketBytes < 256) {
                throw new ArgumentOutOfRangeException(nameof(MaxPacketBytes), "The datagram limit is too small to carry a room advert.");
            }
            if (MaxRooms < 1) {
                throw new ArgumentOutOfRangeException(nameof(MaxRooms), "At least one room must be kept.");
            }
        }
    }
}
