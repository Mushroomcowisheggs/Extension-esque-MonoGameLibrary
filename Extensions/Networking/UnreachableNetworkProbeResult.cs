using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// A probe that could not reach the endpoint or could not read its answer.
    /// </summary>
    public sealed class UnreachableNetworkProbeResult : NetworkProbeResult {
        private readonly string _address;
        private readonly string _error;
        
        /// <summary>
        /// Initializes a failed probe result.
        /// </summary>
        /// <param name="address">The probed endpoint in <c>host:port</c> form. </param>
        /// <param name="error">The human readable explanation for the failure. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> or <paramref name="error"/> is null. </exception>
        public UnreachableNetworkProbeResult(string address, string error) {
            if (address == null) {
                throw new ArgumentNullException(nameof(address));
            }
            if (error == null) {
                throw new ArgumentNullException(nameof(error));
            }
            _address = address;
            _error = error;
        }
        
        /// <inheritdoc />
        public override string Address {
            get { return _address; }
        }
        
        /// <summary>
        /// Gets the human readable explanation for the failure.
        /// </summary>
        public string Error {
            get { return _error; }
        }
    }
}
