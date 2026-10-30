using System;
using System.Numerics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Provides a platform-neutral three-dimensional camera state and frame lifecycle.
    /// </summary>
    public interface IThreeDimensionalCameraService : IDisposable {
        /// <summary>Gets or sets the camera position.</summary>
        Vector3 Position { get; set; }
        
        /// <summary>Gets or sets the camera orientation.</summary>
        Quaternion Orientation { get; set; }
        
        /// <summary>Gets or sets the vertical field of view in radians.</summary>
        float FieldOfViewRadians { get; set; }
        
        /// <summary>Gets the current projection aspect ratio.</summary>
        float AspectRatio { get; }
        
        /// <summary>Gets or sets the near clipping distance.</summary>
        float NearPlaneDistance { get; set; }
        
        /// <summary>Gets or sets the far clipping distance.</summary>
        float FarPlaneDistance { get; set; }
        
        /// <summary>Gets the current view matrix.</summary>
        Matrix4x4 ViewMatrix { get; }
        
        /// <summary>Gets the current projection matrix.</summary>
        Matrix4x4 ProjectionMatrix { get; }
        
        /// <summary>Gets an atomic snapshot of the current camera state.</summary>
        ThreeDimensionalCameraState CameraState { get; }
        
        /// <summary>Updates the camera state for the current frame.</summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
        
        /// <summary>Publishes the camera state for the current draw pass.</summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Draw(FrameTime timeFrame);
    }
}
