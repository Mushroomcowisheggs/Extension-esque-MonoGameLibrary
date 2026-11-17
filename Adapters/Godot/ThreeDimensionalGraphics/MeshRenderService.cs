using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;
using GodotMesh = global::Godot.Mesh;
using GodotArray = global::Godot.Collections.Array;

namespace MonoGameLibrary.Adapters.Godot.ThreeDimensionalGraphics {
    /// <summary>
    /// Godot mesh renderer using pooled <see cref="MeshInstance3D"/> instances for neutral mesh
    /// data and flattened mesh parts for model assets. Every draw pass recycles the pool: one
    /// slot per submission, visible for exactly that frame, which is an immediate submission
    /// model. The neutral path carries the default key/fill/back lighting rig, and
    /// model parts keep the materials of their imported surfaces.
    /// </summary>
    /// <remarks>
    /// Godot renders the 3D pass before the canvas pass that drives the host draw, so pool
    /// updates made here appear in the engine's next 3D pass; per-frame submission lifetimes
    /// are still exactly one draw pass. The camera and light nodes live under the registered
    /// anchor node, and the submissions render in that viewport's world alongside any
    /// scene-tree 3D content the game owns.
    /// </remarks>
    public sealed class MeshRenderService : IMeshRenderService {
        private readonly Node _nodeAnchor;
        private readonly IThreeDimensionalCameraService _serviceCamera;
        private readonly ThreeDimensionalRenderQueue _queueRender = new ThreeDimensionalRenderQueue();
        private readonly Dictionary<IMesh, GpuMesh> _dictionaryMeshes = new Dictionary<IMesh, GpuMesh>();
        private readonly List<MeshInstance3D> _nodesActive = new List<MeshInstance3D>();
        private readonly Stack<MeshInstance3D> _nodesFree = new Stack<MeshInstance3D>();
        private readonly List<DirectionalLight3D> _lights = new List<DirectionalLight3D>();
        private readonly Camera3D _camera;
        private readonly object _lock = new object();
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes the renderer.
        /// </summary>
        /// <param name="nodeAnchor">The node that parents the camera, lights and instance pool.</param>
        /// <param name="serviceCamera">The camera supplying view and projection matrices.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="nodeAnchor"/> or <paramref name="serviceCamera"/> is null.
        /// </exception>
        public MeshRenderService(
            Node nodeAnchor,
            IThreeDimensionalCameraService serviceCamera
        ) {
            if (nodeAnchor == null) {
                throw new ArgumentNullException(nameof(nodeAnchor));
            }
            if (serviceCamera == null) {
                throw new ArgumentNullException(nameof(serviceCamera));
            }
            _nodeAnchor = nodeAnchor;
            _serviceCamera = serviceCamera;
            
            // The rig is the default key/fill/back setup: three directional lights with
            // fixed directions, colors and specular weights. Ambient light cannot be
            // scoped to these draws in Godot's shared world; the neutral material bakes it in.
            _camera = new Camera3D();
            nodeAnchor.AddChild(_camera);
            _camera.Current = true;
            _lights.Add(CreateLight(
                nodeAnchor, -0.5265408f, -0.5735765f, -0.6275069f,
                1f, 0.9607844f, 0.8078432f, 1f
            ));
            _lights.Add(CreateLight(
                nodeAnchor, 0.7198464f, 0.3420201f, 0.6040227f,
                0.9647059f, 0.7607844f, 0.4078432f, 0f
            ));
            _lights.Add(CreateLight(
                nodeAnchor, 0.4545195f, -0.7660444f, 0.4545195f,
                0.3231373f, 0.3607844f, 0.3937255f, 1f
            ));
        }
        
        /// <inheritdoc />
        public void Submit(IMesh mesh, System.Numerics.Matrix4x4 matrixWorld) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            lock (_lock) {
                ThrowIfDisposedLocked();
                _queueRender.Submit(mesh, matrixWorld);
            }
        }
        
        /// <inheritdoc />
        public void Submit(IModel model, System.Numerics.Matrix4x4 matrixWorld) {
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
            lock (_lock) {
                ThrowIfDisposedLocked();
                RecycleSlotsLocked();
            }
            ThreeDimensionalCameraState stateCamera = _serviceCamera.CameraState;
            System.Numerics.Vector3 position = stateCamera.Position;
            System.Numerics.Quaternion orientation = stateCamera.Orientation;
            _camera.GlobalTransform = new Transform3D(
                new Basis(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)),
                new Vector3(position.X, position.Y, position.Z)
            );
            _camera.Fov = (float)(_serviceCamera.FieldOfViewRadians * (180.0 / System.Math.PI));
            _camera.Near = _serviceCamera.NearPlaneDistance;
            _camera.Far = _serviceCamera.FarPlaneDistance;
            
