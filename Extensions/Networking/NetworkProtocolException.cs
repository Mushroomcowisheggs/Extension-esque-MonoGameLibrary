using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Reports a violation of the networking protocol, such as an unregistered message type, an
    /// unsupported protocol version, or a malformed frame.
    /// </summary>
    public sealed class NetworkProtocolException : Exception {
        /// <summary>
        /// Initializes a new instance with a message.
        /// </summary>
        /// <param name="message">The description of the protocol violation.</param>
        public NetworkProtocolException(string message) : base(message) {
        }
        
        /// <summary>
        /// Initializes a new instance with a message and an inner exception.
        /// </summary>
        /// <param name="message">The description of the protocol violation.</param>
        /// <param name="exceptionInner">The exception that caused the violation.</param>
        public NetworkProtocolException(string message, Exception exceptionInner) : base(message, exceptionInner) {
        }
    }
}
