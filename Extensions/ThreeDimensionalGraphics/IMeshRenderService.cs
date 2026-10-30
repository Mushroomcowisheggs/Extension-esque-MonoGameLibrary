using System;
using System.Numerics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Queues three-dimensional assets and renders the queue during the host draw pass.
    /// </summary>
    public interface IMeshRenderService : IDisposable {
        /// <summary>
        /// Submits a mesh for the next draw pass.
        /// </summary>
        /// <param name="mesh">The mesh to submit.</param>
        /// <param name="matrixWorld">The world transform applied to the mesh.</param>
        void Submit(IMesh mesh, Matrix4x4 matrixWorld);
        
        /// <summary>
        /// Submits a model for the next draw pass.
        /// </summary>
        /// <param name="model">The model to submit.</param>
        /// <param name="matrixWorld">The world transform applied to the model.</param>
        void Submit(IModel model, Matrix4x4 matrixWorld);
        
        /// <summary>Updates renderer-owned frame state.</summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
        
        /// <summary>Draws and clears the submissions accumulated for the current frame.</summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Draw(FrameTime timeFrame);
    }
}
