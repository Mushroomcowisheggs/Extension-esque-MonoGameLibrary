using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// An immutable snapshot of one room that was found on the local network.
    /// </summary>
    public sealed class DiscoveredRoom {
        private readonly Guid _identifier;
        private readonly string _addressHost;
        private readonly int _port;
        private readonly NetworkRoomInfo _info;
        private readonly int _millisecondsLatency;
        private readonly long _ticksTimestampSeenUtc;
        private readonly bool _flagIsCompatible;
        
        /// <summary>
        /// Initializes a new snapshot.
        /// </summary>
        /// <param name="identifier">The identifier the host put in its advert.</param>
        /// <param name="addressHost">The address the advert came from, as reported by the network stack.</param>
        /// <param name="port">The port at which the room accepts connections.</param>
        /// <param name="room">The description the host published.</param>
        /// <param name="millisecondsLatency">The measured round-trip time, or -1 when it is not known.</param>
        /// <param name="ticksTimestampSeenUtc">The UTC timestamp of the advert that produced this snapshot.</param>
        /// <param name="flagIsCompatible">True when the room speaks the same protocol version as the local peer.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="addressHost"/> or <paramref name="room"/> is null.</exception>
        public DiscoveredRoom(Guid identifier, string addressHost, int port, NetworkRoomInfo info, int millisecondsLatency, long ticksTimestampSeenUtc, bool flagIsCompatible) {
            if (addressHost == null) {
                throw new ArgumentNullException(nameof(addressHost));
            }
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            _identifier = identifier;
            _addressHost = addressHost;
            _port = port;
            _info = info;
            _millisecondsLatency = millisecondsLatency;
            _ticksTimestampSeenUtc = ticksTimestampSeenUtc;
            _flagIsCompatible = flagIsCompatible;
        }
        
        /// <summary>
        /// Gets the identifier of the room.
        /// </summary>
        public Guid Identifier {
            get {
                return _identifier;
            }
        }
        
        /// <summary>
        /// Gets the address the advert came from. It is taken from the datagram source rather than from the
        /// advert itself, so it is always a route the local machine can actually reach.
        /// </summary>
        public string HostAddress {
            get {
                return _addressHost;
            }
        }
        
        /// <summary>
        /// Gets the port at which the room accepts connections.
        /// </summary>
        public int Port {
            get {
                return _port;
            }
        }
        
        /// <summary>
        /// Gets the description the host published. The instance is replaced whenever the room publishes
        /// something new and is never mutated in place, so it can be held across frames.
        /// </summary>
        public NetworkRoomInfo Info {
            get {
                return _info;
            }
        }
        
        /// <summary>
        /// Gets the measured round-trip time in milliseconds, or -1 when the room only announced itself and
        /// never answered a probe.
        /// </summary>
        public int LatencyMilliseconds {
            get {
                return _millisecondsLatency;
            }
        }
        
        /// <summary>
        /// Gets the UTC timestamp, in ticks, of the advert that produced this snapshot.
        /// </summary>
        public long TimestampSeenUtcTicks {
            get {
                return _ticksTimestampSeenUtc;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the room speaks the same protocol version as the local peer.
        /// </summary>
        public bool IsCompatible {
            get {
                return _flagIsCompatible;
            }
        }
        
        /// <summary>
        /// Gets the endpoint of the room in <c>host:port</c> form, ready to be passed to
        /// <see cref="INetworkService.Connect"/>.
        /// </summary>
        public string Address {
            get {
                return _addressHost + ":" + _port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
