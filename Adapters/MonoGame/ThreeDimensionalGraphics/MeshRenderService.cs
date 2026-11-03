using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.MonoGame.ThreeDimensionalGraphics {
    /// <summary>
    /// MonoGame mesh renderer using BasicEffect for neutral mesh data and the native model draw
    /// path for compiled Model assets.
    /// </summary>
    public sealed class MeshRenderService : IMeshRenderService {
        private readonly GraphicsDevice _deviceGraphics;
        private readonly IThreeDimensionalCameraService _serviceCamera;
        private readonly BasicEffect _effectBasic;
        private readonly ThreadAccess _accessThread = new ThreadAccess();
        private readonly ThreeDimensionalRenderQueue _queueRender = new ThreeDimensionalRenderQueue();
        private readonly Dictionary<IMesh, GpuMesh> _dictionaryMeshes = new Dictionary<IMesh, GpuMesh>();
        private readonly object _lock = new object();
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes the renderer.
        /// </summary>
        /// <param name="deviceGraphics">The graphics device used for drawing.</param>
        /// <param name="serviceCamera">The camera supplying view and projection matrices.</param>
        public MeshRenderService(
            GraphicsDevice deviceGraphics,
            IThreeDimensionalCameraService serviceCamera
        ) {
            if (deviceGraphics == null) {
                throw new ArgumentNullException(nameof(deviceGraphics));
            }
            if (serviceCamera == null) {
                throw new ArgumentNullException(nameof(serviceCamera));
            }
            _deviceGraphics = deviceGraphics;
            _serviceCamera = serviceCamera;
            _effectBasic = new BasicEffect(deviceGraphics);
            _effectBasic.LightingEnabled = true;
            _effectBasic.EnableDefaultLighting();
            _effectBasic.VertexColorEnabled = false;
            _effectBasic.TextureEnabled = false;
        }
        
        /// <inheritdoc />
        public void Submit(IMesh mesh, Matrix4x4 matrixWorld) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            lock (_lock) {
                ThrowIfDisposedLocked();
                _queueRender.Submit(mesh, matrixWorld);
            }
        }
        
        /// <inheritdoc />
        public void Submit(IModel model, Matrix4x4 matrixWorld) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            lock (_lock) {
                ThrowIfDisposedLocked();
                _queueRender.Submit(model, matrixWorld);
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            _accessThread.VerifyAccess();
            lock (_lock) {
                ThrowIfDisposedLocked();
            }
            ThreeDimensionalCameraState stateCamera = _serviceCamera.CameraState;
            Matrix4x4 matrixViewProjection = stateCamera.ViewMatrix * stateCamera.ProjectionMatrix;
            List<ThreeDimensionalSubmission> listSubmissions = _queueRender.TakeVisible(matrixViewProjection);
            if (listSubmissions.Count == 0) {
                return;
            }
            
            Matrix matrixViewNative = ToNative(stateCamera.ViewMatrix);
            Matrix matrixProjectionNative = ToNative(stateCamera.ProjectionMatrix);
            BlendState stateBlend = _deviceGraphics.BlendState;
            DepthStencilState stateDepth = _deviceGraphics.DepthStencilState;
            RasterizerState stateRasterizer = _deviceGraphics.RasterizerState;
            IndexBuffer bufferIndex = _deviceGraphics.Indices;
            try {
                _deviceGraphics.BlendState = BlendState.Opaque;
                _deviceGraphics.DepthStencilState = DepthStencilState.Default;
                _deviceGraphics.RasterizerState = RasterizerState.CullCounterClockwise;
                foreach (ThreeDimensionalSubmission submission in listSubmissions) {
                    if (submission.FlagModel) {
                        DrawModel(submission.Model, submission.WorldMatrix, matrixViewNative, matrixProjectionNative);
                    } else {
                        DrawMesh(submission.Mesh, submission.WorldMatrix, matrixViewNative, matrixProjectionNative);
                    }
                }
            } finally {
                // MonoGame exposes no vertex-buffer readback. The pass leaves the last mesh binding
                // in place; every following pass binds its own buffers.
                _deviceGraphics.Indices = bufferIndex;
                _deviceGraphics.BlendState = stateBlend;
                _deviceGraphics.DepthStencilState = stateDepth;
                _deviceGraphics.RasterizerState = stateRasterizer;
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                _queueRender.Clear();
                foreach (GpuMesh meshGpu in _dictionaryMeshes.Values) {
                    meshGpu.Dispose();
                }
                _dictionaryMeshes.Clear();
            }
            _effectBasic.Dispose();
            GC.SuppressFinalize(this);
        }
        
        private void DrawMesh(
            IMesh mesh,
            Matrix4x4 matrixWorld,
            Matrix matrixViewNative,
            Matrix matrixProjectionNative
        ) {
            GpuMesh meshGpu = GetOrCreateGpuMesh(mesh);
            _deviceGraphics.SetVertexBuffer(meshGpu.VertexBuffer);
            _deviceGraphics.Indices = meshGpu.IndexBuffer;
            _effectBasic.World = ToNative(matrixWorld);
            _effectBasic.View = matrixViewNative;
            _effectBasic.Projection = matrixProjectionNative;
            foreach (EffectPass pass in _effectBasic.CurrentTechnique.Passes) {
                pass.Apply();
                _deviceGraphics.DrawIndexedPrimitives(
                    meshGpu.PrimitiveType,
                    0,
                    0,
                    meshGpu.VertexCount,
                    0,
                    meshGpu.PrimitiveCount
                );
            }
        }
        
        private void DrawModel(
            IModel model,
            Matrix4x4 matrixWorld,
            Matrix matrixViewNative,
            Matrix matrixProjectionNative
        ) {
            AdapterModel modelAdapter = model as AdapterModel;
            if (modelAdapter == null) {
                throw new NotSupportedException(
                    "The supplied model is not a MonoGame AdapterModel."
                );
            }
            modelAdapter.NativeModel.Draw(
                ToNative(matrixWorld),
                matrixViewNative,
                matrixProjectionNative
            );
        }
        
        private GpuMesh GetOrCreateGpuMesh(IMesh mesh) {
            lock (_lock) {
                ThrowIfDisposedLocked();
                GpuMesh meshGpu;
                if (_dictionaryMeshes.TryGetValue(mesh, out meshGpu)) {
                    return meshGpu;
                }
                meshGpu = new GpuMesh(_deviceGraphics, mesh.Data);
                _dictionaryMeshes.Add(mesh, meshGpu);
                return meshGpu;
            }
        }
        
        private void ThrowIfDisposedLocked() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(MeshRenderService));
            }
        }
        
        private static Matrix ToNative(Matrix4x4 value) {
            return new Matrix(
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44
            );
        }
        
        private sealed class GpuMesh : IDisposable {
            public GpuMesh(GraphicsDevice deviceGraphics, MeshData data) {
                if (deviceGraphics == null) {
                    throw new ArgumentNullException(nameof(deviceGraphics));
                }
                if (data == null) {
                    throw new ArgumentNullException(nameof(data));
                }
                VertexPositionNormalTexture[] arrayVertices = new VertexPositionNormalTexture[data.VertexCount];
                for (int index = 0; index < data.VertexCount; index += 1) {
                    MeshVertex vertex = data.Vertices[index];
                    arrayVertices[index] = new VertexPositionNormalTexture(
                        new Microsoft.Xna.Framework.Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z),
                        new Microsoft.Xna.Framework.Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z),
                        new Microsoft.Xna.Framework.Vector2(vertex.TextureCoordinate.X, vertex.TextureCoordinate.Y)
                    );
                }
                int[] arrayIndices = new int[data.IndexCount];
                for (int index = 0; index < data.IndexCount; index += 1) {
                    arrayIndices[index] = data.Indices[index];
                }
                VertexBuffer = new VertexBuffer(
                    deviceGraphics,
                    VertexPositionNormalTexture.VertexDeclaration,
                    data.VertexCount,
                    BufferUsage.WriteOnly
                );
                VertexBuffer.SetData(arrayVertices);
                IndexBuffer = new IndexBuffer(
                    deviceGraphics,
                    IndexElementSize.ThirtyTwoBits,
                    data.IndexCount,
                    BufferUsage.WriteOnly
                );
                IndexBuffer.SetData(arrayIndices);
                VertexCount = data.VertexCount;
                PrimitiveCount = data.PrimitiveCount;
                PrimitiveType = ToNative(data.PrimitiveType);
            }
            
            public VertexBuffer VertexBuffer { get; private set; }
            public IndexBuffer IndexBuffer { get; private set; }
            public PrimitiveType PrimitiveType { get; private set; }
            public int VertexCount { get; private set; }
            public int PrimitiveCount { get; private set; }
            
            public void Dispose() {
                if (IndexBuffer != null) {
                    IndexBuffer.Dispose();
                    IndexBuffer = null;
                }
                if (VertexBuffer != null) {
                    VertexBuffer.Dispose();
                    VertexBuffer = null;
                }
                GC.SuppressFinalize(this);
            }
            
            private static PrimitiveType ToNative(MeshPrimitiveType typePrimitive) {
                if (typePrimitive == MeshPrimitiveType.TriangleList) {
                    return PrimitiveType.TriangleList;
                }
                if (typePrimitive == MeshPrimitiveType.TriangleStrip) {
                    return PrimitiveType.TriangleStrip;
                }
                if (typePrimitive == MeshPrimitiveType.LineList) {
                    return PrimitiveType.LineList;
                }
                return PrimitiveType.LineStrip;
            }
        }
    }
}
