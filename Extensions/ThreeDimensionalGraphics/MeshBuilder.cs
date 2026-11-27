using System;
using System.Collections.Generic;
using System.Numerics;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Builds triangle-list mesh geometry from vertices and triangles in platform-neutral
    /// data. <see cref="AddTriangle"/> preserves the winding the caller supplies, while
    /// <see cref="AddOrientedTriangle"/> orients every triangle so its front face winds
    /// clockwise when viewed from the direction the vertex normals point.
    /// </summary>
    public sealed class MeshBuilder {
        private readonly List<MeshVertex> _listVertices = new List<MeshVertex>();
        private readonly List<int> _listIndices = new List<int>();
        
        /// <summary>Gets the number of vertices added so far.</summary>
        public int VertexCount {
            get { return _listVertices.Count; }
        }
        
        /// <summary>
        /// Adds a white vertex and returns its index.
        /// </summary>
        /// <param name="position">The local-space position.</param>
        /// <param name="normal">The local-space normal.</param>
        /// <param name="coordinateTexture">The texture coordinate.</param>
        /// <returns>The index of the added vertex.</returns>
        public int AddVertex(Vector3 position, Vector3 normal, Vector2 coordinateTexture) {
            return AddVertex(position, normal, coordinateTexture, Color.White);
        }
        
        /// <summary>
        /// Adds a vertex and returns its index.
        /// </summary>
        /// <param name="position">The local-space position.</param>
        /// <param name="normal">The local-space normal.</param>
        /// <param name="coordinateTexture">The texture coordinate.</param>
        /// <param name="color">The vertex color, multiplied into the material albedo.</param>
        /// <returns>The index of the added vertex.</returns>
        public int AddVertex(
            Vector3 position,
            Vector3 normal,
            Vector2 coordinateTexture,
            Color color
        ) {
            _listVertices.Add(new MeshVertex(position, normal, coordinateTexture, color));
            return _listVertices.Count - 1;
        }
        
        /// <summary>
        /// Adds one triangle from three vertex indices, preserving the supplied winding
        /// faithfully. Contract: the front face winds clockwise when viewed from the
        /// front, the shared convention of the supported adapter back ends; the caller
        /// owns the winding order, and a winding that contradicts the vertex normals is
        /// recorded as supplied, never corrected automatically. Use
        /// <see cref="AddOrientedTriangle"/> for normal-driven orientation.
        /// </summary>
        /// <param name="indexFirst">The first vertex index.</param>
        /// <param name="indexSecond">The second vertex index.</param>
        /// <param name="indexThird">The third vertex index.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an index is outside the vertex list.</exception>
        public void AddTriangle(int indexFirst, int indexSecond, int indexThird) {
            ThrowIfIndexOutOfRange(indexFirst);
            ThrowIfIndexOutOfRange(indexSecond);
            ThrowIfIndexOutOfRange(indexThird);
            _listIndices.Add(indexFirst);
            _listIndices.Add(indexSecond);
            _listIndices.Add(indexThird);
        }
        
        /// <summary>
        /// Adds one triangle from three vertex indices, orienting it by the vertex
        /// normals so the front face winds clockwise when viewed from the direction the
        /// normals point: a triangle whose supplied winding disagrees with the vertex
        /// normals is reversed.
        /// </summary>
        /// <param name="indexFirst">The first vertex index.</param>
        /// <param name="indexSecond">The second vertex index.</param>
        /// <param name="indexThird">The third vertex index.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an index is outside the vertex list.</exception>
        public void AddOrientedTriangle(int indexFirst, int indexSecond, int indexThird) {
            ThrowIfIndexOutOfRange(indexFirst);
            ThrowIfIndexOutOfRange(indexSecond);
            ThrowIfIndexOutOfRange(indexThird);
            MeshVertex vertexFirst = _listVertices[indexFirst];
            MeshVertex vertexSecond = _listVertices[indexSecond];
            MeshVertex vertexThird = _listVertices[indexThird];
            Vector3 directionEdge1 = vertexSecond.Position - vertexFirst.Position;
            Vector3 directionEdge2 = vertexThird.Position - vertexFirst.Position;
            Vector3 normalFace = Vector3.Cross(directionEdge1, directionEdge2);
            Vector3 normalSum =
                vertexFirst.Normal + vertexSecond.Normal + vertexThird.Normal;
            // A triangle whose winding disagrees with the vertex normals is reversed, so
            // every front face winds clockwise as the back ends expect.
            if (Vector3.Dot(normalFace, normalSum) < 0f) {
                _listIndices.Add(indexFirst);
                _listIndices.Add(indexThird);
                _listIndices.Add(indexSecond);
                return;
            }
            _listIndices.Add(indexFirst);
            _listIndices.Add(indexSecond);
            _listIndices.Add(indexThird);
        }
        
        /// <summary>
        /// Builds the accumulated geometry into immutable mesh data.
        /// </summary>
        /// <param name="material">The material, or null for the default material.</param>
        /// <returns>The built mesh data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no vertex or no triangle was added.</exception>
        public MeshData BuildMeshData(MeshMaterial? material) {
            if (_listVertices.Count == 0) {
                throw new InvalidOperationException("A mesh needs at least one vertex.");
            }
            if (_listIndices.Count == 0) {
                throw new InvalidOperationException("A mesh needs at least one triangle.");
            }
            return new MeshData(
                _listVertices.ToArray(),
                _listIndices.ToArray(),
                MeshPrimitiveType.TriangleList,
                material
            );
        }
        
        /// <summary>
        /// Builds the accumulated geometry into a mesh with the default material.
        /// </summary>
        /// <returns>The built mesh.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no vertex or no triangle was added.</exception>
        public Mesh BuildMesh() {
            return new Mesh(BuildMeshData(null));
        }
        
        private void ThrowIfIndexOutOfRange(int index) {
            if (index < 0 || index >= _listVertices.Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