            System.Numerics.Matrix4x4 matrixViewProjection =
                stateCamera.ViewMatrix * stateCamera.ProjectionMatrix;
            List<ThreeDimensionalSubmission> listSubmissions =
                _queueRender.TakeVisible(matrixViewProjection);
            if (listSubmissions.Count == 0) {
                return;
            }
            foreach (ThreeDimensionalSubmission submission in listSubmissions) {
                if (submission.FlagModel) {
                    DrawModel(submission.Model, submission.WorldMatrix);
                } else {
                    DrawMesh(submission.Mesh, submission.WorldMatrix);
                }
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
                foreach (MeshInstance3D node in _nodesActive) {
                    node.QueueFree();
                }
                _nodesActive.Clear();
                foreach (MeshInstance3D node in _nodesFree) {
                    node.QueueFree();
                }
                _nodesFree.Clear();
            }
            _camera.QueueFree();
            foreach (DirectionalLight3D light in _lights) {
                light.QueueFree();
            }
            GC.SuppressFinalize(this);
        }
        
        private void DrawMesh(
            IMesh mesh,
            System.Numerics.Matrix4x4 matrixWorld
        ) {
            GpuMesh meshGpu = GetOrCreateGpuMesh(mesh);
            MeshInstance3D node = AcquireSlot();
            node.Mesh = meshGpu.MeshNative;
            node.GlobalTransform = ToGodotTransform(matrixWorld);
        }
        
        private void DrawModel(
            IModel model,
            System.Numerics.Matrix4x4 matrixWorld
        ) {
            AdapterModel modelAdapter = model as AdapterModel;
            if (modelAdapter == null) {
                throw new NotSupportedException(
                    "The supplied model is not a Godot AdapterModel."
                );
            }
            Transform3D transformWorld = ToGodotTransform(matrixWorld);
            foreach (AdapterModel.ModelPart part in modelAdapter.Parts) {
                MeshInstance3D node = AcquireSlot();
                node.Mesh = part.MeshNative;
                node.GlobalTransform = transformWorld * part.TransformLocal;
            }
        }
        
        private GpuMesh GetOrCreateGpuMesh(IMesh mesh) {
            lock (_lock) {
                ThrowIfDisposedLocked();
                GpuMesh meshGpu;
                if (_dictionaryMeshes.TryGetValue(mesh, out meshGpu)) {
                    return meshGpu;
                }
                meshGpu = new GpuMesh(mesh.Data);
                _dictionaryMeshes.Add(mesh, meshGpu);
                return meshGpu;
            }
        }
        
        private MeshInstance3D AcquireSlot() {
            lock (_lock) {
                ThrowIfDisposedLocked();
                MeshInstance3D node;
                if (_nodesFree.Count > 0) {
                    node = _nodesFree.Pop();
                } else {
                    node = new MeshInstance3D();
                    node.Visible = false;
                    _nodeAnchor.AddChild(node);
                }
                node.Visible = true;
                _nodesActive.Add(node);
                return node;
            }
        }
        
        private void RecycleSlotsLocked() {
            foreach (MeshInstance3D node in _nodesActive) {
                node.Visible = false;
                _nodesFree.Push(node);
            }
            _nodesActive.Clear();
        }
        
        private void ThrowIfDisposedLocked() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(MeshRenderService));
            }
        }
        
        private static DirectionalLight3D CreateLight(
            Node nodeParent,
            float x,
            float y,
            float z,
            float red,
            float green,
            float blue,
            float specular
        ) {
            // The rig's constants give the direction the light travels along; Godot's
            // directional light emits along its local -Z axis.
            Vector3 directionTravel = new Vector3(x, y, z).Normalized();
            Vector3 axisZ = -directionTravel;
            Vector3 axisUp = MathF.Abs(axisZ.Dot(new Vector3(0f, 1f, 0f))) > 0.999f
                ? new Vector3(1f, 0f, 0f)
                : new Vector3(0f, 1f, 0f);
            Vector3 axisX = axisUp.Cross(axisZ).Normalized();
            Vector3 axisY = axisZ.Cross(axisX);
            DirectionalLight3D light = new DirectionalLight3D();
            nodeParent.AddChild(light);
            light.GlobalTransform = new Transform3D(new Basis(axisX, axisY, axisZ), Vector3.Zero);
            light.LightColor = new Color(red, green, blue, 1f);
            light.LightSpecular = specular;
            return light;
        }
        
        private static Transform3D ToGodotTransform(System.Numerics.Matrix4x4 value) {
            // System.Numerics uses row vectors (v * M) with translation in the fourth row,
            // so the Godot basis columns are the matrix rows and the origin is row four.
            Basis basis = new Basis(
                new Vector3(value.M11, value.M12, value.M13),
                new Vector3(value.M21, value.M22, value.M23),
                new Vector3(value.M31, value.M32, value.M33)
            );
            return new Transform3D(basis, new Vector3(value.M41, value.M42, value.M43));
        }
        
        private sealed class GpuMesh : IDisposable {
            private ArrayMesh _meshNative;
            private StandardMaterial3D _material;
            private bool _flagDisposed;
            
            public GpuMesh(MeshData data) {
                if (data == null) {
                    throw new ArgumentNullException(nameof(data));
                }
                Vector3[] arrayPosition = new Vector3[data.VertexCount];
                Vector3[] arrayNormal = new Vector3[data.VertexCount];
                Vector2[] arrayTexture = new Vector2[data.VertexCount];
                for (int index = 0; index < data.VertexCount; index += 1) {
                    MeshVertex vertex = data.Vertices[index];
                    arrayPosition[index] = new Vector3(
                        vertex.Position.X, vertex.Position.Y, vertex.Position.Z
                    );
                    arrayNormal[index] = new Vector3(
                        vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z
                    );
                    arrayTexture[index] = new Vector2(
                        vertex.TextureCoordinate.X, vertex.TextureCoordinate.Y
                    );
                }
                int[] arrayIndex = new int[data.IndexCount];
                for (int index = 0; index < data.IndexCount; index += 1) {
                    arrayIndex[index] = data.Indices[index];
                }
                GodotArray arrays = new GodotArray();
                arrays.Resize((int)GodotMesh.ArrayType.Max);
                arrays[(int)GodotMesh.ArrayType.Vertex] = arrayPosition;
                arrays[(int)GodotMesh.ArrayType.Normal] = arrayNormal;
                arrays[(int)GodotMesh.ArrayType.TexUV] = arrayTexture;
                arrays[(int)GodotMesh.ArrayType.Index] = arrayIndex;
                _meshNative = new ArrayMesh();
                _meshNative.AddSurfaceFromArrays(
                    ToNative(data.PrimitiveType), arrays, default, default, default
                );
                // Untextured geometry renders in white plus the rig's ambient term;
                // emission carries that ambient into Godot's shared world.
                _material = new StandardMaterial3D();
                _material.AlbedoColor = new Color(1f, 1f, 1f, 1f);
                _material.EmissionEnabled = true;
                _material.Emission = new Color(0.05333332f, 0.09882354f, 0.1819608f, 1f);
                _meshNative.SurfaceSetMaterial(0, _material);
            }
            
            public ArrayMesh MeshNative {
                get {
                    if (_meshNative == null) {
                        throw new ObjectDisposedException(nameof(GpuMesh));
                    }
                    return _meshNative;
                }
            }
            
            public void Dispose() {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                try {
                    if (_meshNative != null) {
                        _meshNative.Dispose();
                    }
                } catch (Exception exception) {
                    ReportCleanupFailure(exception);
                } finally {
                    _meshNative = null;
                }
                try {
                    if (_material != null) {
                        _material.Dispose();
                    }
                } catch (Exception exception) {
                    ReportCleanupFailure(exception);
                } finally {
                    _material = null;
                }
                GC.SuppressFinalize(this);
            }
            
            private static void ReportCleanupFailure(Exception exception) {
                // This nested holder has no logger to report through, so the failure is
                // surfaced on the platform error channel; the per-resource guards exist so
                // that one release failure cannot abandon the remaining release.
                GD.PushError("GpuMesh.Dispose: " + exception.Message);
            }
            
            private static GodotMesh.PrimitiveType ToNative(MeshPrimitiveType typePrimitive) {
                if (typePrimitive == MeshPrimitiveType.TriangleList) {
                    return GodotMesh.PrimitiveType.Triangles;
                }
                if (typePrimitive == MeshPrimitiveType.TriangleStrip) {
                    return GodotMesh.PrimitiveType.TriangleStrip;
                }
                if (typePrimitive == MeshPrimitiveType.LineList) {
                    return GodotMesh.PrimitiveType.Lines;
                }
                return GodotMesh.PrimitiveType.LineStrip;
            }
        }
    }
}
