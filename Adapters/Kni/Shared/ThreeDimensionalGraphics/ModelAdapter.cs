using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Kni.ThreeDimensionalGraphics {
    /// <summary>
    /// KNI adapter for a content-managed model.
    /// </summary>
    public sealed class ModelAdapter : IModel {
        private Model _model;
        private readonly BoundingSphere _sphereBounds;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a model adapter.
        /// </summary>
        /// <param name="model">The native model.</param>
        public ModelAdapter(Model model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            _model = model;
            _sphereBounds = CalculateBounds(model);
        }
        
        /// <inheritdoc />
        public BoundingSphere Bounds { get { return _sphereBounds; } }
        
        internal Model NativeModel {
            get {
                if (_model == null) {
                    throw new ObjectDisposedException(nameof(ModelAdapter));
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
        
        private static BoundingSphere CalculateBounds(Model model) {
            float radius = 0f;
            foreach (ModelMesh mesh in model.Meshes) {
                Microsoft.Xna.Framework.BoundingSphere boundsNative = mesh.BoundingSphere;
                System.Numerics.Vector3 center = new System.Numerics.Vector3(
                    boundsNative.Center.X,
                    boundsNative.Center.Y,
                    boundsNative.Center.Z
                );
                float distance = center.Length() + boundsNative.Radius;
                if (distance > radius) {
                    radius = distance;
                }
            }
            return new BoundingSphere(System.Numerics.Vector3.Zero, radius);
        }
    }
}
