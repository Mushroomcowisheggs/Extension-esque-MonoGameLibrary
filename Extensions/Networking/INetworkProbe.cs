using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Asks a remote room for its status without joining it, so that a game can show a reachable server
    /// list entry complete with name, player count and latency before the player commits to connecting.
    /// </summary>
    public interface INetworkProbe {
        /// <summary>
        /// Probes a remote endpoint.
        /// </summary>
        /// <param name="addressHost">The host name or address literal to reach.</param>
        /// <param name="port">The port to reach.</param>
        /// <param name="token">A token that cancels the probe.</param>
        /// <returns>The probe result. A failure to reach the endpoint is reported inside the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="addressHost"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="addressHost"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is outside the range 1 to 65535.</exception>
        Task<NetworkProbeResult> ProbeAsync(string addressHost, int port, CancellationToken token);
    }
}
