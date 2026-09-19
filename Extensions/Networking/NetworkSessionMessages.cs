using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Declares the control messages the library itself exchanges, together with their reserved kinds.
    /// A game never needs to send these; <see cref="INetworkService"/> produces them automatically.
    /// </summary>
    public static class NetworkSessionMessages {
        /// <summary>The kind of <see cref="NetworkHelloMessage"/>.</summary>
        public const string HelloKind = NetworkDefaults.KindPrefix + "hello";
        
        /// <summary>The kind of <see cref="NetworkWelcomeMessage"/>.</summary>
        public const string WelcomeKind = NetworkDefaults.KindPrefix + "welcome";
        
        /// <summary>The kind of <see cref="NetworkRefusedMessage"/>.</summary>
        public const string RefusedKind = NetworkDefaults.KindPrefix + "refused";
        
        /// <summary>The kind of <see cref="NetworkRosterMessage"/>.</summary>
        public const string RosterKind = NetworkDefaults.KindPrefix + "roster";
        
        /// <summary>The kind of <see cref="NetworkPingMessage"/>.</summary>
        public const string PingKind = NetworkDefaults.KindPrefix + "ping";
        
        /// <summary>The kind of <see cref="NetworkPongMessage"/>.</summary>
        public const string PongKind = NetworkDefaults.KindPrefix + "pong";
        
        /// <summary>The kind of <see cref="NetworkDisconnectMessage"/>.</summary>
        public const string DisconnectKind = NetworkDefaults.KindPrefix + "disconnect";
        
        /// <summary>
        /// Determines whether a kind identifier belongs to the reserved control range.
        /// </summary>
        /// <param name="kind">The kind identifier to test.</param>
        /// <returns><c>true</c> when the kind is owned by the library; otherwise <c>false</c>.</returns>
        public static bool IsControlKind(string kind) {
            if (string.IsNullOrEmpty(kind)) {
                return false;
            }
            return kind.StartsWith(NetworkDefaults.KindPrefix, StringComparison.Ordinal);
        }
    }
    
    /// <summary>
    /// The first message a client sends after a socket is established.
    /// </summary>
    public sealed class NetworkHelloMessage : INetworkMessage {
        /// <summary>Gets or sets the protocol version of the sender.</summary>
        public int ProtocolVersion { get; set; }
        
        /// <summary>Gets or sets the name of the sending application.</summary>
        public string ApplicationName { get; set; } = "";
        
        /// <summary>Gets or sets the version of the sending application.</summary>
        public string ApplicationVersion { get; set; } = "";
        
        /// <summary>Gets or sets the display name the sender wants to appear under.</summary>
        public string DisplayName { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the identifier the sender generated for itself. The host adopts it, so a client that
        /// reconnects after a brief loss is recognised as the same player instead of a new one.
        /// </summary>
        public Guid Identifier { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating that the sender only asks for the room description and does not want
        /// to become a player. A host answers such a handshake and then closes the link without touching its
        /// roster, so a status check costs neither a player slot nor a join event.
        /// </summary>
        public bool IsProbe { get; set; }
    }
    
    /// <summary>
    /// The answer a host sends when it accepts a client. It carries the room description the client
    /// should display and the identifier the host assigned to the client.
    /// </summary>
    public sealed class NetworkWelcomeMessage : INetworkMessage {
        /// <summary>Gets or sets the protocol version of the host.</summary>
        public int ProtocolVersion { get; set; }
        
        /// <summary>Gets or sets the identifier the host assigned to the joining peer.</summary>
        public Guid PeerIdentifier { get; set; }
        
        /// <summary>Gets or sets the identifier of the host.</summary>
        public Guid HostIdentifier { get; set; }
        
        /// <summary>Gets or sets the description of the joined room.</summary>
        public NetworkRoomInfo Info { get; set; } = new NetworkRoomInfo();
        
        /// <summary>Gets or sets the roster as it stands after the join, including the joining peer.</summary>
        public List<NetworkPeerEntry> Peers { get; set; } = new List<NetworkPeerEntry>();
    }
    
    /// <summary>
    /// The answer a host sends when it refuses a client, for example because the room is full or the
    /// protocol version differs.
    /// </summary>
    public sealed class NetworkRefusedMessage : INetworkMessage {
        /// <summary>Gets or sets the human readable reason for the refusal.</summary>
        public string Reason { get; set; } = "";
    }
    
    /// <summary>
    /// The authoritative list of participants. The host sends it whenever the roster changes, and a
    /// receiver derives peer-joined and peer-left notifications by comparing it with its previous view.
    /// </summary>
    public sealed class NetworkRosterMessage : INetworkMessage {
        /// <summary>Gets or sets the participants of the session.</summary>
        public List<NetworkPeerEntry> Peers { get; set; } = new List<NetworkPeerEntry>();
    }
    
    /// <summary>
    /// A round-trip probe. Either side may send it; the receiver always answers with
    /// <see cref="NetworkPongMessage"/>, which also serves as the session keep-alive.
    /// </summary>
    public sealed class NetworkPingMessage : INetworkMessage {
        /// <summary>Gets or sets the sender timestamp, echoed unchanged by the receiver.</summary>
        public long TimestampTicks { get; set; }
    }
    
    /// <summary>
    /// The answer to <see cref="NetworkPingMessage"/>.
    /// </summary>
    public sealed class NetworkPongMessage : INetworkMessage {
        /// <summary>Gets or sets the timestamp that was received in the ping.</summary>
        public long TimestampTicks { get; set; }
    }
    
    /// <summary>
    /// Announces an orderly shutdown so that the remote peer can report a reason instead of a lost socket.
    /// </summary>
    public sealed class NetworkDisconnectMessage : INetworkMessage {
        /// <summary>Gets or sets the human readable reason for the shutdown.</summary>
        public string Reason { get; set; } = "";
    }
}
