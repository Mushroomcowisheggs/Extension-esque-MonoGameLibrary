using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Kni.ThreeDimensionalGraphics {
    /// <summary>
    /// KNI mesh renderer using BasicEffect for buffer-backed meshes and the
    /// native model draw path for compiled Model assets.
    /// </summary>
    public sealed class MeshRenderService : IMeshRenderService {
        private readonly GraphicsDevice _deviceGraphics;
        private readonly IThreeDimensionalCameraService _serviceCamera;
        private readonly BasicEffect _effectBasic;
        private readonly ThreadAccess _accessThread = new ThreadAccess();
        private readonly object _lock = new object();
        private readonly List<Submission> _listSubmissions = new List<Submission>();
        private bool _flagDisposed;
        
        private readonly struct Submission {
            public Submission(IMesh mesh, System.Numerics.Matrix4x4 matrixWorld) {
                if (mesh == null) {
                    throw new ArgumentNullException(nameof(mesh));
                }
                Mesh = mesh;
                Model = null;
                WorldMatrix = matrixWorld;
            }
            
            public Submission(IModel model, System.Numerics.Matrix4x4 matrixWorld) {
                if (model == null) {
                    throw new ArgumentNullException(nameof(model));
                }
                Mesh = null;
                Model = model;
                WorldMatrix = matrixWorld;
            }
            
            public IMesh Mesh { get; }
            public IModel Model { get; }
            public System.Numerics.Matrix4x4 WorldMatrix { get; }
            public bool FlagModel {
                get { return Model != null; }
            }
        }
        
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
        public void Submit(IMesh mesh, System.Numerics.Matrix4x4 matrixWorld) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            lock (_lock) {
                ThrowIfDisposedLocked();
                _listSubmissions.Add(new Submission(mesh, matrixWorld));
            }
        }
        
        /// <inheritdoc />
        public void Submit(IModel model, System.Numerics.Matrix4x4 matrixWorld) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            lock (_lock) {
                ThrowIfDisposedLocked();
                _listSubmissions.Add(new Submission(model, matrixWorld));
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                ThrowIfDisposedLocked();
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            _accessThread.VerifyAccess();
            List<Submission> listSubmissions;
            lock (_lock) {
                ThrowIfDisposedLocked();
                listSubmissions = new List<Submission>(_listSubmissions);
                _listSubmissions.Clear();
            }
            if (listSubmissions.Count == 0) {
                return;
            }
            
            ThreeDimensionalCameraState stateCamera = _serviceCamera.CameraState;
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
                foreach (Submission submission in listSubmissions) {
                    if (submission.FlagModel) {
                        DrawModel(submission.Model, submission.WorldMatrix, matrixViewNative, matrixProjectionNative);
                    } else {
                        DrawMesh(submission.Mesh, submission.WorldMatrix, matrixViewNative, matrixProjectionNative);
                    }
                }
            } finally {
                _deviceGraphics.SetVertexBuffers();
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
                _listSubmissions.Clear();
            }
            _effectBasic.Dispose();
            GC.SuppressFinalize(this);
        }
        
        private void DrawMesh(
            IMesh mesh,
            System.Numerics.Matrix4x4 matrixWorld,
            Matrix matrixViewNative,
            Matrix matrixProjectionNative
        ) {
            MeshAdapter meshAdapter = mesh as MeshAdapter;
            if (meshAdapter == null) {
                throw new NotSupportedException(
                    "The supplied mesh is not a KNI MeshAdapter."
                );
            }
            _deviceGraphics.SetVertexBuffer(meshAdapter.NativeVertexBuffer);
            _deviceGraphics.Indices = meshAdapter.NativeIndexBuffer;
            _effectBasic.World = ToNative(matrixWorld);
            _effectBasic.View = matrixViewNative;
            _effectBasic.Projection = matrixProjectionNative;
            foreach (EffectPass pass in _effectBasic.CurrentTechnique.Passes) {
                pass.Apply();
                _deviceGraphics.DrawIndexedPrimitives(
                    meshAdapter.NativePrimitiveType,
                    meshAdapter.NativeVertexOffset,
                    0,
                    meshAdapter.NativeVertexCount,
                    meshAdapter.NativeIndexStart,
                    meshAdapter.NativePrimitiveCount
                );
            }
        }
        
        private void DrawModel(
            IModel model,
            System.Numerics.Matrix4x4 matrixWorld,
            Matrix matrixViewNative,
            Matrix matrixProjectionNative
        ) {
            ModelAdapter modelAdapter = model as ModelAdapter;
            if (modelAdapter == null) {
                throw new NotSupportedException(
                    "The supplied model is not a KNI ModelAdapter."
                );
            }
            modelAdapter.NativeModel.Draw(
                ToNative(matrixWorld),
                matrixViewNative,
                matrixProjectionNative
            );
        }
        
        private void ThrowIfDisposedLocked() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(MeshRenderService));
            }
        }
        
        private static Matrix ToNative(System.Numerics.Matrix4x4 value) {
            return new Matrix(
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44
            );
        }
    }
}
