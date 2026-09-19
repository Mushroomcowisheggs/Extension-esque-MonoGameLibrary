namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Exposes the constants that the networking extension uses when no explicit configuration is
    /// supplied. The values are deliberately unrelated to any third-party game so that a library host
    /// never collides with one.
    /// </summary>
    public static class NetworkDefaults {
        /// <summary>
        /// The default TCP port on which a room host accepts connections.
        /// </summary>
        public const int GamePort = 27700;
        
        /// <summary>
        /// The default UDP port on which rooms are announced and browsed on the local network.
        /// It is intentionally different from <see cref="GamePort"/> because it stays fixed for every
        /// host, while <see cref="GamePort"/> may differ per room.
        /// </summary>
        public const int DiscoveryPort = 27701;
        
        /// <summary>
        /// The default protocol version exchanged during the connection handshake. Peers that report a
        /// different value are refused before a single application message is accepted.
        /// </summary>
        public const int ProtocolVersion = 1;
        
        /// <summary>
        /// The default upper bound, in bytes, of a single serialized message.
        /// </summary>
        public const int MessageSizeLimit = 262144;
        
        /// <summary>
        /// The reserved kind prefix owned by the library itself. Application message kinds must not
        /// start with this prefix.
        /// </summary>
        public const string KindPrefix = "library.";
    }
}
