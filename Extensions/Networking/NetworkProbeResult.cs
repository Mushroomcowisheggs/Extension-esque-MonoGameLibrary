using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// The outcome of asking a remote room for its status without joining it. It is the equivalent of the
    /// server list entry a player sees before connecting: reachability, latency, player count and version.
    /// </summary>
    public sealed class NetworkProbeResult {
        private readonly string _address;
        private readonly bool _flagReachable;
        private readonly bool _flagIsCompatible;
        private readonly int _latencyMilliseconds;
        private readonly NetworkRoomInfo _info;
        private readonly string _error;
        
        /// <summary>
        /// Initializes a new result.
        /// </summary>
        /// <param name="address">The probed endpoint in <c>host:port</c> form.</param>
        /// <param name="flagReachable">True when the endpoint answered the handshake.</param>
        /// <param name="flagIsCompatible">True when the endpoint speaks the same protocol version.</param>
        /// <param name="millisecondsLatency">The measured round-trip time, or -1 when the endpoint did not answer.</param>
        /// <param name="info">The room description the endpoint returned, or null when it did not answer.</param>
        /// <param name="error">A human readable explanation when the probe failed, otherwise an empty string.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is null.</exception>
        public NetworkProbeResult(string address, bool flagReachable, bool flagIsCompatible, int millisecondsLatency, NetworkRoomInfo info, string error) {
            if (address == null) {
                throw new ArgumentNullException(nameof(address));
            }
            _address = address;
            _flagReachable = flagReachable;
            _flagIsCompatible = flagIsCompatible;
            _latencyMilliseconds = millisecondsLatency;
            _info = info;
            if (error == null) {
                _error = "";
            } else {
                _error = error;
            }
        }
        
        /// <summary>
        /// Gets the probed endpoint in <c>host:port</c> form.
        /// </summary>
        public string Address {
            get {
                return _address;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the endpoint answered the handshake.
        /// </summary>
        public bool IsReachable {
            get {
                return _flagReachable;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the endpoint speaks the same protocol version as the local peer.
        /// </summary>
        public bool IsCompatible {
            get {
                return _flagIsCompatible;
            }
        }
        
        /// <summary>
        /// Gets the measured round-trip time in milliseconds, or -1 when the endpoint did not answer.
        /// </summary>
        public int LatencyMilliseconds {
            get {
                return _latencyMilliseconds;
            }
        }
        
        /// <summary>
        /// Gets the room description the endpoint returned, or null when it did not answer.
        /// </summary>
        public NetworkRoomInfo Info {
            get {
                return _info;
            }
        }
        
        /// <summary>
        /// Gets a human readable explanation when the probe failed, otherwise an empty string.
        /// </summary>
        public string Error {
            get {
                return _error;
            }
        }
    }
}
