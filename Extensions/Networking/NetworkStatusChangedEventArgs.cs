using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Reports a transition of the session state, together with the reason when the transition was not
    /// requested by the local game.
    /// </summary>
    public sealed class NetworkStatusChangedEventArgs : EventArgs {
        private readonly NetworkStatus _statusPrevious;
        private readonly NetworkStatus _statusCurrent;
        private readonly string _reason;
        
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="statusPrevious">The state the session left.</param>
        /// <param name="statusCurrent">The state the session entered.</param>
        /// <param name="reason">A human readable explanation, or an empty string when none applies.</param>
        public NetworkStatusChangedEventArgs(NetworkStatus statusPrevious, NetworkStatus statusCurrent, string reason) {
            _statusPrevious = statusPrevious;
            _statusCurrent = statusCurrent;
            if (reason == null) {
                _reason = "";
            } else {
                _reason = reason;
            }
        }
        
        /// <summary>
        /// Gets the state the session left.
        /// </summary>
        public NetworkStatus StatusPrevious {
            get {
                return _statusPrevious;
            }
        }
        
        /// <summary>
        /// Gets the state the session entered.
        /// </summary>
        public NetworkStatus StatusCurrent {
            get {
                return _statusCurrent;
            }
        }
        
        /// <summary>
        /// Gets the human readable explanation of the transition, or an empty string when none applies.
        /// </summary>
        public string Reason {
            get {
                return _reason;
            }
        }
    }
}
