using System;
using System.Collections.Generic;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral mesh geometry: vertices, indices, topology, an optional material
    /// and bounds. Front faces wind clockwise when viewed from the front, the shared
    /// convention of the supported adapter back ends; a back end with a different
    /// convention converts at upload time.
    /// </summary>
    public sealed class MeshData {
        private readonly MeshVertex[] _arrayVertices;
        private readonly int[] _arrayIndices;
        private readonly MeshPrimitiveType _typePrimitive;
        private readonly BoundingSphere _sphereBounds;
        private readonly Aabb _aabbBounds;
        private readonly MeshMaterial? _material;
        
        /// <summary>
        /// Initializes mesh data with the default material.
        /// </summary>
        /// <param name="arrayVertices">The vertices.</param>
        /// <param name="arrayIndices">The indices.</param>
        /// <param name="typePrimitive">The primitive topology.</param>
        /// <exception cref="ArgumentNullException">Thrown when an array is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the arrays are empty or an index is out of range.</exception>
        public MeshData(
            MeshVertex[] arrayVertices,
            int[] arrayIndices,
            MeshPrimitiveType typePrimitive
        ) : this(arrayVertices, arrayIndices, typePrimitive, null) {
        }
        
        /// <summary>
        /// Initializes mesh data with an optional material.
        /// </summary>
        /// <param name="arrayVertices">The vertices.</param>
        /// <param name="arrayIndices">The indices.</param>
        /// <param name="typePrimitive">The primitive topology.</param>
        /// <param name="material">The material, or null for the default material.</param>
        /// <exception cref="ArgumentNullException">Thrown when an array is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the arrays are empty or an index is out of range.</exception>
        public MeshData(
            MeshVertex[] arrayVertices,
            int[] arrayIndices,
            MeshPrimitiveType typePrimitive,
            MeshMaterial? material
        ) {
            if (arrayVertices == null) {
                throw new ArgumentNullException(nameof(arrayVertices));
            }
            if (arrayIndices == null) {
                throw new ArgumentNullException(nameof(arrayIndices));
            }
            if (arrayVertices.Length == 0) {
                throw new ArgumentException("A mesh needs at least one vertex.", nameof(arrayVertices));
            }
            if (arrayIndices.Length == 0) {
                throw new ArgumentException("A mesh needs at least one index.", nameof(arrayIndices));
            }
            for (int index = 0; index < arrayIndices.Length; index += 1) {
                if (arrayIndices[index] < 0 || arrayIndices[index] >= arrayVertices.Length) {
                    throw new ArgumentException("An index is outside the vertex array.", nameof(arrayIndices));
                }
            }
            _arrayVertices = arrayVertices;
            _arrayIndices = arrayIndices;
            _typePrimitive = typePrimitive;
            _material = material;
            _sphereBounds = CalculateBounds(arrayVertices);
            _aabbBounds = CalculateBoundsAabb(arrayVertices);
        }
        
        /// <summary>Gets the vertex data.</summary>
        public IReadOnlyList<MeshVertex> Vertices {
            get { return _arrayVertices; }
        }
        
        /// <summary>Gets the index data.</summary>
        public IReadOnlyList<int> Indices {
            get { return _arrayIndices; }
        }
        
        /// <summary>Gets the primitive topology.</summary>
        public MeshPrimitiveType PrimitiveType {
            get { return _typePrimitive; }
        }
        
        /// <summary>
        /// Gets the exact-enclosing local-space bounding sphere of the vertices. The
        /// vertex positions are always readable, so this sphere encloses the geometry
        /// with an exact known size; exact dimensions are also available through
        /// <see cref="BoundsAabb"/>.
        /// </summary>
        public BoundingSphere BoundsBoundingSphere {
            get { return _sphereBounds; }
        }
        
        /// <summary>Gets the exact local-space axis-aligned bounding box of the vertices.</summary>
        public Aabb BoundsAabb {
            get { return _aabbBounds; }
        }
        
        /// <summary>Gets the material, or null for the default material.</summary>
        public MeshMaterial? Material {
            get { return _material; }
        }
        
        /// <summary>Gets the vertex count.</summary>
        public int VertexCount {
            get { return _arrayVertices.Length; }
        }
        
        /// <summary>Gets the index count.</summary>
        public int IndexCount {
            get { return _arrayIndices.Length; }
        }
        
        /// <summary>Gets the primitive count for the declared topology.</summary>
        public int PrimitiveCount {
            get {
                if (_typePrimitive == MeshPrimitiveType.TriangleList) {
                    return _arrayIndices.Length / 3;
                }
                if (_typePrimitive == MeshPrimitiveType.TriangleStrip) {
                    if (_arrayIndices.Length < 3) {
                        return 0;
                    }
                    return _arrayIndices.Length - 2;
                }
                if (_typePrimitive == MeshPrimitiveType.LineList) {
                    return _arrayIndices.Length / 2;
                }
                if (_arrayIndices.Length < 2) {
                    return 0;
                }
                return _arrayIndices.Length - 1;
            }
        }
        
        private static BoundingSphere CalculateBounds(MeshVertex[] arrayVertices) {
            Vector3 center = Vector3.Zero;
            for (int index = 0; index < arrayVertices.Length; index += 1) {
                center += arrayVertices[index].Position;
            }
            center /= (float)arrayVertices.Length;
            float radius = 0f;
            for (int index = 0; index < arrayVertices.Length; index += 1) {
                float distance = Vector3.Distance(center, arrayVertices[index].Position);
                if (distance > radius) {
                    radius = distance;
                }
            }
            return new BoundingSphere(center, radius);
        }
        
        private static Aabb CalculateBoundsAabb(MeshVertex[] arrayVertices) {
            Vector3 minimum = arrayVertices[0].Position;
            Vector3 maximum = arrayVertices[0].Position;
            for (int index = 1; index < arrayVertices.Length; index += 1) {
                Vector3 position = arrayVertices[index].Position;
                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }
            return new Aabb(minimum, maximum);
        }
    }
}
