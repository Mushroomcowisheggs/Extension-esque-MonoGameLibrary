using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Fna.ThreeDimensionalGraphics {
    /// <summary>
    /// FNA implementation of the three-dimensional camera contract.
    /// </summary>
    public sealed class ThreeDimensionalCameraService : IThreeDimensionalCameraService {
        private readonly GraphicsDevice _deviceGraphics;
        private readonly ThreadAccess _accessThread = new ThreadAccess();
        private readonly object _lock = new object();
        private System.Numerics.Vector3 _position;
        private System.Numerics.Quaternion _orientation;
        private float _radianFieldOfView;
        private float _ratioAspect;
        private float _distanceNearPlane;
        private float _distanceFarPlane;
        private System.Numerics.Matrix4x4 _matrixView;
        private System.Numerics.Matrix4x4 _matrixProjection;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a camera that follows the graphics device viewport aspect ratio.
        /// </summary>
        /// <param name="deviceGraphics">The graphics device that owns the camera viewport.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="deviceGraphics"/> is null.</exception>
        public ThreeDimensionalCameraService(GraphicsDevice deviceGraphics) {
            if (deviceGraphics == null) {
                throw new ArgumentNullException(nameof(deviceGraphics));
            }
            _deviceGraphics = deviceGraphics;
            _position = new System.Numerics.Vector3(0f, 0f, 5f);
            _orientation = System.Numerics.Quaternion.Identity;
            _radianFieldOfView = MathF.PI / 4f;
            _ratioAspect = 1f;
            _distanceNearPlane = 0.1f;
            _distanceFarPlane = 1000f;
            _matrixView = System.Numerics.Matrix4x4.Identity;
            _matrixProjection = System.Numerics.Matrix4x4.Identity;
            RebuildMatricesLocked();
        }
        
        /// <inheritdoc />
        public System.Numerics.Vector3 Position {
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
        public System.Numerics.Quaternion Orientation {
            get { lock (_lock) { return _orientation; } }
            set {
                float length = value.Length();
                if (length <= float.Epsilon) {
                    throw new ArgumentException("Orientation must have a non-zero length.", nameof(value));
                }
                lock (_lock) {
                    ThrowIfDisposedLocked();
                    _orientation = System.Numerics.Quaternion.Normalize(value);
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public float FieldOfViewRadians {
            get { lock (_lock) { return _radianFieldOfView; } }
            set {
                if (value <= 0f || value >= MathF.PI) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock (_lock) {
                    ThrowIfDisposedLocked();
                    _radianFieldOfView = value;
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
        public System.Numerics.Matrix4x4 ViewMatrix {
            get { lock (_lock) { return _matrixView; } }
        }
        
        /// <inheritdoc />
        public System.Numerics.Matrix4x4 ProjectionMatrix {
            get { lock (_lock) { return _matrixProjection; } }
        }
        
        /// <inheritdoc />
        public ThreeDimensionalCameraState CameraState {
            get {
                lock (_lock) {
                    return new ThreeDimensionalCameraState(
                        _position,
                        _orientation,
                        _ratioAspect,
                        _matrixView,
                        _matrixProjection
                    );
                }
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            _accessThread.VerifyAccess();
            lock (_lock) {
                ThrowIfDisposedLocked();
                Viewport viewport = _deviceGraphics.Viewport;
                if (viewport.Width > 0 && viewport.Height > 0) {
                    _ratioAspect = (float)viewport.Width / (float)viewport.Height;
                    RebuildMatricesLocked();
                }
            }
        }
        
        /// <inheritdoc />
        public void Draw(FrameTime timeFrame) {
            _accessThread.VerifyAccess();
            lock (_lock) {
                ThrowIfDisposedLocked();
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
            System.Numerics.Vector3 forward = System.Numerics.Vector3.Transform(-System.Numerics.Vector3.UnitZ, _orientation);
            System.Numerics.Vector3 up = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, _orientation);
            System.Numerics.Vector3 target = _position + forward;
            Vector3 positionNative = ToNative(_position);
            Vector3 targetNative = ToNative(target);
            Vector3 upNative = ToNative(up);
            Matrix matrixViewNative = Matrix.CreateLookAt(positionNative, targetNative, upNative);
            Matrix matrixProjectionNative = Matrix.CreatePerspectiveFieldOfView(
                _radianFieldOfView,
                _ratioAspect,
                _distanceNearPlane,
                _distanceFarPlane
            );
            _matrixView = ToNeutral(matrixViewNative);
            _matrixProjection = ToNeutral(matrixProjectionNative);
        }
        
        private void ThrowIfDisposedLocked() {
            if (_flagDisposed) {
                throw new ObjectDisposedException(nameof(ThreeDimensionalCameraService));
            }
        }
        
        private static Vector3 ToNative(System.Numerics.Vector3 value) {
            return new Vector3(value.X, value.Y, value.Z);
        }
        
        private static System.Numerics.Matrix4x4 ToNeutral(Matrix value) {
            return new System.Numerics.Matrix4x4(
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44
            );
        }
    }
}
