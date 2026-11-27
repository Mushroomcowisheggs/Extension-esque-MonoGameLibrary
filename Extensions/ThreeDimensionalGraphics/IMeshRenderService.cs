using System;
using System.Numerics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Queues three-dimensional assets and renders the queue during the host draw pass.
    /// </summary>
    /// <remarks>
    /// Mesh and model implementations are supplied by the selected platform adapter; the renderer
    /// does not accept arbitrary third-party implementations. The draw pass owns vertex-buffer
    /// binding, so a subsequent pass must bind its own buffers.
    /// </remarks>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IMeshRenderService : IDisposable {
        /// <summary>
        /// Submits a mesh for the next draw pass. The mesh renders with its own material,
        /// or with the default material when it carries none.
        /// </summary>
        /// <param name="mesh">The mesh to submit.</param>
        /// <param name="matrixWorld">The world transform applied to the mesh.</param>
        void Submit(IMesh mesh, Matrix4x4 matrixWorld);
        
        /// <summary>
        /// Submits a mesh for the next draw pass with an explicit material that overrides
        /// the mesh's own material for this submission.
        /// </summary>
        /// <param name="mesh">The mesh to submit.</param>
        /// <param name="matrixWorld">The world transform applied to the mesh.</param>
        /// <param name="material">The material to render the mesh with.</param>
        void Submit(IMesh mesh, Matrix4x4 matrixWorld, MeshMaterial material);
        
        /// <summary>
        /// Submits a model for the next draw pass. Model parts render with the materials
        /// of their imported surfaces.
        /// </summary>
        /// <param name="model">The model to submit.</param>
        /// <param name="matrixWorld">The world transform applied to the model.</param>
        void Submit(IModel model, Matrix4x4 matrixWorld);
        
        /// <summary>
        /// Submits a model for the next draw pass with an explicit material that overrides
        /// every part's surface material for this submission.
        /// </summary>
        /// <param name="model">The model to submit.</param>
        /// <param name="matrixWorld">The world transform applied to the model.</param>
        /// <param name="material">The material to render all parts with.</param>
        void Submit(IModel model, Matrix4x4 matrixWorld, MeshMaterial material);
        
        /// <summary>Draws and clears the submissions accumulated for the current frame.</summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        [ThreadCondition(ThreadAffinity.Frame)]
        void Draw(FrameTime timeFrame);
    }
}
