namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// One notification produced by a background loop and consumed by <see cref="NetworkService.Update"/>.
    /// Instances are created off the main thread and are never mutated afterwards, so they need no locking.
    /// </summary>
    internal sealed class NetworkInboundEvent {
        /// <summary>The classification of this notification.</summary>
        internal NetworkInboundKind Kind;
        
        /// <summary>The state the session left, for a status change.</summary>
        internal NetworkStatus StatusPrevious;
        
        /// <summary>The state the session entered, for a status change.</summary>
        internal NetworkStatus CurrentStatus;
        
        /// <summary>A human readable explanation, or an empty string.</summary>
        internal string Reason = "";
        
        /// <summary>The peer the notification is about, when there is one.</summary>
        internal NetworkPeer Peer;
        
        /// <summary>The received application message, for a message notification.</summary>
        internal INetworkMessage Message;
    }
}
