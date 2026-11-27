using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;
using GodotMesh = global::Godot.Mesh;
using GodotAabb = global::Godot.Aabb;
using ModelAabb = MonoGameLibrary.Extensions.ThreeDimensionalGraphics.Aabb;

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
        private readonly ModelAabb? _aabbBounds;
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
            ModelAabb aabbModel = CalculateBoundsAabb(parts);
            _sphereBounds = CalculateBounds(aabbModel);
            _aabbBounds = aabbModel;
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
            ModelAabb aabbModel = CalculateBoundsAabb(_parts);
            _sphereBounds = CalculateBounds(aabbModel);
            _aabbBounds = aabbModel;
        }
        
        /// <summary>
        /// Gets the conservative culling hull that is guaranteed to contain all model
        /// geometry: the sphere around the merged model-space box of the parts, centered
        /// on the box center and reaching the farthest corner. Exact dimensions are only
        /// available through <see cref="BoundsAabb"/>.
        /// </summary>
        public BoundingSphere BoundsBoundingSphere { get { return _sphereBounds; } }
        
        /// <summary>
        /// Gets the exact model-space axis-aligned bounding box, or null when the back
        /// end cannot compute it exactly. A null value never masquerades as a coarse
        /// approximation.
        /// </summary>
        public ModelAabb? BoundsAabb { get { return _aabbBounds; } }
        
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
            bool flagChildParentIsNode3D;
            if (node3D != null) {
                flagChildParentIsNode3D = true;
            } else {
                flagChildParentIsNode3D = false;
            }
            foreach (Node child in node.GetChildren()) {
                CollectParts(child, transformCurrent, flagChildParentIsNode3D, parts);
            }
        }
        
        /// <summary>
        /// Computes the model-space conservative culling hull from the merged box of the
        /// parts: the sphere centered on the box center whose radius reaches the farthest
        /// box corner. Exact dimensions are only available through BoundsAabb.
        /// </summary>
        private static BoundingSphere CalculateBounds(ModelAabb aabbModel) {
            System.Numerics.Vector3 center = aabbModel.Center;
            float radius = (aabbModel.Maximum - center).Length();
            return new BoundingSphere(center, radius);
        }
        
        /// <summary>
        /// Computes the model-space axis-aligned bounding box by transforming the eight
        /// corners of every part's local mesh box and merging them, the same box source
        /// Godot uses for scene bounds.
        /// </summary>
        private static ModelAabb CalculateBoundsAabb(List<ModelPart> parts) {
            bool flagAny = false;
            System.Numerics.Vector3 minimum = System.Numerics.Vector3.Zero;
            System.Numerics.Vector3 maximum = System.Numerics.Vector3.Zero;
            foreach (ModelPart part in parts) {
                GodotAabb aabb = part.MeshNative.GetAabb();
                for (int corner = 0; corner < 8; corner += 1) {
                    float positionCornerX;
                    if ((corner & 1) == 0) {
                        positionCornerX = aabb.Position.X;
                    } else {
                        positionCornerX = aabb.Position.X + aabb.Size.X;
                    }
                    float positionCornerY;
                    if ((corner & 2) == 0) {
                        positionCornerY = aabb.Position.Y;
                    } else {
                        positionCornerY = aabb.Position.Y + aabb.Size.Y;
                    }
                    float positionCornerZ;
                    if ((corner & 4) == 0) {
                        positionCornerZ = aabb.Position.Z;
                    } else {
                        positionCornerZ = aabb.Position.Z + aabb.Size.Z;
                    }
                    Vector3 positionLocal = new Vector3(
                        positionCornerX,
                        positionCornerY,
                        positionCornerZ
                    );
                    Vector3 positionTransformed = part.TransformLocal * positionLocal;
                    System.Numerics.Vector3 position = new System.Numerics.Vector3(
                        positionTransformed.X,
                        positionTransformed.Y,
                        positionTransformed.Z
                    );
                    if (!flagAny) {
                        minimum = position;
                        maximum = position;
                        flagAny = true;
                    } else {
                        minimum = System.Numerics.Vector3.Min(minimum, position);
                        maximum = System.Numerics.Vector3.Max(maximum, position);
                    }
                }
            }
            if (!flagAny) {
                return new ModelAabb(System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero);
            }
            return new ModelAabb(minimum, maximum);
        }
    }
}
