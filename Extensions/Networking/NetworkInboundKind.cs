namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Classifies the notifications that background loops hand to the main thread.
    /// </summary>
    internal enum NetworkInboundKind {
        /// <summary>A session state transition.</summary>
        StatusChangedHandler = 0,
        
        /// <summary>A remote peer joined.</summary>
        PeerJoinedHandler = 1,
        
        /// <summary>A remote peer left.</summary>
        PeerLeftHandler = 2,
        
        /// <summary>An application message arrived.</summary>
        Message = 3
    }
}
