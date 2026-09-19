using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Finds and publishes rooms on the local network, which is the equivalent of Minecraft's
    /// "scanning for games on your local network" list and its "open to LAN" switch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A host calls <see cref="StartAnnouncing"/> so that its room appears in the list of every player on the
    /// same network, and keeps the published description current with <see cref="UpdateRoomInfo"/> as players
    /// come and go. A player who wants to join something calls <see cref="StartBrowsing"/> and reads
    /// <see cref="Rooms"/>, or waits for the events.
    /// </para>
    /// <para>
    /// Announcements and probes travel as datagrams, so neither direction is guaranteed. A room therefore
    /// announces itself on its own schedule and also answers every probe it receives, and a browser both
    /// listens and probes. An entry that stops arriving is dropped from the list, which is what keeps closed
    /// rooms from lingering.
    /// </para>
    /// </remarks>
    public interface IDiscoveryService : IDisposable {
        /// <summary>
        /// Gets a value indicating whether the service is currently looking for rooms.
        /// </summary>
        bool IsBrowsing { get; }
        
        /// <summary>
        /// Gets a value indicating whether the service is currently publishing a room.
        /// </summary>
        bool IsAnnouncing { get; }
        
        /// <summary>
        /// Gets the rooms that are currently visible, ordered by the moment they were first seen. The list is
        /// materialised on each call, so a game that draws it every frame should cache it between updates.
        /// </summary>
        IReadOnlyList<DiscoveredRoom> Rooms { get; }
        
        /// <summary>
        /// Raised when a room appears in the list.
        /// </summary>
        event EventHandler<DiscoveredRoomEventArgs> RoomDiscovered;
        
        /// <summary>
        /// Raised when the published description of a known room changes, for example because a player joined
        /// or left it. A change of measured latency does not raise this event; read <see cref="Rooms"/> instead.
        /// </summary>
        event EventHandler<DiscoveredRoomEventArgs> RoomUpdated;
        
        /// <summary>
        /// Raised when a room stops announcing itself and is dropped from the list.
        /// </summary>
        event EventHandler<DiscoveredRoomEventArgs> RoomLost;
        
        /// <summary>
        /// Starts looking for rooms and keeps the list fresh until <see cref="StopBrowsing"/> is called.
        /// </summary>
        /// <exception cref="NetworkProtocolException">Thrown if the discovery port cannot be used.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the service has been disposed.</exception>
        void StartBrowsing();
        
        /// <summary>
        /// Stops looking for rooms. The rooms that were found remain available until
        /// <see cref="ClearRooms"/> is called.
        /// </summary>
        void StopBrowsing();
        
        /// <summary>
        /// Empties the list of found rooms without raising <see cref="RoomLost"/>.
        /// </summary>
        void ClearRooms();
        
        /// <summary>
        /// Starts publishing a room on the local network and answering probes for it.
        /// </summary>
        /// <param name="room">The description to publish. It is copied, so later changes to the caller's instance have no effect.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="room"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the description is invalid.</exception>
        /// <exception cref="NetworkProtocolException">Thrown if the discovery port cannot be used.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the service has been disposed.</exception>
        void StartAnnouncing(NetworkRoomInfo room);
        
        /// <summary>
        /// Replaces the published description, which is how a host reports a changed player count.
        /// </summary>
        /// <param name="room">The new description.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="room"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no room is being announced.</exception>
        void UpdateRoomInfo(NetworkRoomInfo room);
        
        /// <summary>
        /// Stops publishing the room, so that it disappears from every list within one expiry interval.
        /// </summary>
        void StopAnnouncing();
        
        /// <summary>
        /// Dispatches everything the background loops received since the previous call, expires stale entries
        /// and raises the corresponding events. The host calls this once per frame through
        /// <see cref="DiscoveryModule"/>.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
    }
}
