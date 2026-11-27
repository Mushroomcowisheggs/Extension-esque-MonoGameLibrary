using System;
using System.Numerics;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// One platform-neutral vertex: position, normal, texture coordinate and color.
    /// </summary>
    public readonly struct MeshVertex {
        /// <summary>
        /// Initializes a white vertex.
        /// </summary>
        /// <param name="position">The local-space position.</param>
        /// <param name="normal">The local-space normal.</param>
        /// <param name="coordinateTexture">The texture coordinate.</param>
        public MeshVertex(Vector3 position, Vector3 normal, Vector2 coordinateTexture)
            : this(position, normal, coordinateTexture, Color.White) {
        }
        
        /// <summary>
        /// Initializes a vertex with an explicit color.
        /// </summary>
        /// <param name="position">The local-space position.</param>
        /// <param name="normal">The local-space normal.</param>
        /// <param name="coordinateTexture">The texture coordinate.</param>
        /// <param name="color">The vertex color, multiplied into the material albedo.</param>
        public MeshVertex(Vector3 position, Vector3 normal, Vector2 coordinateTexture, Color color) {
            Position = position;
            Normal = normal;
            TextureCoordinate = coordinateTexture;
            Color = color;
        }
        
        /// <summary>Gets the local-space position.</summary>
        public Vector3 Position { get; }
        
        /// <summary>Gets the local-space normal.</summary>
        public Vector3 Normal { get; }
        
        /// <summary>Gets the texture coordinate.</summary>
        public Vector2 TextureCoordinate { get; }
        
        /// <summary>Gets the vertex color, multiplied into the material albedo.</summary>
        public Color Color { get; }
    }
}
