using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.MonoGame.ThreeDimensionalGraphics {
    /// <summary>
    /// MonoGame adapter for a content-managed model.
    /// </summary>
    public sealed class AdapterModel : IModel {
        private Model _model;
        private readonly BoundingSphere _sphereBounds;
        private readonly Aabb? _aabbBounds;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a model adapter.
        /// </summary>
        /// <param name="model">The native model.</param>
        public AdapterModel(Model model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            _model = model;
            _sphereBounds = CalculateBounds(model);
            _aabbBounds = CalculateBoundsAabb(model);
        }
        
        /// <summary>
        /// Gets the model-space conservative culling hull that is guaranteed to contain
        /// all model geometry: the platform mesh bounding spheres are transformed by
        /// their absolute bone transforms and merged. Exact dimensions are only
        /// available through <see cref="BoundsAabb"/>.
        /// </summary>
        public BoundingSphere BoundsBoundingSphere { get { return _sphereBounds; } }
        
        /// <summary>
        /// Gets the exact model-space axis-aligned bounding box, or null when the back
        /// end cannot compute it exactly. A null value never masquerades as a coarse
        /// approximation.
        /// </summary>
        public Aabb? BoundsAabb { get { return _aabbBounds; } }
        
        internal Model NativeModel {
            get {
                if (_model == null) {
                    throw new ObjectDisposedException(nameof(AdapterModel));
                }
                return _model;
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            _model = null;
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Computes the model-space conservative culling hull that is guaranteed to
        /// contain all model geometry. A mesh bounding sphere lives in its parent bone's
        /// local space, so it is carried into model space by the absolute bone transform
        /// of its mesh: the sphere center is transformed by that matrix and the radius is
        /// scaled by the matrix's largest axis scale. The transformed spheres are merged
        /// with the standard sphere merge. Exact dimensions are only available through
        /// BoundsAabb.
        /// </summary>
        private static BoundingSphere CalculateBounds(Model model) {
            bool flagAny = false;
            BoundingSphere sphereMerged = new BoundingSphere(System.Numerics.Vector3.Zero, 0f);
            Microsoft.Xna.Framework.Matrix[] arrayTransforms =
                new Microsoft.Xna.Framework.Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(arrayTransforms);
            foreach (ModelMesh mesh in model.Meshes) {
                Microsoft.Xna.Framework.BoundingSphere boundsNative = mesh.BoundingSphere;
                Microsoft.Xna.Framework.Matrix transformBone;
                if (mesh.ParentBone == null) {
                    transformBone = Microsoft.Xna.Framework.Matrix.Identity;
                } else {
                    transformBone = arrayTransforms[mesh.ParentBone.Index];
                }
                Microsoft.Xna.Framework.Vector3 centerModelSpace =
                    Microsoft.Xna.Framework.Vector3.Transform(boundsNative.Center, transformBone);
                BoundingSphere sphereModelSpace = new BoundingSphere(
                    new System.Numerics.Vector3(
                        centerModelSpace.X, centerModelSpace.Y, centerModelSpace.Z
                    ),
                    boundsNative.Radius * MaxAxisScale(transformBone)
                );
                if (!flagAny) {
                    sphereMerged = sphereModelSpace;
                    flagAny = true;
                } else {
                    sphereMerged = MergeSpheres(sphereMerged, sphereModelSpace);
                }
            }
            if (!flagAny) {
                return new BoundingSphere(System.Numerics.Vector3.Zero, 0f);
            }
            return sphereMerged;
        }
        
        /// <summary>
        /// Gets the largest axis scale of a bone transform.
        /// </summary>
        private static float MaxAxisScale(Microsoft.Xna.Framework.Matrix matrix) {
            float scale = matrix.Right.Length();
            float scaleY = matrix.Up.Length();
            float scaleZ = matrix.Down.Length();
            if (scaleY > scale) {
                scale = scaleY;
            }
            if (scaleZ > scale) {
                scale = scaleZ;
            }
            return scale;
        }
        
        /// <summary>
        /// Merges two spheres into the standard enclosing sphere: with the centers a
        /// distance d apart and r1 &gt;= r2, the merged center is center1 plus
        /// (d + r2 - r1) / (2d) along the direction from center1 to center2 and the
        /// merged radius is (d + r1 + r2) / 2; at d = 0 the larger sphere encloses the
        /// smaller.
        /// </summary>
        private static BoundingSphere MergeSpheres(
            BoundingSphere sphereFirst,
            BoundingSphere sphereSecond
        ) {
            System.Numerics.Vector3 direction = sphereSecond.Center - sphereFirst.Center;
            float distance = direction.Length();
            if (distance == 0f) {
                if (sphereFirst.Radius >= sphereSecond.Radius) {
                    return sphereFirst;
                }
                return sphereSecond;
            }
            float radius = (distance + sphereFirst.Radius + sphereSecond.Radius) * 0.5f;
            if (sphereFirst.Radius >= sphereSecond.Radius) {
                System.Numerics.Vector3 centerMerged = sphereFirst.Center
                    + direction * ((distance + sphereSecond.Radius - sphereFirst.Radius) / (2f * distance));
                return new BoundingSphere(centerMerged, radius);
            }
            System.Numerics.Vector3 centerMergedSecond = sphereSecond.Center
                + (sphereFirst.Center - sphereSecond.Center)
                    * ((distance + sphereFirst.Radius - sphereSecond.Radius) / (2f * distance));
            return new BoundingSphere(centerMergedSecond, radius);
        }
        
        /// <summary>
        /// Computes the model-space axis-aligned bounding box from the vertex positions of
        /// every mesh part, transformed by the owning bone. Exact or nothing: when the
        /// positions of a part cannot be read back (for example on back ends without
        /// buffer readback) or its position element is not a three-component float, the
        /// method returns null instead of substituting any approximation.
        /// </summary>
        private static Aabb? CalculateBoundsAabb(Model model) {
            bool flagAny = false;
            System.Numerics.Vector3 minimum = System.Numerics.Vector3.Zero;
            System.Numerics.Vector3 maximum = System.Numerics.Vector3.Zero;
            Microsoft.Xna.Framework.Matrix[] arrayTransforms =
                new Microsoft.Xna.Framework.Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(arrayTransforms);
            foreach (ModelMesh mesh in model.Meshes) {
                Microsoft.Xna.Framework.Matrix transformBone;
                if (mesh.ParentBone == null) {
                    transformBone = Microsoft.Xna.Framework.Matrix.Identity;
                } else {
                    transformBone = arrayTransforms[mesh.ParentBone.Index];
                }
                foreach (ModelMeshPart part in mesh.MeshParts) {
                    System.Numerics.Vector3[] arrayPosition;
                    if (!TryReadPositions(part, out arrayPosition)) {
                        return null;
                    }
                    for (int index = 0; index < arrayPosition.Length; index += 1) {
                        Microsoft.Xna.Framework.Vector3 positionTransformed =
                            Microsoft.Xna.Framework.Vector3.Transform(
                                new Microsoft.Xna.Framework.Vector3(
                                    arrayPosition[index].X,
                                    arrayPosition[index].Y,
                                    arrayPosition[index].Z
                                ),
                                transformBone
                            );
                        Merge(
                            new System.Numerics.Vector3(
                                positionTransformed.X,
                                positionTransformed.Y,
                                positionTransformed.Z
                            ),
                            ref flagAny, ref minimum, ref maximum
                        );
                    }
                }
            }
            if (!flagAny) {
                return new Aabb(System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero);
            }
            return new Aabb(minimum, maximum);
        }
        
        private static bool TryReadPositions(
            ModelMeshPart part,
            out System.Numerics.Vector3[] arrayPosition
        ) {
            arrayPosition = null;
            VertexBuffer bufferVertex = part.VertexBuffer;
            if (bufferVertex == null) {
                return false;
            }
            VertexDeclaration declarationVertex = bufferVertex.VertexDeclaration;
            int offsetPosition = -1;
            foreach (VertexElement element in declarationVertex.GetVertexElements()) {
                if (element.VertexElementUsage == VertexElementUsage.Position
                    && element.VertexElementFormat == VertexElementFormat.Vector3) {
                    offsetPosition = element.Offset;
                    break;
                }
            }
            if (offsetPosition < 0) {
                return false;
            }
            int stride = declarationVertex.VertexStride;
            int sizeBuffer = bufferVertex.VertexCount * stride;
            if (stride <= 0 || sizeBuffer < stride || offsetPosition + 12 > stride) {
                return false;
            }
            byte[] arrayBytes = new byte[sizeBuffer];
            try {
                bufferVertex.GetData<byte>(arrayBytes);
            } catch (NotSupportedException) {
                // A back end without vertex-buffer readback cannot supply exact positions.
                return false;
            } catch (NotImplementedException) {
                return false;
            }
            int countVertex = sizeBuffer / stride;
            arrayPosition = new System.Numerics.Vector3[countVertex];
            for (int index = 0; index < countVertex; index += 1) {
                int offset = index * stride + offsetPosition;
                arrayPosition[index] = new System.Numerics.Vector3(
                    BitConverter.ToSingle(arrayBytes, offset),
                    BitConverter.ToSingle(arrayBytes, offset + 4),
                    BitConverter.ToSingle(arrayBytes, offset + 8)
                );
            }
            return true;
        }
        
        private static void Merge(
            System.Numerics.Vector3 position,
            ref bool flagAny,
            ref System.Numerics.Vector3 minimum,
            ref System.Numerics.Vector3 maximum
        ) {
            if (!flagAny) {
                minimum = position;
                maximum = position;
                flagAny = true;
                return;
            }
            minimum = System.Numerics.Vector3.Min(minimum, position);
            maximum = System.Numerics.Vector3.Max(maximum, position);
        }
    }
}
