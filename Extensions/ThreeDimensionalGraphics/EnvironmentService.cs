using System;
using System.Numerics;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Plain in-memory <see cref="IEnvironmentService"/> holding the description until an
    /// adapter applies it. Usable on every adapter.
    /// </summary>
    public sealed class EnvironmentService : IEnvironmentService {
        private readonly object _lock = new object();
        private bool _flagEnabled;
        private Vector3 _directionSun;
        private Color _colorSun;
        private float _intensitySun;
        private Color _colorAmbient;
        private bool _flagShadows;
        private Color _colorBackground;
        
        /// <summary>
        /// Initializes a disabled environment with a warm sun, a cool ambient term and a
        /// cornflower background.
        /// </summary>
        public EnvironmentService() {
            _flagEnabled = false;
            _directionSun = Vector3.Normalize(new Vector3(-0.5265408f, -0.5735765f, -0.6275069f));
            _colorSun = new Color(255, 245, 206);
            _intensitySun = 1f;
            _colorAmbient = new Color(14, 25, 46);
            _flagShadows = false;
            _colorBackground = Color.CornflowerBlue;
        }
        
        /// <inheritdoc />
        public bool FlagEnabled {
            get {
                lock (_lock) {
                    return _flagEnabled;
                }
            }
            set {
                lock (_lock) {
                    _flagEnabled = value;
                }
            }
        }
        
        /// <inheritdoc />
        public Vector3 SunDirection {
            get {
                lock (_lock) {
                    return _directionSun;
                }
            }
            set {
                if (value.LengthSquared() == 0f) {
                    throw new ArgumentException(
                        "The sun direction must not be the zero vector.", nameof(value)
                    );
                }
                lock (_lock) {
                    _directionSun = value;
                }
            }
        }
        
        /// <inheritdoc />
        public Color SunColor {
            get {
                lock (_lock) {
                    return _colorSun;
                }
            }
            set {
                lock (_lock) {
                    _colorSun = value;
                }
            }
        }
        
        /// <inheritdoc />
        public float SunIntensity {
            get {
                lock (_lock) {
                    return _intensitySun;
                }
            }
            set {
                if (value < 0f) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock (_lock) {
                    _intensitySun = value;
                }
            }
        }
        
        /// <inheritdoc />
        public Color AmbientColor {
            get {
                lock (_lock) {
                    return _colorAmbient;
                }
            }
            set {
                lock (_lock) {
                    _colorAmbient = value;
                }
            }
        }
        
        /// <inheritdoc />
        public bool FlagShadows {
            get {
                lock (_lock) {
                    return _flagShadows;
                }
            }
            set {
                lock (_lock) {
                    _flagShadows = value;
                }
            }
        }
        
        /// <inheritdoc />
        public Color BackgroundColor {
            get {
                lock (_lock) {
                    return _colorBackground;
                }
            }
            set {
                lock (_lock) {
                    _colorBackground = value;
                }
            }
        }
    }
}
