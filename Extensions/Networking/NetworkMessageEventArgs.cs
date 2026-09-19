using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Reports one application message that a remote peer sent.
    /// </summary>
    public sealed class NetworkMessageEventArgs : EventArgs {
        private readonly NetworkPeer _peerSender;
        private readonly INetworkMessage _message;
        
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="peerSender">The peer the message came from.</param>
        /// <param name="message">The message that was received.</param>
        /// <exception cref="ArgumentNullException">Thrown if an argument is null.</exception>
        public NetworkMessageEventArgs(NetworkPeer peerSender, INetworkMessage message) {
            if (peerSender == null) {
                throw new ArgumentNullException(nameof(peerSender));
            }
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            _peerSender = peerSender;
            _message = message;
        }
        
        /// <summary>
        /// Gets the peer the message came from. On a host, this identifies which client sent it; on a
        /// client, it is always the host.
        /// </summary>
        public NetworkPeer Peer {
            get {
                return _peerSender;
            }
        }
        
        /// <summary>
        /// Gets the message. Cast it to the application type that was registered with the serializer.
        /// </summary>
        public INetworkMessage Message {
            get {
                return _message;
            }
        }
    }
}
