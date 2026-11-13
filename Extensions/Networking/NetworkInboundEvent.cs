using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// One notification produced by a background loop and consumed by <see cref="NetworkService.Update"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A notification is immutable after construction, so the queue is the only cross-thread edge
    /// between a producing loop and the frame: the loop's writes to this object precede its enqueue,
    /// the frame's reads follow its dequeue, and there is no second field to publish. The payload the
    /// event carries is frozen the same way. <see cref="NetworkPeer"/> is constructed once per
    /// observation and exposes no setter, and a deserialized <see cref="INetworkMessage"/> belongs to
    /// one event and is not shared with the loop that produced it.
    /// </para>
    /// <para>
    /// Construction goes through the factory that names the kind, so a notification cannot carry a
    /// kind and a payload that disagree.
    /// </para>
    /// </remarks>
    internal sealed class NetworkInboundEvent {
        private readonly NetworkInboundKind _kind;
        private readonly NetworkStatus _statusPrevious;
        private readonly NetworkStatus _statusCurrent;
        private readonly string _reason;
        private readonly NetworkPeer _peer;
        private readonly INetworkMessage _message;
        
        private NetworkInboundEvent(
            NetworkInboundKind kind,
            NetworkStatus statusPrevious,
            NetworkStatus statusCurrent,
            string reason,
            NetworkPeer peer,
            INetworkMessage message
        ) {
            _kind = kind;
            _statusPrevious = statusPrevious;
            _statusCurrent = statusCurrent;
            _reason = reason;
            _peer = peer;
            _message = message;
        }
        
        /// <summary>
        /// Creates a notification for a session state transition.
        /// </summary>
        /// <param name="statusPrevious">The state the session left. </param>
        /// <param name="statusCurrent">The state the session entered. </param>
        /// <param name="reason">A human readable explanation, or null. </param>
        /// <returns>The notification. </returns>
        internal static NetworkInboundEvent CreateStatusChange(
            NetworkStatus statusPrevious,
            NetworkStatus statusCurrent,
            string reason
        ) {
            string reasonReported = reason;
            if (reasonReported == null) {
                reasonReported = "";
            }
            return new NetworkInboundEvent(
                NetworkInboundKind.StatusChangedHandler,
                statusPrevious,
                statusCurrent,
                reasonReported,
                null,
                null
            );
        }
        
        /// <summary>
        /// Creates a notification for a remote peer that joined.
        /// </summary>
        /// <param name="peer">The frozen peer snapshot. </param>
        /// <returns>The notification. </returns>
        internal static NetworkInboundEvent CreatePeerJoined(NetworkPeer peer) {
            return new NetworkInboundEvent(
                NetworkInboundKind.PeerJoinedHandler,
                NetworkStatus.Idle,
                NetworkStatus.Idle,
                "",
                peer,
                null
            );
        }
        
        /// <summary>
        /// Creates a notification for a remote peer that left.
        /// </summary>
        /// <param name="peer">The frozen peer snapshot. </param>
        /// <returns>The notification. </returns>
        internal static NetworkInboundEvent CreatePeerLeft(NetworkPeer peer) {
            return new NetworkInboundEvent(
                NetworkInboundKind.PeerLeftHandler,
                NetworkStatus.Idle,
                NetworkStatus.Idle,
                "",
                peer,
                null
            );
        }
        
        /// <summary>
        /// Creates a notification for a received application message.
        /// </summary>
        /// <param name="peer">The frozen peer snapshot the message came from. </param>
        /// <param name="message">The deserialized message, owned by this notification. </param>
        /// <returns>The notification. </returns>
        internal static NetworkInboundEvent CreateMessageReceived(NetworkPeer peer, INetworkMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            return new NetworkInboundEvent(
                NetworkInboundKind.Message,
                NetworkStatus.Idle,
                NetworkStatus.Idle,
                "",
                peer,
                message
            );
        }
        
        /// <summary>Gets the classification of this notification. </summary>
        internal NetworkInboundKind Kind {
            get { return _kind; }
        }
        
        /// <summary>Gets the state the session left, for a status change. </summary>
        internal NetworkStatus StatusPrevious {
            get { return _statusPrevious; }
        }
        
        /// <summary>Gets the state the session entered, for a status change. </summary>
        internal NetworkStatus CurrentStatus {
            get { return _statusCurrent; }
        }
        
        /// <summary>Gets the human readable explanation, or an empty string. </summary>
        internal string Reason {
            get { return _reason; }
        }
        
        /// <summary>Gets the peer the notification is about, when there is one. </summary>
        internal NetworkPeer Peer {
            get { return _peer; }
        }
        
        /// <summary>Gets the received application message, for a message notification. </summary>
        internal INetworkMessage Message {
            get { return _message; }
        }
    }
}
