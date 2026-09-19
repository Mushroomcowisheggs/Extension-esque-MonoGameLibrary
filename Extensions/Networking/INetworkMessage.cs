namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Marks a type as a payload that can travel over an <see cref="INetworkService"/>.
    /// Implementations must be plain data objects with public settable properties or fields so that an
    /// <see cref="INetworkSerializer"/> can round-trip them without extra code.
    /// </summary>
    /// <remarks>
    /// Messages carry no routing information. The sender is always supplied by the service through
    /// <see cref="NetworkMessageEventArgs.Peer"/>, which keeps payload types free of transport concerns.
    /// </remarks>
    public interface INetworkMessage {
    }
}
