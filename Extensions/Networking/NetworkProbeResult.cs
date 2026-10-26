namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Base class for the outcome of asking a remote room for its status without joining it.
    /// </summary>
    public abstract class NetworkProbeResult {
        /// <summary>
        /// Gets the probed endpoint in <c>host:port</c> form.
        /// </summary>
        public abstract string Address { get; }
    }
}
