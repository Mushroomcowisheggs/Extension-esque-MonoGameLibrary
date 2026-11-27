using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Fna.ThreeDimensionalGraphics {
    /// <summary>
    /// FNA mesh renderer using BasicEffect for neutral mesh data and the native model draw
    /// path for compiled Model assets.
    /// </summary>
    public sealed class MeshRenderService : IMeshRenderService {
        private readonly GraphicsDevice _deviceGraphics;
        private readonly IThreeDimensionalCameraService _serviceCamera;
        private readonly IEnvironmentService _serviceEnvironment;
        private readonly BasicEffect _effectBasic;
        private readonly ThreeDimensionalRenderQueue _queueRender = new ThreeDimensionalRenderQueue();
        private readonly Dictionary<IMesh, GpuMesh> _dictionaryMeshes = new Dictionary<IMesh, GpuMesh>();
        private readonly object _lock = new object();
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes the renderer with the default lighting rig and no environment
        /// override.
        /// </summary>
        /// <param name="deviceGraphics">The graphics device used for drawing.</param>
        /// <param name="serviceCamera">The camera supplying view and projection matrices.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
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
            _serviceEnvironment = null;
            _effectBasic = CreateEffect(deviceGraphics);
        }
        
        /// <summary>
        /// Initializes the renderer with an environment service. While the
        /// environment is enabled its sun, ambient term and background replace the
        /// default lighting rig and clear color; while it is disabled the
        /// renderer behaves as before. Shadows are a Godot-only capability and are not
        /// drawn here.
        /// </summary>
        /// <param name="deviceGraphics">The graphics device used for drawing.</param>
        /// <param name="serviceCamera">The camera supplying view and projection matrices.</param>
        /// <param name="serviceEnvironment">The environment description.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public MeshRenderService(
            GraphicsDevice deviceGraphics,
            IThreeDimensionalCameraService serviceCamera,
            IEnvironmentService serviceEnvironment
        ) {
            if (deviceGraphics == null) {
                throw new ArgumentNullException(nameof(deviceGraphics));
            }
            if (serviceCamera == null) {
                throw new ArgumentNullException(nameof(serviceCamera));
            }
            if (serviceEnvironment == null) {
                throw new ArgumentNullException(nameof(serviceEnvironment));
            }
            _deviceGraphics = deviceGraphics;
            _serviceCamera = serviceCamera;
            _serviceEnvironment = serviceEnvironment;
            _effectBasic = CreateEffect(deviceGraphics);
        }
        
        /// <summary>
        /// Creates the shared basic effect with the default lighting rig enabled.
        /// </summary>
        /// <param name="deviceGraphics">The graphics device used for drawing.</param>
        private static BasicEffect CreateEffect(GraphicsDevice deviceGraphics) {
            BasicEffect effectBasic = new BasicEffect(deviceGraphics);
            effectBasic.LightingEnabled = true;
            effectBasic.EnableDefaultLighting();
            effectBasic.VertexColorEnabled = true;
            effectBasic.TextureEnabled = false;
            return effectBasic;
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
        public void Submit(IMesh mesh, Matrix4x4 matrixWorld, MeshMaterial material) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            lock (_lock) {
                ThrowIfDisposedLocked();
                _queueRender.Submit(mesh, matrixWorld, material);
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
        public void Submit(IModel model, Matrix4x4 matrixWorld, MeshMaterial material) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            lock (_lock) {
                ThrowIfDisposedLocked();
                _queueRender.Submit(model, matrixWorld, material);
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            lock (_lock) {
                ThrowIfDisposedLocked();
            }
            ThreeDimensionalCameraState stateCamera = _serviceCamera.CameraState;
            ApplyEnvironment();
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
                        MeshMaterial? materialModel = submission.Material;
                        if (materialModel == null) {
                            DrawModel(submission.Model, submission.WorldMatrix, matrixViewNative, matrixProjectionNative);
                        } else {
                            DrawModelWithMaterial(submission.Model, submission.WorldMatrix, matrixViewNative, matrixProjectionNative, materialModel.Value);
                        }
                    } else {
                        MeshMaterial? materialEffective;
                        if (submission.Material == null) {
                            materialEffective = submission.Mesh.Data.Material;
                        } else {
                            materialEffective = submission.Material;
                        }
                        ApplyRenderState(materialEffective);
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
                    "The supplied model is not a FNA AdapterModel."
                );
            }
            modelAdapter.NativeModel.Draw(
                ToNative(matrixWorld),
                matrixViewNative,
                matrixProjectionNative
            );
        }
        
        /// <summary>
        /// Draws every part of a model with the supplied material applied to its basic
        /// effect, restoring the default effect parameters afterwards. Parts whose effect
        /// is not a basic effect keep their own look.
        /// </summary>
        private void DrawModelWithMaterial(
            IModel model,
            Matrix4x4 matrixWorld,
            Matrix matrixViewNative,
            Matrix matrixProjectionNative,
            MeshMaterial material
        ) {
            AdapterModel modelAdapter = model as AdapterModel;
            if (modelAdapter == null) {
                throw new NotSupportedException(
                    "The supplied model is not a FNA AdapterModel."
                );
            }
            ApplyRenderState(material);
            Model modelNative = modelAdapter.NativeModel;
            try {
                Microsoft.Xna.Framework.Matrix[] arrayTransforms =
                    new Microsoft.Xna.Framework.Matrix[modelNative.Bones.Count];
                modelNative.CopyAbsoluteBoneTransformsTo(arrayTransforms);
                foreach (ModelMesh mesh in modelNative.Meshes) {
                    Microsoft.Xna.Framework.Matrix transformBone;
                    if (mesh.ParentBone == null) {
                        transformBone = Microsoft.Xna.Framework.Matrix.Identity;
                    } else {
                        transformBone = arrayTransforms[mesh.ParentBone.Index];
                    }
                    foreach (ModelMeshPart part in mesh.MeshParts) {
                        BasicEffect effectBasic = part.Effect as BasicEffect;
                        if (effectBasic == null) {
                            continue;
                        }
                        ApplyMaterial(effectBasic, material);
                        effectBasic.World = transformBone * ToNative(matrixWorld);
                        effectBasic.View = matrixViewNative;
                        effectBasic.Projection = matrixProjectionNative;
                        _deviceGraphics.SetVertexBuffer(part.VertexBuffer);
                        _deviceGraphics.Indices = part.IndexBuffer;
                        foreach (EffectPass pass in effectBasic.CurrentTechnique.Passes) {
                            pass.Apply();
                            _deviceGraphics.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList,
                                part.VertexOffset,
                                0,
                                part.NumVertices,
                                part.StartIndex,
                                part.PrimitiveCount
                            );
                        }
                    }
                }
            } finally {
                foreach (ModelMesh mesh in modelNative.Meshes) {
                    foreach (ModelMeshPart part in mesh.MeshParts) {
                        BasicEffect effectBasic = part.Effect as BasicEffect;
                        if (effectBasic != null) {
                            ResetMaterial(effectBasic);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies the environment description for this frame. While it is enabled the
        /// sun and ambient term replace the default lighting rig and the background
        /// clears the frame; while disabled the default rig renders as before and the
        /// background is left to the game. A zero sun direction reported by the
        /// environment is rejected here with an <see cref="ArgumentException"/> instead
        /// of being silently replaced, so a custom <see cref="IEnvironmentService"/>
        /// cannot smuggle an undefined direction in.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the sun direction is the zero vector.</exception>
        private void ApplyEnvironment() {
            if (_serviceEnvironment == null || !_serviceEnvironment.FlagEnabled) {
                _effectBasic.EnableDefaultLighting();
                return;
            }
            _effectBasic.LightingEnabled = true;
            _effectBasic.AmbientLightColor = ToNativeRgb(_serviceEnvironment.AmbientColor);
            _effectBasic.DirectionalLight0.Enabled = true;
            System.Numerics.Vector3 direction = _serviceEnvironment.SunDirection;
            Microsoft.Xna.Framework.Vector3 directionSun = new Microsoft.Xna.Framework.Vector3(
                direction.X, direction.Y, direction.Z
            );
            if (directionSun.LengthSquared() == 0f) {
                throw new ArgumentException(
                    "The environment sun direction must not be the zero vector."
                );
            }
            directionSun.Normalize();
            _effectBasic.DirectionalLight0.Direction = directionSun;
            Microsoft.Xna.Framework.Vector3 colorSun = ToNativeRgb(_serviceEnvironment.SunColor);
            _effectBasic.DirectionalLight0.DiffuseColor = colorSun * _serviceEnvironment.SunIntensity;
            _effectBasic.DirectionalLight0.SpecularColor = Microsoft.Xna.Framework.Vector3.Zero;
            _effectBasic.DirectionalLight1.Enabled = false;
            _effectBasic.DirectionalLight2.Enabled = false;
            MonoGameLibrary.Core.Primitives.Color colorBackground = _serviceEnvironment.BackgroundColor;
            _deviceGraphics.Clear(new Microsoft.Xna.Framework.Color(
                colorBackground.R, colorBackground.G, colorBackground.B, colorBackground.A
            ));
        }
        
        /// <summary>
        /// Applies the effective material of one submission to the shared effect and to
        /// the blend and culling state. A null material restores the default look.
        /// </summary>
        private void ApplyRenderState(MeshMaterial? material) {
            if (material == null) {
                ResetMaterial(_effectBasic);
                _deviceGraphics.BlendState = BlendState.Opaque;
                _deviceGraphics.RasterizerState = RasterizerState.CullCounterClockwise;
                return;
            }
            MeshMaterial materialValue = material.Value;
            ApplyMaterial(_effectBasic, materialValue);
            _deviceGraphics.BlendState = materialValue.AlbedoColor.A < 255
                ? BlendState.AlphaBlend
                : BlendState.Opaque;
            _deviceGraphics.RasterizerState = ToNative(materialValue.CullMode);
        }
        
        private static void ApplyMaterial(BasicEffect effect, MeshMaterial material) {
            effect.DiffuseColor = ToNativeRgb(material.AlbedoColor);
            effect.Alpha = material.AlbedoColor.A / 255f;
            effect.EmissiveColor = ToNativeRgb(material.EmissionColor);
            effect.LightingEnabled = !material.UnshadedFlag;
        }
        
        private static void ResetMaterial(BasicEffect effect) {
            effect.DiffuseColor = Microsoft.Xna.Framework.Vector3.One;
            effect.Alpha = 1f;
            effect.EmissiveColor = Microsoft.Xna.Framework.Vector3.Zero;
            effect.LightingEnabled = true;
        }
        
        private static Microsoft.Xna.Framework.Vector3 ToNativeRgb(
            MonoGameLibrary.Core.Primitives.Color color
        ) {
            return new Microsoft.Xna.Framework.Vector3(
                color.R / 255f, color.G / 255f, color.B / 255f
            );
        }
        
        private static RasterizerState ToNative(MeshCullMode cullMode) {
            if (cullMode == MeshCullMode.None) {
                return RasterizerState.CullNone;
            }
            if (cullMode == MeshCullMode.Front) {
                return RasterizerState.CullClockwise;
            }
            return RasterizerState.CullCounterClockwise;
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
                VertexPositionNormalTextureColor[] arrayVertices = new VertexPositionNormalTextureColor[data.VertexCount];
                for (int index = 0; index < data.VertexCount; index += 1) {
                    MeshVertex vertex = data.Vertices[index];
                    arrayVertices[index] = new VertexPositionNormalTextureColor(
                        new Microsoft.Xna.Framework.Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z),
                        new Microsoft.Xna.Framework.Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z),
                        new Microsoft.Xna.Framework.Vector2(vertex.TextureCoordinate.X, vertex.TextureCoordinate.Y),
                        new Microsoft.Xna.Framework.Color(vertex.Color.R, vertex.Color.G, vertex.Color.B, vertex.Color.A)
                    );
                }
                int[] arrayIndices = new int[data.IndexCount];
                for (int index = 0; index < data.IndexCount; index += 1) {
                    arrayIndices[index] = data.Indices[index];
                }
                VertexBuffer = new VertexBuffer(
                    deviceGraphics,
                    VertexPositionNormalTextureColor.Declaration,
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
            
            /// <summary>
            /// One GPU vertex: position, normal, texture coordinate and color, the colored
            /// counterpart of XNA's position/normal/texture vertex.
            /// </summary>
            private struct VertexPositionNormalTextureColor : IVertexType {
                public Microsoft.Xna.Framework.Vector3 Position;
                public Microsoft.Xna.Framework.Vector3 Normal;
                public Microsoft.Xna.Framework.Vector2 TextureCoordinate;
                public Microsoft.Xna.Framework.Color Color;
                
                public VertexPositionNormalTextureColor(
                    Microsoft.Xna.Framework.Vector3 position,
                    Microsoft.Xna.Framework.Vector3 normal,
                    Microsoft.Xna.Framework.Vector2 coordinateTexture,
                    Microsoft.Xna.Framework.Color color
                ) {
                    Position = position;
                    Normal = normal;
                    TextureCoordinate = coordinateTexture;
                    Color = color;
                }
                
                public static readonly VertexDeclaration Declaration = new VertexDeclaration(
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                );
                
                VertexDeclaration IVertexType.VertexDeclaration {
                    get { return Declaration; }
                }
            }
        }
    }
}
