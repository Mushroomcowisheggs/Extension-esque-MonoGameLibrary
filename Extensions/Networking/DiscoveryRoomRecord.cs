namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The browser-side state of one room that was seen on the local network.
    /// </summary>
    internal sealed class DiscoveryRoomRecord {
        /// <summary>The latest advert that was received for the room.</summary>
        internal RoomAdvertisement Advertisement;
        
        /// <summary>The address the advert came from.</summary>
        internal string HostAddress = "";
        
        /// <summary>The port at which the room accepts connections.</summary>
        internal int Port;
        
        /// <summary>The measured round-trip time, or -1 when only unsolicited adverts arrived.</summary>
        internal int LatencyMilliseconds = -1;
        
        /// <summary>The monotonic timestamp of the last advert, used for expiry.</summary>
        internal long TimestampSeenUtcTicks;
        
        /// <summary>The UTC timestamp of the last advert, used for display.</summary>
        
        /// <summary>The order in which the room was first seen, used to keep the list stable.</summary>
        internal long SeenTimestamp;
        
        /// <summary>The order in which the room was first seen, used to keep the list stable.</summary>
        internal long Sequence;
        
        /// <summary>True when the room speaks the protocol version of the local peer.</summary>
        internal bool FlagIsCompatible;
    }
}
