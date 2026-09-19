namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Converts <see cref="INetworkMessage"/> instances to and from the self-describing byte payload that
    /// <see cref="INetworkService"/> puts on the wire. A payload must carry the message kind so that a
    /// receiver can reconstruct the concrete type without out-of-band information.
    /// </summary>
    /// <remarks>
    /// Every implementation must also handle the library control messages declared in
    /// <see cref="NetworkSessionMessages"/>. <see cref="JsonNetworkSerializer"/> registers them itself.
    /// </remarks>
    public interface INetworkSerializer {
        /// <summary>
        /// Serializes a message into a self-describing payload.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialized payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
        /// <exception cref="NetworkProtocolException">Thrown if the message type is not registered.</exception>
        byte[] Serialize(INetworkMessage message);
        
        /// <summary>
        /// Attempts to reconstruct a message from a payload produced by <see cref="Serialize"/>.
        /// </summary>
        /// <param name="payload">The received payload.</param>
        /// <param name="kind">The kind identifier found in the payload, or an empty string when it cannot be read.</param>
        /// <param name="message">The reconstructed message, or null when the method returns <c>false</c>.</param>
        /// <returns><c>true</c> if the payload was understood; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method never throws for malformed or unknown payloads. It reports them through its return
        /// value so that a receiver can drop the payload and log a warning instead of tearing down a session.
        /// </remarks>
        bool TryDeserialize(byte[] payload, out string kind, out INetworkMessage message);
    }
}
