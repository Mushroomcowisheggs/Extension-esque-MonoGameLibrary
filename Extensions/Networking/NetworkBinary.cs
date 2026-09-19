using System;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Provides the little-endian primitives used by the wire formats of this extension. They are kept
    /// separate from the service so that a payload written on one machine can always be read on another,
    /// whatever the endianness of either host.
    /// </summary>
    internal static class NetworkBinary {
        /// <summary>
        /// Writes a 32-bit signed integer in little-endian order.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset to write at.</param>
        /// <param name="value">The value to write.</param>
        internal static void WriteInt32(byte[] buffer, int offset, int value) {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
        
        /// <summary>
        /// Reads a 32-bit signed integer in little-endian order.
        /// </summary>
        /// <param name="buffer">The source buffer.</param>
        /// <param name="offset">The offset to read from.</param>
        /// <returns>The value that was read.</returns>
        internal static int ReadInt32(byte[] buffer, int offset) {
            int value = buffer[offset];
            value |= buffer[offset + 1] << 8;
            value |= buffer[offset + 2] << 16;
            value |= buffer[offset + 3] << 24;
            return value;
        }
    }
}
