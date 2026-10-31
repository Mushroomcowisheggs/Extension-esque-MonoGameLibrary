using System;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// One mesh or model submission waiting for the next three-dimensional draw pass.
    /// </summary>
    public readonly struct ThreeDimensionalSubmission {
        /// <summary>
        /// Initializes a mesh submission.
        /// </summary>
        /// <param name="mesh">The mesh to draw.</param>
        /// <param name="matrixWorld">The world transform.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mesh"/> is null.</exception>
        public ThreeDimensionalSubmission(IMesh mesh, Matrix4x4 matrixWorld) {
            if (mesh == null) {
                throw new ArgumentNullException(nameof(mesh));
            }
            Mesh = mesh;
            Model = null;
            WorldMatrix = matrixWorld;
        }
        
        /// <summary>
        /// Initializes a model submission.
        /// </summary>
        /// <param name="model">The model to draw.</param>
        /// <param name="matrixWorld">The world transform.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is null.</exception>
        public ThreeDimensionalSubmission(IModel model, Matrix4x4 matrixWorld) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Mesh = null;
            Model = model;
            WorldMatrix = matrixWorld;
        }
        
        /// <summary>Gets the submitted mesh, or null for a model submission.</summary>
        public IMesh Mesh { get; }
        
        /// <summary>Gets the submitted model, or null for a mesh submission.</summary>
        public IModel Model { get; }
        
        /// <summary>Gets the world transform.</summary>
        public Matrix4x4 WorldMatrix { get; }
        
        /// <summary>Gets a value indicating whether this is a model submission.</summary>
        public bool FlagModel {
            get { return Model != null; }
        }
    }
}
