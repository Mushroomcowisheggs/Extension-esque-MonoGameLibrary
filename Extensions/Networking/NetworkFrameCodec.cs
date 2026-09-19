namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Encodes and decodes the transport frame that wraps every serialized payload. A frame is a
    /// four-byte little-endian length followed by exactly that many payload bytes, which lets a receiver
    /// read one whole message at a time from a stream transport.
    /// </summary>
    internal static class NetworkFrameCodec {
        /// <summary>
        /// The number of bytes a frame header occupies.
        /// </summary>
        internal const int HeaderSize = 4;
        
        /// <summary>
        /// Wraps a payload in a length-prefixed frame.
        /// </summary>
        /// <param name="payload">The serialized message.</param>
        /// <returns>The framed bytes ready to be written to a channel.</returns>
        internal static byte[] Encode(byte[] payload) {
            byte[] frame = new byte[HeaderSize + payload.Length];
            NetworkBinary.WriteInt32(frame, 0, payload.Length);
            System.Buffer.BlockCopy(payload, 0, frame, HeaderSize, payload.Length);
            return frame;
        }
        
        /// <summary>
        /// Reads the payload length stored in a frame header.
        /// </summary>
        /// <param name="header">A buffer holding at least <see cref="HeaderSize"/> bytes.</param>
        /// <returns>The number of payload bytes that follow the header.</returns>
        internal static int DecodeLength(byte[] header) {
            return NetworkBinary.ReadInt32(header, 0);
        }
    }
}
