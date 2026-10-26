using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// A probe the endpoint answered under the same protocol version.
    /// </summary>
    public sealed class CompatibleNetworkProbeResult : NetworkProbeResult {
        private readonly string _address;
        private readonly int _latencyMilliseconds;
        private readonly NetworkRoomInfo _info;
        
        /// <summary>
        /// Initializes a compatible probe result.
        /// </summary>
        /// <param name="address">The probed endpoint in <c>host:port</c> form. </param>
        /// <param name="latencyMilliseconds">The measured round-trip time. </param>
        /// <param name="info">The room description the endpoint returned. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> or <paramref name="info"/> is null. </exception>
        public CompatibleNetworkProbeResult(string address, int latencyMilliseconds, NetworkRoomInfo info) {
            if (address == null) {
                throw new ArgumentNullException(nameof(address));
            }
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            _address = address;
            _latencyMilliseconds = latencyMilliseconds;
            _info = info;
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
    }
}
