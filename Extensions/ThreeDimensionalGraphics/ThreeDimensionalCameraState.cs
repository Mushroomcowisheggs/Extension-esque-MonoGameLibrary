using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// An immutable camera state captured for one rendering frame.
    /// </summary>
    public readonly struct ThreeDimensionalCameraState {
        /// <summary>
        /// Initializes a camera state.
        /// </summary>
        /// <param name="position">The camera position.</param>
        /// <param name="orientation">The camera orientation.</param>
        /// <param name="aspectRatio">The projection aspect ratio.</param>
        /// <param name="matrixView">The view matrix.</param>
        /// <param name="matrixProjection">The projection matrix.</param>
        public ThreeDimensionalCameraState(
            Vector3 position,
            Quaternion orientation,
            float aspectRatio,
            Matrix4x4 matrixView,
            Matrix4x4 matrixProjection
        ) {
            Position = position;
            Orientation = orientation;
            AspectRatio = aspectRatio;
            ViewMatrix = matrixView;
            ProjectionMatrix = matrixProjection;
        }
        
        /// <summary>Gets the camera position.</summary>
        public Vector3 Position { get; }
        
        /// <summary>Gets the camera orientation.</summary>
        public Quaternion Orientation { get; }
        
        /// <summary>Gets the projection aspect ratio.</summary>
        public float AspectRatio { get; }
        
        /// <summary>Gets the view matrix.</summary>
        public Matrix4x4 ViewMatrix { get; }
        
        /// <summary>Gets the projection matrix.</summary>
        public Matrix4x4 ProjectionMatrix { get; }
    }
}
