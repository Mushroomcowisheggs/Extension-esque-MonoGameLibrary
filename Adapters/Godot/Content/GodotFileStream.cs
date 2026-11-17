using System;
using System.IO;
using FileAccess = global::Godot.FileAccess;

namespace MonoGameLibrary.Adapters.Godot.Content {
    /// <summary>
    /// A read-only <see cref="Stream"/> over a Godot <see cref="FileAccess"/> handle. Godot's
    /// file access is the only route that also reaches packed resources in an exported
    /// project, so <see cref="System.IO.File"/> is not used here.
    /// </summary>
    public sealed class GodotFileStream : Stream {
        private readonly FileAccess _file;
        private bool _flagDisposed;
        
        /// <summary>
        /// Wraps an open Godot file handle. The stream takes ownership of the handle.
        /// </summary>
        /// <param name="file">The open Godot file handle.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="file"/> is null.</exception>
        public GodotFileStream(FileAccess file) {
            if (file == null) { throw new ArgumentNullException(nameof(file)); }
            _file = file;
        }
        
        /// <inheritdoc />
        public override bool CanRead { get { return !_flagDisposed; } }
        
        /// <inheritdoc />
        public override bool CanSeek { get { return !_flagDisposed; } }
        
        /// <inheritdoc />
        public override bool CanWrite { get { return false; } }
        
        /// <inheritdoc />
        public override long Length {
            get {
                ThrowIfDisposed();
                return checked((long)_file.GetLength());
            }
        }
        
        /// <inheritdoc />
        public override long Position {
            get {
                ThrowIfDisposed();
                return checked((long)_file.GetPosition());
            }
            set {
                ThrowIfDisposed();
                if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _file.Seek(checked((ulong)value));
            }
        }
        
        /// <inheritdoc />
        public override void Flush() {
            ThrowIfDisposed();
        }
        
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) {
            ThrowIfDisposed();
            if (buffer == null) { throw new ArgumentNullException(nameof(buffer)); }
            if (offset < 0) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0) { throw new ArgumentOutOfRangeException(nameof(count)); }
            if (buffer.Length - offset < count) {
                throw new ArgumentException("The offset and count exceed the buffer length.");
            }
            if (_file.EofReached() || count == 0) {
                return 0;
            }
            byte[] chunk = _file.GetBuffer(count);
            int countRead = chunk.Length;
            if (countRead > 0) {
                Array.Copy(chunk, 0, buffer, offset, countRead);
            }
            return countRead;
        }
        
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) {
            ThrowIfDisposed();
            switch (origin) {
                case SeekOrigin.Begin:
                    if (offset < 0) { throw new ArgumentOutOfRangeException(nameof(offset)); }
                    _file.Seek(checked((ulong)offset));
                    break;
                case SeekOrigin.Current:
                    _file.Seek(checked((ulong)((long)_file.GetPosition() + offset)));
                    break;
                case SeekOrigin.End:
                    _file.SeekEnd(offset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
            return checked((long)_file.GetPosition());
        }
        
        /// <inheritdoc />
        public override void SetLength(long value) {
            throw new NotSupportedException("GodotFileStream is read-only.");
        }
        
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("GodotFileStream is read-only.");
        }
        
        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            if (disposing) {
                _file.Close();
            }
            base.Dispose(disposing);
        }
        
        private void ThrowIfDisposed() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(GodotFileStream));
            }
        }
    }
}
