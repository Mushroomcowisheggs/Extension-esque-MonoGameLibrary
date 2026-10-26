using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// A probe the endpoint answered but refused, for example because the room is full.
    /// </summary>
    public sealed class RefusedNetworkProbeResult : NetworkProbeResult {
        private readonly string _address;
        private readonly int _latencyMilliseconds;
        private readonly string _reason;
        
        /// <summary>
        /// Initializes a refused probe result.
        /// </summary>
        /// <param name="address">The probed endpoint in <c>host:port</c> form. </param>
        /// <param name="latencyMilliseconds">The measured round-trip time. </param>
        /// <param name="reason">The refusal reason supplied by the endpoint. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> or <paramref name="reason"/> is null. </exception>
        public RefusedNetworkProbeResult(string address, int latencyMilliseconds, string reason) {
            if (address == null) {
                throw new ArgumentNullException(nameof(address));
            }
            if (reason == null) {
                throw new ArgumentNullException(nameof(reason));
            }
            _address = address;
            _latencyMilliseconds = latencyMilliseconds;
            _reason = reason;
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
        /// Gets the refusal reason supplied by the endpoint.
        /// </summary>
        public string Reason {
            get { return _reason; }
        }
    }
}
