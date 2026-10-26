using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// A probe the endpoint answered with a different protocol version.
    /// </summary>
    public sealed class IncompatibleNetworkProbeResult : NetworkProbeResult {
        private readonly string _address;
        private readonly int _latencyMilliseconds;
        private readonly NetworkRoomInfo _info;
        private readonly string _error;
        
        /// <summary>
        /// Initializes an incompatible probe result.
        /// </summary>
        /// <param name="address">The probed endpoint in <c>host:port</c> form. </param>
        /// <param name="latencyMilliseconds">The measured round-trip time. </param>
        /// <param name="info">The room description the endpoint returned. </param>
        /// <param name="error">The incompatibility explanation. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/>, <paramref name="info"/> or <paramref name="error"/> is null. </exception>
        public IncompatibleNetworkProbeResult(string address, int latencyMilliseconds, NetworkRoomInfo info, string error) {
            if (address == null) {
                throw new ArgumentNullException(nameof(address));
            }
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            if (error == null) {
                throw new ArgumentNullException(nameof(error));
            }
            _address = address;
            _latencyMilliseconds = latencyMilliseconds;
            _info = info;
            _error = error;
        }
        
        /// <inheritdoc />
        public override string Address {
            get { return _address; }
        }
        
        /// <summary>
        /// Gets the measured round-trip time in milliseconds.
        /// </summary>
        public int MillisecondsLatency {
            get { return _latencyMilliseconds; }
        }
        
        /// <summary>
        /// Gets the room description the endpoint returned.
        /// </summary>
        public NetworkRoomInfo Info {
            get { return _info; }
        }
        
        /// <summary>
        /// Gets the incompatibility explanation.
        /// </summary>
        public string Error {
            get { return _error; }
        }
    }
}
