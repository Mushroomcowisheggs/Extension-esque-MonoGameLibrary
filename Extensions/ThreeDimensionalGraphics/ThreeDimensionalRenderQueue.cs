using System;
using System.Collections.Generic;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral submission queue and frustum culling for three-dimensional drawing.
    /// </summary>
    public sealed class ThreeDimensionalRenderQueue {
        private readonly object _lock = new object();
        private readonly List<ThreeDimensionalSubmission> _listSubmissions =
            new List<ThreeDimensionalSubmission>();
        
        /// <summary>
        /// Submits a mesh for the next draw pass.
        /// </summary>
        /// <param name="mesh">The mesh to submit.</param>
        /// <param name="matrixWorld">The world transform.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mesh"/> is null.</exception>
        public void Submit(IMesh mesh, Matrix4x4 matrixWorld) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            lock (_lock) {
                _listSubmissions.Add(new ThreeDimensionalSubmission(mesh, matrixWorld));
            }
        }
        
        /// <summary>
        /// Submits a mesh with an explicit material override for the next draw pass.
        /// </summary>
        /// <param name="mesh">The mesh to submit.</param>
        /// <param name="matrixWorld">The world transform.</param>
        /// <param name="material">The material to render the mesh with.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mesh"/> is null.</exception>
        public void Submit(IMesh mesh, Matrix4x4 matrixWorld, MeshMaterial material) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            lock (_lock) {
                _listSubmissions.Add(new ThreeDimensionalSubmission(mesh, matrixWorld, material));
            }
        }
        
        /// <summary>
        /// Submits a model for the next draw pass.
        /// </summary>
        /// <param name="model">The model to submit.</param>
        /// <param name="matrixWorld">The world transform.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is null.</exception>
        public void Submit(IModel model, Matrix4x4 matrixWorld) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            lock (_lock) {
                _listSubmissions.Add(new ThreeDimensionalSubmission(model, matrixWorld));
            }
        }
        
        /// <summary>
        /// Submits a model with an explicit material override for the next draw pass.
        /// </summary>
        /// <param name="model">The model to submit.</param>
        /// <param name="matrixWorld">The world transform.</param>
        /// <param name="material">The material to render all parts with.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is null.</exception>
        public void Submit(IModel model, Matrix4x4 matrixWorld, MeshMaterial material) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            lock (_lock) {
                _listSubmissions.Add(new ThreeDimensionalSubmission(model, matrixWorld, material));
            }
        }
        
        /// <summary>
        /// Takes the submissions that intersect the view frustum and clears the queue.
        /// A submission whose asset supplies an exact bounds box is tested with the
        /// world-transformed box; a submission whose asset reports no exact box falls
        /// back to the conservative bounding-sphere test.
        /// </summary>
        /// <param name="matrixViewProjection">The combined view-projection matrix.</param>
        /// <returns>The visible submissions for this draw pass.</returns>
        public List<ThreeDimensionalSubmission> TakeVisible(Matrix4x4 matrixViewProjection) {
            List<ThreeDimensionalSubmission> listPending;
            lock (_lock) {
                listPending = new List<ThreeDimensionalSubmission>(_listSubmissions);
                _listSubmissions.Clear();
            }
            List<ThreeDimensionalSubmission> listVisible = new List<ThreeDimensionalSubmission>();
            if (listPending.Count == 0) {
                return listVisible;
            }
            Frustum frustum = new Frustum(matrixViewProjection);
            foreach (ThreeDimensionalSubmission submission in listPending) {
                Aabb? aabbBounds;
                if (submission.FlagModel) {
                    aabbBounds = submission.Model.BoundsAabb;
                } else {
                    aabbBounds = submission.Mesh.Data.BoundsAabb;
                }
                if (aabbBounds == null) {
                    // No exact box: fall back to the conservative sphere test.
                    BoundingSphere sphereBounds;
                    if (submission.FlagModel) {
                        sphereBounds = submission.Model.BoundsBoundingSphere;
                    } else {
                        sphereBounds = submission.Mesh.Data.BoundsBoundingSphere;
                    }
                    Vector3 center = Vector3.Transform(sphereBounds.Center, submission.WorldMatrix);
                    float radius = sphereBounds.Radius * MaxScale(submission.WorldMatrix);
                    if (frustum.Intersects(new BoundingSphere(center, radius))) {
                        listVisible.Add(submission);
                    }
                } else {
                    Aabb aabbWorld = TransformAabb(aabbBounds.Value, submission.WorldMatrix);
                    if (frustum.Intersects(aabbWorld)) {
                        listVisible.Add(submission);
                    }
                }
            }
            return listVisible;
        }
        
        /// <summary>
        /// Takes every pending submission without frustum culling and clears the queue.
        /// Diagnostics and self-checks use it to assert culling behaviour by comparing
        /// the visible set against the full submitted set.
        /// </summary>
        /// <returns>All submissions queued for this draw pass.</returns>
        public List<ThreeDimensionalSubmission> TakeAll() {
            List<ThreeDimensionalSubmission> listPending;
            lock (_lock) {
                listPending = new List<ThreeDimensionalSubmission>(_listSubmissions);
                _listSubmissions.Clear();
            }
            return listPending;
        }
        
        /// <summary>
        /// Discards all pending submissions.
        /// </summary>
        public void Clear() {
            lock (_lock) {
                _listSubmissions.Clear();
            }
        }
        
        private static float MaxScale(Matrix4x4 matrix) {
            float scaleX = new Vector3(matrix.M11, matrix.M12, matrix.M13).Length();
            float scaleY = new Vector3(matrix.M21, matrix.M22, matrix.M23).Length();
            float scaleZ = new Vector3(matrix.M31, matrix.M32, matrix.M33).Length();
            return MathF.Max(scaleX, MathF.Max(scaleY, scaleZ));
        }
        
        /// <summary>
        /// Transforms the eight corners of a model-space box by the world matrix and
        /// returns the box enclosing the transformed corners.
        /// </summary>
        /// <param name="aabbLocal">The model-space box.</param>
        /// <param name="matrixWorld">The world transform.</param>
        /// <returns>The enclosing world-space box.</returns>
        private static Aabb TransformAabb(Aabb aabbLocal, Matrix4x4 matrixWorld) {
            Vector3 minimum = Vector3.Transform(aabbLocal.Minimum, matrixWorld);
            Vector3 maximum = minimum;
            for (int corner = 1; corner < 8; corner += 1) {
                Vector3 positionCorner = new Vector3(
                    aabbLocal.Minimum.X,
                    aabbLocal.Minimum.Y,
                    aabbLocal.Minimum.Z
                );
                if ((corner & 1) != 0) {
                    positionCorner.X = aabbLocal.Maximum.X;
                }
                if ((corner & 2) != 0) {
                    positionCorner.Y = aabbLocal.Maximum.Y;
                }
                if ((corner & 4) != 0) {
                    positionCorner.Z = aabbLocal.Maximum.Z;
                }
                Vector3 positionTransformed = Vector3.Transform(positionCorner, matrixWorld);
                minimum = Vector3.Min(minimum, positionTransformed);
                maximum = Vector3.Max(maximum, positionTransformed);
            }
            return new Aabb(minimum, maximum);
        }
    }
}
