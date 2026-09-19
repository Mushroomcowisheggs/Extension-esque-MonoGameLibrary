using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// An immutable snapshot of one participant of a network session. Snapshots are produced by the
    /// service and handed to the game, so a game can hold one across frames without it changing.
    /// </summary>
    public sealed class NetworkPeer {
        private readonly Guid _identifier;
        private readonly string _nameDisplay;
        private readonly string _address;
        private readonly int _millisecondsLatency;
        private readonly bool _flagHost;
        private readonly bool _flagLocal;
        
        /// <summary>
        /// Initializes a new snapshot.
        /// </summary>
        /// <param name="identifier">The stable identifier of the peer.</param>
        /// <param name="nameDisplay">The display name announced during the handshake.</param>
        /// <param name="address">The remote endpoint in <c>host:port</c> form, or an empty string for the local peer.</param>
        /// <param name="millisecondsLatency">The measured round-trip time, or -1 when it has not been measured.</param>
        /// <param name="flagHost">True when the peer is the session host.</param>
        /// <param name="flagLocal">True when the peer is the local process.</param>
        public NetworkPeer(Guid identifier, string nameDisplay, string address, int millisecondsLatency, bool flagHost, bool flagLocal) {
            if (nameDisplay == null) {
                throw new ArgumentNullException(nameof(nameDisplay));
            }
            if (address == null) {
                throw new ArgumentNullException(nameof(address));
            }
            _identifier = identifier;
            _nameDisplay = nameDisplay;
            _address = address;
            _millisecondsLatency = millisecondsLatency;
            _flagHost = flagHost;
            _flagLocal = flagLocal;
        }
        
        /// <summary>
        /// Gets the stable identifier of the peer. The host generates it during the handshake.
        /// </summary>
        public Guid Identifier {
            get {
                return _identifier;
            }
        }
        
        /// <summary>
        /// Gets the display name the peer announced during the handshake.
        /// </summary>
        public string DisplayName {
            get {
                return _nameDisplay;
            }
        }
        
        /// <summary>
        /// Gets the remote endpoint in <c>host:port</c> form, or an empty string for the local peer. On a
        /// client, peers other than the host report the address of the host, because that is the only route the
        /// client has to reach them.
        /// </summary>
        public string Address {
            get {
                return _address;
            }
        }
        
        /// <summary>
        /// Gets the last measured round-trip time in milliseconds, or -1 when it is not known yet.
        /// </summary>
        public int LatencyMilliseconds {
            get {
                return _millisecondsLatency;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether this peer is the host of the session.
        /// </summary>
        public bool IsHost {
            get {
                return _flagHost;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether this peer is the local process.
        /// </summary>
        public bool IsLocal {
            get {
                return _flagLocal;
            }
        }
    }
}
