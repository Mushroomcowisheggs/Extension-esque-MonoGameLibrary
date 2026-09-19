namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Describes which side of a network session the local peer represents.
    /// </summary>
    public enum NetworkRole {
        /// <summary>
        /// No session exists; the service is idle.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// The local peer listens for incoming connections and owns the authoritative state.
        /// </summary>
        Host = 1,
        
        /// <summary>
        /// The local peer connected to a remote host.
        /// </summary>
        Client = 2
    }
}
