using System;
using System.Collections.Generic;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral mesh geometry: vertices, indices, topology and bounds.
    /// </summary>
    public sealed class MeshData {
        private readonly MeshVertex[] _arrayVertices;
        private readonly int[] _arrayIndices;
        private readonly MeshPrimitiveType _typePrimitive;
        private readonly BoundingSphere _sphereBounds;
        
        /// <summary>
        /// Initializes mesh data.
        /// </summary>
        /// <param name="arrayVertices">The vertices.</param>
        /// <param name="arrayIndices">The indices.</param>
        /// <param name="typePrimitive">The primitive topology.</param>
        /// <exception cref="ArgumentNullException">Thrown when an array is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the arrays are empty or an index is out of range.</exception>
        public MeshData(
            MeshVertex[] vertexArray,
            int[] arrayIndices,
            MeshPrimitiveType typePrimitive
        ) {
            if (vertexArray == null) {
                throw new ArgumentNullException(nameof(vertexArray));
            }
            if (arrayIndices == null) {
                throw new ArgumentNullException(nameof(arrayIndices));
            }
            if (vertexArray.Length == 0) {
                throw new ArgumentException("A mesh needs at least one vertex.", nameof(vertexArray));
            }
            if (arrayIndices.Length == 0) {
                throw new ArgumentException("A mesh needs at least one index.", nameof(arrayIndices));
            }
            for (int index = 0; index < arrayIndices.Length; index += 1) {
                if (arrayIndices[index] < 0 || arrayIndices[index] >= vertexArray.Length) {
                    throw new ArgumentException("An index is outside the vertex array.", nameof(arrayIndices));
                }
            }
            _arrayVertices = vertexArray;
            _arrayIndices = arrayIndices;
            _typePrimitive = typePrimitive;
            _sphereBounds = CalculateBounds(vertexArray);
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
        
        /// <summary>Gets a conservative local-space bounding sphere.</summary>
        public BoundingSphere BoundsBoundingSphere {
            get { return _sphereBounds; }
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
        
        private static BoundingSphere CalculateBounds(MeshVertex[] vertexArray) {
            Vector3 center = Vector3.Zero;
            for (int index = 0; index < vertexArray.Length; index += 1) {
                center += vertexArray[index].Position;
            }
            center /= (float)vertexArray.Length;
            float radius = 0f;
            for (int index = 0; index < vertexArray.Length; index += 1) {
                float distance = Vector3.Distance(center, vertexArray[index].Position);
                if (distance > radius) {
                    radius = distance;
                }
            }
            return new BoundingSphere(center, radius);
        }
    }
}
