using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;
using GodotMesh = global::Godot.Mesh;

namespace MonoGameLibrary.Adapters.Godot.ThreeDimensionalGraphics {
    /// <summary>
    /// Godot adapter for a content-managed model. A model is either a <see cref="PackedScene"/>
    /// (a hierarchy of <see cref="MeshInstance3D"/> nodes, e.g. an imported glTF scene) or a bare
    /// <see cref="GodotMesh"/> resource (e.g. an imported OBJ). The scene form is flattened into
    /// mesh parts carrying their node-local transforms, which keeps the model semantics
    /// of "several transformed meshes drawn under one world matrix".
    /// </summary>
    public sealed class AdapterModel : IModel {
        private readonly List<ModelPart> _parts;
        private readonly BoundingSphere _sphereBounds;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a model adapter over a packed scene.
        /// </summary>
        /// <param name="scene">The native packed scene.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scene"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the scene contains no renderable mesh data.</exception>
        public AdapterModel(PackedScene scene) {
            if (scene == null) {
                throw new ArgumentNullException(nameof(scene));
            }
            Node nodeRoot = scene.Instantiate(PackedScene.GenEditState.Disabled);
            List<ModelPart> parts = new List<ModelPart>();
            try {
                CollectParts(nodeRoot, Transform3D.Identity, true, parts);
            } finally {
                // The instantiated tree is only inspected; the parts hold the mesh references.
                nodeRoot.Free();
            }
            if (parts.Count == 0) {
                throw new InvalidOperationException("The model contains no renderable mesh data.");
            }
            _parts = parts;
            _sphereBounds = CalculateBounds(parts);
        }
        
        /// <summary>
        /// Initializes a model adapter over a single mesh resource.
        /// </summary>
        /// <param name="mesh">The native mesh.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="mesh"/> is null.</exception>
        public AdapterModel(GodotMesh mesh) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            _parts = new List<ModelPart>();
            _parts.Add(new ModelPart(mesh, Transform3D.Identity));
            _sphereBounds = CalculateBounds(_parts);
        }
        
        /// <inheritdoc />
        public BoundingSphere BoundsBoundingSphere { get { return _sphereBounds; } }
        
        /// <summary>
        /// Gets the flattened mesh parts with their model-space transforms.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the model is disposed.</exception>
        internal IReadOnlyList<ModelPart> Parts {
            get {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(nameof(AdapterModel));
                }
                return _parts;
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            _parts.Clear();
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// One flattened model part: a native mesh and its model-space transform.
        /// </summary>
        internal sealed class ModelPart {
            private readonly GodotMesh _mesh;
            private readonly Transform3D _transformLocal;
            
            /// <summary>Initializes a model part.</summary>
            /// <param name="mesh">The native mesh.</param>
            /// <param name="transformLocal">The model-space transform of the mesh.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="mesh"/> is null.</exception>
            public ModelPart(GodotMesh mesh, Transform3D transformLocal) {
                if (mesh == null) {
                    throw new ArgumentNullException(nameof(mesh));
                }
                _mesh = mesh;
                _transformLocal = transformLocal;
            }
            
            /// <summary>Gets the native mesh.</summary>
            public GodotMesh MeshNative { get { return _mesh; } }
            
            /// <summary>Gets the model-space transform.</summary>
            public Transform3D TransformLocal { get { return _transformLocal; } }
        }
        
        private static void CollectParts(
            Node node,
            Transform3D transformAccumulated,
            bool flagParentIsNode3D,
            List<ModelPart> parts
        ) {
            Transform3D transformCurrent = transformAccumulated;
            Node3D node3D = node as Node3D;
            if (node3D != null) {
                // A Node3D whose parent is not a Node3D is top level: its transform is
                // relative to the world, not to the intervening plain node.
                transformCurrent = flagParentIsNode3D
                    ? transformAccumulated * node3D.Transform
                    : node3D.Transform;
            }
            MeshInstance3D instance = node as MeshInstance3D;
            if (instance != null && instance.Mesh != null) {
                parts.Add(new ModelPart(instance.Mesh, transformCurrent));
            }
            bool flagChildParentIsNode3D = node3D != null;
            foreach (Node child in node.GetChildren()) {
                CollectParts(child, transformCurrent, flagChildParentIsNode3D, parts);
            }
        }
        
        private static BoundingSphere CalculateBounds(List<ModelPart> parts) {
            float radius = 0f;
            foreach (ModelPart part in parts) {
                Aabb aabb = part.MeshNative.GetAabb();
                System.Numerics.Vector3 center = new System.Numerics.Vector3(
                    aabb.Position.X + aabb.Size.X * 0.5f,
                    aabb.Position.Y + aabb.Size.Y * 0.5f,
                    aabb.Position.Z + aabb.Size.Z * 0.5f
                );
                float radiusLocal = aabb.Size.Length() * 0.5f;
                Vector3 centerTransformed = part.TransformLocal * new Vector3(center.X, center.Y, center.Z);
                System.Numerics.Vector3 centerWorld = new System.Numerics.Vector3(
                    centerTransformed.X,
                    centerTransformed.Y,
                    centerTransformed.Z
                );
                float scale = MaxAxisScale(part.TransformLocal.Basis);
                float distance = centerWorld.Length() + radiusLocal * scale;
                if (distance > radius) {
                    radius = distance;
                }
            }
            return new BoundingSphere(System.Numerics.Vector3.Zero, radius);
        }
        
        private static float MaxAxisScale(Basis basis) {
            float scale = basis.X.Length();
            float scaleY = basis.Y.Length();
            float scaleZ = basis.Z.Length();
            if (scaleY > scale) {
                scale = scaleY;
            }
            if (scaleZ > scale) {
                scale = scaleZ;
            }
            return scale;
        }
    }
}
