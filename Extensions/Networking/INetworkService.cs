using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Manages one network session, either as the host that owns the authoritative state or as a client
    /// that joined a host. The service performs all socket work on background loops and reports everything
    /// through <see cref="Update"/>, so a game never blocks its frame on the network.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two ways of reaching a session are supported, mirroring the behaviour players know from Minecraft:
    /// <see cref="Host"/> opens a room on a known port, and <see cref="Connect"/> joins one by address and
    /// port. A room opened with <see cref="Host"/> can additionally be advertised on the local network with
    /// <see cref="IDiscoveryService"/>, which is the equivalent of opening a world to LAN.
    /// </para>
    /// <para>
    /// Message flow is deliberately simple: a client sends to the host, and a host broadcasts to every
    /// client. A game that needs finer routing includes the intended recipient inside its own message.
    /// </para>
    /// </remarks>
    public interface INetworkService : IDisposable {
        /// <summary>
        /// Gets the current state of the session.
        /// </summary>
        NetworkStatus Status { get; }
        
        /// <summary>
        /// Gets the role the local peer took when the current session started. The value stays available
        /// after the session ends so that a game can report what happened.
        /// </summary>
        NetworkRole Role { get; }
        
        /// <summary>
        /// Gets the port the local peer listens on while hosting, or the remote port while connected.
        /// Returns zero while no session exists.
        /// </summary>
        int Port { get; }
        
        /// <summary>
        /// Gets the identifier of the local peer, or <see cref="Guid.Empty"/> while no session exists.
        /// </summary>
        Guid LocalIdentifier { get; }
        
        /// <summary>
        /// Gets the display name the local peer announced, or an empty string while no session exists.
        /// </summary>
        string LocalDisplayName { get; }
        
        /// <summary>
        /// Gets the description of the room in use, or null while no session exists. Do not mutate the
        /// returned instance; it is the live description the service keeps updating.
        /// </summary>
        NetworkRoomInfo Info { get; }
        
        /// <summary>
        /// Gets a snapshot of every participant, including the local peer. The list is materialised on each
        /// call, so a game that needs it every frame should cache it.
        /// </summary>
        IReadOnlyList<NetworkPeer> Peers { get; }
        
        /// <summary>
        /// Raised for every application message received from a remote peer. Control messages that belong to
        /// the session itself are consumed by the service and never reach this event.
        /// </summary>
        event EventHandler<NetworkMessageEventArgs> MessageReceived;
        
        /// <summary>
        /// Raised when a remote peer joins the session.
        /// </summary>
        event EventHandler<NetworkPeerEventArgs> PeerJoined;
        
        /// <summary>
        /// Raised when a remote peer leaves the session, whether it disconnected or was lost.
        /// </summary>
        event EventHandler<NetworkPeerEventArgs> PeerLeft;
        
        /// <summary>
        /// Raised when <see cref="Status"/>, and possibly <see cref="Peers"/>, changed.
        /// </summary>
        event EventHandler<NetworkStatusChangedEventArgs> StatusChanged;
        
        /// <summary>
        /// Opens a room and starts accepting connections.
        /// </summary>
        /// <param name="port">The port to listen on, or zero to let the operating system choose one.</param>
        /// <param name="room">The description of the room that joining players will receive.</param>
        /// <param name="displayName">The name the host appears under in the roster.</param>
        /// <exception cref="ArgumentNullException">Thrown if an argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the room description or the display name is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a session already exists.</exception>
        void Host(int port, NetworkRoomInfo room, string displayName);
        
        /// <summary>
        /// Joins a room by address and port.
        /// </summary>
        /// <param name="addressHost">The host name or address literal of the room, for example <c>192.168.1.20</c>.</param>
        /// <param name="port">The port of the room.</param>
        /// <param name="displayName">The name the local peer appears under in the roster.</param>
        /// <exception cref="ArgumentNullException">Thrown if an argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the address or the display name is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is outside the range 1 to 65535.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a session already exists.</exception>
        void Connect(string addressHost, int port, string displayName);
        
        /// <summary>
        /// Closes the session and releases every socket. The method is safe to call while no session exists.
        /// </summary>
        /// <param name="reason">A human readable reason reported to remote peers and to the local game.</param>
        void Disconnect(string reason);
        
        /// <summary>
        /// Sends a message. A client sends to the host; a host broadcasts to every client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no session exists.</exception>
        /// <exception cref="NetworkProtocolException">Thrown if the message type is not registered with the serializer.</exception>
        void Send(INetworkMessage message);
        
        /// <summary>
        /// Sends a message to exactly one peer.
        /// </summary>
        /// <param name="identifierPeer">The identifier of the destination peer.</param>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no session exists.</exception>
        /// <exception cref="NetworkProtocolException">Thrown if the message type is not registered with the serializer.</exception>
        void SendTo(Guid identifierPeer, INetworkMessage message);
        
        /// <summary>
        /// Dispatches everything the background loops received since the previous call and raises the
        /// corresponding events. The host calls this once per frame through <see cref="NetworkModule"/>.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
    }
}
