using System;
using System.Numerics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral three-dimensional camera state and matrix construction.
    /// </summary>
    public sealed class ThreeDimensionalCameraService : IThreeDimensionalCameraService {
        private readonly IThreeDimensionalViewport _viewport;
        private readonly object _lock = new object();
        private Vector3 _position;
        private Quaternion _orientation;
        private float _radiansFieldOfView;
        private float _ratioAspect;
        private float _distanceNearPlane;
        private float _distanceFarPlane;
        private Matrix4x4 _matrixView;
        private Matrix4x4 _matrixProjection;
        private ThreeDimensionalCameraState _stateCamera;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a camera over the supplied viewport.
        /// </summary>
        /// <param name="viewport">The viewport that supplies the current aspect ratio.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewport"/> is null.</exception>
        public ThreeDimensionalCameraService(IThreeDimensionalViewport viewport) {
            if (viewport == null) {
                throw new ArgumentNullException(nameof(viewport));
            }
            _viewport = viewport;
            _position = new Vector3(0f, 0f, 5f);
            _orientation = Quaternion.Identity;
            _radiansFieldOfView = MathF.PI / 4f;
            _ratioAspect = 1f;
            _distanceNearPlane = 0.1f;
            _distanceFarPlane = 1000f;
            _matrixView = Matrix4x4.Identity;
            _matrixProjection = Matrix4x4.Identity;
            RebuildMatricesLocked();
            _stateCamera = new ThreeDimensionalCameraState(
                _position,
                _orientation,
                _ratioAspect,
                _matrixView,
                _matrixProjection
            );
        }
        
        /// <inheritdoc />
        public Vector3 Position {
            get { lock (_lock) { return _position; } }
            set {
                lock (_lock) {
                    ThrowIfDisposedLocked();
                    _position = value;
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public Quaternion Orientation {
            get { lock (_lock) { return _orientation; } }
            set {
                float length = value.Length();
                if (length <= float.Epsilon) {
                    throw new ArgumentException("Orientation must have a non-zero length.", nameof(value));
                }
                lock (_lock) {
                    ThrowIfDisposedLocked();
                    _orientation = Quaternion.Normalize(value);
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public float FieldOfViewRadians {
            get { lock (_lock) { return _radiansFieldOfView; } }
            set {
                if (value <= 0f || value >= MathF.PI) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock (_lock) {
                    ThrowIfDisposedLocked();
                    _radiansFieldOfView = value;
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public float AspectRatio {
            get { lock (_lock) { return _ratioAspect; } }
        }
        
        /// <inheritdoc />
        public float NearPlaneDistance {
            get { lock (_lock) { return _distanceNearPlane; } }
            set {
                if (value <= 0f) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock (_lock) {
                    ThrowIfDisposedLocked();
                    if (value >= _distanceFarPlane) {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }
                    _distanceNearPlane = value;
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public float FarPlaneDistance {
            get { lock (_lock) { return _distanceFarPlane; } }
            set {
                lock (_lock) {
                    ThrowIfDisposedLocked();
                    if (value <= _distanceNearPlane) {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }
                    _distanceFarPlane = value;
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public Matrix4x4 ViewMatrix {
            get { lock (_lock) { return _matrixView; } }
        }
        
        /// <inheritdoc />
        public Matrix4x4 ProjectionMatrix {
            get { lock (_lock) { return _matrixProjection; } }
        }
        
        /// <inheritdoc />
        public ThreeDimensionalCameraState CameraState {
            get {
                lock (_lock) {
                    return _stateCamera;
                }
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                ThrowIfDisposedLocked();
                float ratioAspect = _viewport.AspectRatio;
                if (ratioAspect > 0f && ratioAspect != _ratioAspect) {
                    _ratioAspect = ratioAspect;
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            lock (_lock) {
                ThrowIfDisposedLocked();
                _stateCamera = new ThreeDimensionalCameraState(
                    _position,
                    _orientation,
                    _ratioAspect,
                    _matrixView,
                    _matrixProjection
                );
            }
        }
        
        /// <inheritdoc />
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
        
        private void RebuildMatricesLocked() {
            Vector3 directionForward = Vector3.Transform(-Vector3.UnitZ, _orientation);
            Vector3 directionUp = Vector3.Transform(Vector3.UnitY, _orientation);
            Vector3 positionTarget = _position + directionForward;
            _matrixView = Matrix4x4.CreateLookAt(_position, positionTarget, directionUp);
            _matrixProjection = Matrix4x4.CreatePerspectiveFieldOfView(
                _radiansFieldOfView,
                _ratioAspect,
                _distanceNearPlane,
                _distanceFarPlane
            );
        }
        
        private void ThrowIfDisposedLocked() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(ThreeDimensionalCameraService));
            }
        }
    }
}
