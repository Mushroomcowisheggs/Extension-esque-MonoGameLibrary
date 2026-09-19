namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Holds one entry of the peer table together with the sequence number that gives the roster a stable
    /// order. The sequence is what keeps a player list from reshuffling itself between frames.
    /// </summary>
    internal sealed class NetworkPeerRecord {
        /// <summary>The immutable snapshot handed to the game.</summary>
        internal NetworkPeer Peer;
        
        /// <summary>The order in which the peer became known; the local peer is always first.</summary>
        internal long Sequence;
    }
}
