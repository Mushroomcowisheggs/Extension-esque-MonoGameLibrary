using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.MonoGame.ThreeDimensionalGraphics {
    /// <summary>
    /// MonoGame adapter for a mesh backed by vertex and index buffers.
    /// </summary>
    public sealed class MeshAdapter : IMesh {
        private VertexBuffer _bufferVertex;
        private IndexBuffer _bufferIndex;
        private readonly PrimitiveType _typePrimitive;
        private readonly int _offsetVertex;
        private readonly int _countVertex;
        private readonly int _indexStart;
        private readonly int _countPrimitive;
        private readonly BoundingSphere _sphereBounds;
        private readonly bool _flagOwnsBuffers;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a mesh adapter.
        /// </summary>
        /// <param name="bufferVertex">The vertex buffer.</param>
        /// <param name="bufferIndex">The index buffer.</param>
        /// <param name="typePrimitive">The primitive topology.</param>
        /// <param name="offsetVertex">The base vertex offset.</param>
        /// <param name="countVertex">The number of vertices used by the draw.</param>
        /// <param name="indexStart">The first index used by the draw.</param>
        /// <param name="countPrimitive">The primitive count.</param>
        /// <param name="sphereBounds">The local-space bounding sphere.</param>
        /// <param name="flagOwnsBuffers">Whether disposal also releases the buffers.</param>
        public MeshAdapter(
            VertexBuffer bufferVertex,
            IndexBuffer bufferIndex,
            PrimitiveType typePrimitive,
            int offsetVertex,
            int countVertex,
            int indexStart,
            int countPrimitive,
            BoundingSphere sphereBounds,
            bool flagOwnsBuffers = false
        ) {
            if (bufferVertex == null) {
                throw new ArgumentNullException(nameof(bufferVertex));
            }
            if (bufferIndex == null) {
                throw new ArgumentNullException(nameof(bufferIndex));
            }
            if (offsetVertex < 0) {
                throw new ArgumentOutOfRangeException(nameof(offsetVertex));
            }
            if (countVertex <= 0) {
                throw new ArgumentOutOfRangeException(nameof(countVertex));
            }
            if (indexStart < 0) {
                throw new ArgumentOutOfRangeException(nameof(indexStart));
            }
            if (countPrimitive <= 0) {
                throw new ArgumentOutOfRangeException(nameof(countPrimitive));
            }
            _bufferVertex = bufferVertex;
            _bufferIndex = bufferIndex;
            _typePrimitive = typePrimitive;
            _offsetVertex = offsetVertex;
            _countVertex = countVertex;
            _indexStart = indexStart;
            _countPrimitive = countPrimitive;
            _sphereBounds = sphereBounds;
            _flagOwnsBuffers = flagOwnsBuffers;
        }
        
        /// <inheritdoc />
        public int VertexCount { get { return _countVertex; } }
        
        /// <inheritdoc />
        public int IndexCount {
            get {
                if (_typePrimitive == PrimitiveType.TriangleList) {
                    return _countPrimitive * 3;
                }
                if (_typePrimitive == PrimitiveType.TriangleStrip) {
                    return _countPrimitive + 2;
                }
                if (_typePrimitive == PrimitiveType.LineList) {
                    return _countPrimitive * 2;
                }
                if (_typePrimitive == PrimitiveType.LineStrip) {
                    return _countPrimitive + 1;
                }
                return _countPrimitive;
            }
        }
        
        /// <inheritdoc />
        public int PrimitiveCount { get { return _countPrimitive; } }
        
        /// <inheritdoc />
        public BoundingSphere Bounds { get { return _sphereBounds; } }
        
        internal VertexBuffer NativeVertexBuffer {
            get {
                if (_bufferVertex == null) {
                    throw new ObjectDisposedException(nameof(MeshAdapter));
                }
                return _bufferVertex;
            }
        }
        
        internal IndexBuffer NativeIndexBuffer {
            get {
                if (_bufferIndex == null) {
                    throw new ObjectDisposedException(nameof(MeshAdapter));
                }
                return _bufferIndex;
            }
        }
        
        internal PrimitiveType NativePrimitiveType { get { return _typePrimitive; } }
        internal int NativeVertexOffset { get { return _offsetVertex; } }
        internal int NativeVertexCount { get { return _countVertex; } }
        internal int NativeIndexStart { get { return _indexStart; } }
        internal int NativePrimitiveCount { get { return _countPrimitive; } }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            if (_flagOwnsBuffers) {
                if (_bufferIndex != null) {
                    _bufferIndex.Dispose();
                }
                if (_bufferVertex != null) {
                    _bufferVertex.Dispose();
                }
            }
            _bufferIndex = null;
            _bufferVertex = null;
            GC.SuppressFinalize(this);
        }
    }
}
