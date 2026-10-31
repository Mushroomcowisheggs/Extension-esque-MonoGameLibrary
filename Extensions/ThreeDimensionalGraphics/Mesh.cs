using System;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral mesh asset backed by immutable geometry data.
    /// </summary>
    public sealed class Mesh : IMesh {
        private MeshData _data;
        
        /// <summary>
        /// Initializes a mesh from geometry data.
        /// </summary>
        /// <param name="data">The mesh data.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        public Mesh(MeshData data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            _data = data;
        }
        
        /// <inheritdoc />
        public MeshData Data {
            get {
                if (_data == null) {
                    throw new ObjectDisposedException(nameof(Mesh));
                }
                return _data;
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            _data = null;
            GC.SuppressFinalize(this);
        }
    }
}
