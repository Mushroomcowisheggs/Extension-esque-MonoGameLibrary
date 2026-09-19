namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Describes the lifecycle state of a network session.
    /// </summary>
    public enum NetworkStatus {
        /// <summary>
        /// No session is active and none is being established.
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// A listener is open and the local peer accepts incoming connections.
        /// </summary>
        Hosting = 1,
        
        /// <summary>
        /// A connection attempt, including name resolution and the handshake, is in progress.
        /// </summary>
        Connecting = 2,
        
        /// <summary>
        /// The handshake completed and messages can be exchanged.
        /// </summary>
        Connected = 3,
        
        /// <summary>
        /// The session is being torn down and sockets are closing.
        /// </summary>
        Disconnecting = 4,
        
        /// <summary>
        /// The session ended in an orderly fashion, either locally or by the remote peer.
        /// </summary>
        Disconnected = 5,
        
        /// <summary>
        /// The session ended because of an error, such as a refused handshake or a lost socket.
        /// </summary>
        Failed = 6
    }
}
