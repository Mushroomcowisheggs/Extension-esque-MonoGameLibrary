using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Reports that a peer joined or left the session.
    /// </summary>
    public sealed class NetworkPeerEventArgs : EventArgs {
        private readonly NetworkPeer _peer;
        
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="peer">The peer the event is about.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="peer"/> is null.</exception>
        public NetworkPeerEventArgs(NetworkPeer peer) {
            if (peer == null) {
                throw new ArgumentNullException(nameof(peer));
            }
            _peer = peer;
        }
        
        /// <summary>
        /// Gets the peer the event is about.
        /// </summary>
        public NetworkPeer Peer {
            get {
                return _peer;
            }
        }
    }
}
