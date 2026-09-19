using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Tunes the behaviour of <see cref="INetworkService"/>. Every value has a working default, so a game
    /// only sets what it wants to change.
    /// </summary>
    /// <remarks>
    /// The defaults follow the behaviour players expect from a Minecraft-style session: a short connect
    /// budget, a handshake that refuses mismatched versions immediately, a frequent keep-alive that keeps
    /// the displayed latency fresh, and a reconnect attempt that starts again after a dropped link.
    /// </remarks>
    public sealed class NetworkOptions {
        /// <summary>
        /// Gets or sets the application name announced during the handshake.
        /// </summary>
        public string ApplicationName { get; set; } = "MonoGameLibrary Application";
        
        /// <summary>
        /// Gets or sets the application version announced during the handshake and shown in a room list.
        /// </summary>
        public string ApplicationVersion { get; set; } = "1.0";
        
        /// <summary>
        /// Gets or sets the protocol version this peer speaks. Peers that report a different value are
        /// refused before any application message is exchanged.
        /// </summary>
        public int ProtocolVersion { get; set; } = NetworkDefaults.ProtocolVersion;
        
        /// <summary>
        /// Gets or sets the number of remote peers a host accepts.
        /// </summary>
        public int MaxPeers { get; set; } = 8;
        
        /// <summary>
        /// Gets or sets the largest single message, in bytes, that is accepted from a remote peer.
        /// </summary>
        public int MaxMessageBytes { get; set; } = NetworkDefaults.MessageSizeLimit;
        
        /// <summary>
        /// Gets or sets the time budget of a connection attempt, including name resolution.
        /// </summary>
        public int ConnectTimeoutMilliseconds { get; set; } = 5000;
        
        /// <summary>
        /// Gets or sets the time budget of the handshake that follows a connection.
        /// </summary>
        public int HandshakeTimeoutMilliseconds { get; set; } = 5000;
        
        /// <summary>
        /// Gets or sets how often a round-trip probe is sent to every peer. The probe doubles as the
        /// keep-alive that proves a peer is still reachable.
        /// </summary>
        public int KeepAliveIntervalMilliseconds { get; set; } = 2000;
        
        /// <summary>
        /// Gets or sets how long a peer may stay silent before the session treats it as lost.
        /// </summary>
        public int IdleTimeoutMilliseconds { get; set; } = 15000;
        
        /// <summary>
        /// Gets or sets the largest number of received messages that a single update dispatches. The limit
        /// keeps a burst from a chatty peer from stretching one frame.
        /// </summary>
        public int MaxDispatchPerUpdate { get; set; } = 256;
        
        /// <summary>
        /// Gets or sets a value indicating whether a client reconnects automatically after an unexpected
        /// loss of the link. A disconnect requested by the game never triggers a reconnect.
        /// </summary>
        public bool EnableAutoReconnect { get; set; } = false;
        
        /// <summary>
        /// Gets or sets how many reconnect attempts follow one unexpected loss.
        /// </summary>
        public int MaxReconnectAttempts { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the delay before the first reconnect attempt and between consecutive attempts.
        /// </summary>
        public int ReconnectDelayMilliseconds { get; set; } = 1500;
        
        /// <summary>
        /// Verifies that every value can be used to open a session.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if a text value is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if a numeric value is outside its allowed range.</exception>
        public void Validate() {
            if (string.IsNullOrWhiteSpace(ApplicationName)) {
                throw new ArgumentException("An application name is required.", nameof(ApplicationName));
            }
            if (ApplicationVersion == null) {
                throw new ArgumentException("An application version is required.", nameof(ApplicationVersion));
            }
            if (ProtocolVersion < 1) {
                throw new ArgumentOutOfRangeException(nameof(ProtocolVersion), "The protocol version must be positive.");
            }
            if (MaxPeers < 1) {
                throw new ArgumentOutOfRangeException(nameof(MaxPeers), "At least one remote peer must be accepted.");
            }
            if (MaxMessageBytes < 64) {
                throw new ArgumentOutOfRangeException(nameof(MaxMessageBytes), "The message limit is too small to carry a control message.");
            }
            if (ConnectTimeoutMilliseconds < 1) {
                throw new ArgumentOutOfRangeException(nameof(ConnectTimeoutMilliseconds), "The connect budget must be positive.");
            }
            if (HandshakeTimeoutMilliseconds < 1) {
                throw new ArgumentOutOfRangeException(nameof(HandshakeTimeoutMilliseconds), "The handshake budget must be positive.");
            }
            if (KeepAliveIntervalMilliseconds < 100) {
                throw new ArgumentOutOfRangeException(nameof(KeepAliveIntervalMilliseconds), "The keep-alive interval must be at least 100 milliseconds.");
            }
            if (IdleTimeoutMilliseconds <= KeepAliveIntervalMilliseconds) {
                throw new ArgumentOutOfRangeException(nameof(IdleTimeoutMilliseconds), "The idle timeout must exceed the keep-alive interval.");
            }
            if (MaxDispatchPerUpdate < 1) {
                throw new ArgumentOutOfRangeException(nameof(MaxDispatchPerUpdate), "At least one message must be dispatched per update.");
            }
            if (MaxReconnectAttempts < 0) {
                throw new ArgumentOutOfRangeException(nameof(MaxReconnectAttempts), "The reconnect attempt count cannot be negative.");
            }
            if (ReconnectDelayMilliseconds < 0) {
                throw new ArgumentOutOfRangeException(nameof(ReconnectDelayMilliseconds), "The reconnect delay cannot be negative.");
            }
        }
    }
}
