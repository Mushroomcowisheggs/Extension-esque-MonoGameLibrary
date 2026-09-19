namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Classifies a change in the browsed room list.
    /// </summary>
    internal enum DiscoveryNotificationKind {
        /// <summary>A room appeared.</summary>
        Discovered = 0,
        
        /// <summary>The published description of a room changed.</summary>
        Updated = 1,
        
        /// <summary>A room stopped announcing itself.</summary>
        Lost = 2
    }
    
    /// <summary>
    /// One change in the browsed room list, produced by a background loop and reported by
    /// <see cref="DiscoveryService.Update"/> on the main thread.
    /// </summary>
    internal sealed class DiscoveryNotification {
        /// <summary>The classification of the change.</summary>
        internal DiscoveryNotificationKind Kind;
        
        /// <summary>The room the change is about.</summary>
        internal DiscoveredRoom Room;
    }
}
