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
        /// Takes the submissions that intersect the view frustum and clears the queue.
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
            }
            return listVisible;
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
    }
}
