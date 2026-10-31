using System;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// One platform-neutral vertex: position, normal and texture coordinate.
    /// </summary>
    public readonly struct MeshVertex {
        /// <summary>
        /// Initializes a vertex.
        /// </summary>
        /// <param name="position">The local-space position.</param>
        /// <param name="normal">The local-space normal.</param>
        /// <param name="textureCoordinate">The texture coordinate.</param>
        public MeshVertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate) {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
        }
        
        /// <summary>Gets the local-space position.</summary>
        public Vector3 Position { get; }
        
        /// <summary>Gets the local-space normal.</summary>
        public Vector3 Normal { get; }
        
        /// <summary>Gets the texture coordinate.</summary>
        public Vector2 TextureCoordinate { get; }
    }
}
