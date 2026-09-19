using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The wire representation of one participant of a session. It is exchanged inside
    /// <see cref="NetworkRosterMessage"/> and converted to <see cref="NetworkPeer"/> snapshots by the service.
    /// </summary>
    public sealed class NetworkPeerEntry {
        /// <summary>
        /// Gets or sets the stable identifier of the peer.
        /// </summary>
        public Guid Identifier { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the peer.
        /// </summary>
        public string DisplayName { get; set; } = "";
        
        /// <summary>
        /// Gets or sets a value indicating whether the peer is the session host.
        /// </summary>
        public bool IsHost { get; set; }
        
        /// <summary>
        /// Gets or sets the last measured round-trip time in milliseconds, or -1 when it is unknown.
        /// </summary>
        public int LatencyMilliseconds { get; set; } = -1;
    }
}
