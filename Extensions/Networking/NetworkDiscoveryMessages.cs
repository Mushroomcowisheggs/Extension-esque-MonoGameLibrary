using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Declares the datagram messages that rooms and browsers exchange on the local network, together with
    /// their reserved kinds.
    /// </summary>
    public static class NetworkDiscoveryMessages {
        /// <summary>The kind of <see cref="RoomAdvertisement"/>.</summary>
        public const string AdvertisementKind = NetworkDefaults.KindPrefix + "room.advertisement";
        
        /// <summary>The kind of <see cref="RoomProbeRequest"/>.</summary>
        public const string ProbeKind = NetworkDefaults.KindPrefix + "room.probe";
    }
    
    /// <summary>
    /// Describes a room to every browser on the local network. A host sends it on its own schedule and as an
    /// answer to a probe, which is how a room keeps appearing in a lobby list even on a network that drops
    /// one of the two directions.
    /// </summary>
    public sealed class RoomAdvertisement : INetworkMessage {
        /// <summary>
        /// Gets or sets the identifier of the room. A browser uses it to recognise the same room across
        /// adverts, and a host uses it to ignore the echo of its own broadcast.
        /// </summary>
        public Guid RoomIdentifier { get; set; }
        
        /// <summary>Gets or sets the identifier of the hosting player.</summary>
        public Guid HostIdentifier { get; set; }
        
        /// <summary>
        /// Gets or sets the address the host believes it is reachable at. A browser ignores this hint and
        /// uses the source address of the datagram instead, because only the network knows which interface a
        /// packet actually left through.
        /// </summary>
        public string HostAddress { get; set; } = "";
        
        /// <summary>Gets or sets the TCP port at which the room accepts connections.</summary>
        public int Port { get; set; }
        
        /// <summary>Gets or sets the description of the room.</summary>
        public NetworkRoomInfo Info { get; set; } = new NetworkRoomInfo();
        
        /// <summary>
        /// Gets or sets the timestamp that was received in the probe this advert answers, or zero for an
        /// unsolicited advert. A browser measures the round trip from it.
        /// </summary>
        public long TimestampTicks { get; set; }
        
        /// <summary>Gets or sets a value indicating whether this advert answers a probe.</summary>
        public bool IsResponse { get; set; }
    }
    
    /// <summary>
    /// Asks every room on the local network to identify itself. Browsing sends it on its own schedule as a
    /// fallback for networks that filter broadcast traffic in one direction.
    /// </summary>
    public sealed class RoomProbeRequest : INetworkMessage {
        /// <summary>Gets or sets the identifier of the browsing peer.</summary>
        public Guid RequesterIdentifier { get; set; }
        
        /// <summary>Gets or sets the sender timestamp that the answer must echo.</summary>
        public long TimestampTicks { get; set; }
        
        /// <summary>Gets or sets the protocol version of the browsing peer.</summary>
        public int ProtocolVersion { get; set; }
    }
}
