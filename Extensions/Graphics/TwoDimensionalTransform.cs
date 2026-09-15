using System;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// The pure data 2D transform applied to every draw call of a render block.
    /// It is a value type so that hot draw loops can build one without allocating.
    /// Composition order is scale, then rotation, then translation.
    /// </summary>
    public readonly struct TwoDimensionalTransform {
        private readonly float _scaleX;
        private readonly float _scaleY;
        private readonly float _rotation;
        private readonly float _translationX;
        private readonly float _translationY;
        
        /// <summary>Gets the horizontal scale factor.</summary>
        public float ScaleX {
            get {
                return _scaleX;
            }
        }
        
        /// <summary>Gets the vertical scale factor.</summary>
        public float ScaleY {
            get {
                return _scaleY;
            }
        }
        
        /// <summary>Gets the rotation in radians around the origin of the render block.</summary>
        public float Rotation {
            get {
                return _rotation;
            }
        }
        
        /// <summary>Gets the horizontal translation applied after scale and rotation.</summary>
        public float TranslationX {
            get {
                return _translationX;
            }
        }
        
        /// <summary>Gets the vertical translation applied after scale and rotation.</summary>
        public float TranslationY {
            get {
                return _translationY;
            }
        }
        
        /// <summary>Gets the transform that maps every coordinate onto itself.</summary>
        public static TwoDimensionalTransform Identity {
            get {
                return new TwoDimensionalTransform(1f, 1f, 0f, 0f, 0f);
            }
        }
        
        /// <summary>Initializes a transform from explicit components.</summary>
        /// <param name="scaleX">The horizontal scale factor. Must be finite.</param>
        /// <param name="scaleY">The vertical scale factor. Must be finite.</param>
        /// <param name="rotation">The rotation in radians. Must be finite.</param>
        /// <param name="translationX">The horizontal translation. Must be finite.</param>
        /// <param name="translationY">The vertical translation. Must be finite.</param>
        /// <exception cref="ArgumentException">Thrown when any component is not a finite number.</exception>
        public TwoDimensionalTransform(float scaleX, float scaleY, float rotation, float translationX, float translationY) {
            if (!float.IsFinite(scaleX)) {
                throw new ArgumentException("The horizontal scale must be a finite number.", nameof(scaleX));
            }
            if (!float.IsFinite(scaleY)) {
                throw new ArgumentException("The vertical scale must be a finite number.", nameof(scaleY));
            }
            if (!float.IsFinite(rotation)) {
                throw new ArgumentException("The rotation must be a finite number.", nameof(rotation));
            }
            if (!float.IsFinite(translationX)) {
                throw new ArgumentException("The horizontal translation must be a finite number.", nameof(translationX));
            }
            if (!float.IsFinite(translationY)) {
                throw new ArgumentException("The vertical translation must be a finite number.", nameof(translationY));
            }
            _scaleX = scaleX;
            _scaleY = scaleY;
            _rotation = rotation;
            _translationX = translationX;
            _translationY = translationY;
        }
        
        /// <summary>Creates a transform that only scales.</summary>
        /// <param name="scaleX">The horizontal scale factor.</param>
        /// <param name="scaleY">The vertical scale factor.</param>
        /// <returns>The resulting transform.</returns>
        public static TwoDimensionalTransform CreateScale(float scaleX, float scaleY) {
            return new TwoDimensionalTransform(scaleX, scaleY, 0f, 0f, 0f);
        }
        
        /// <summary>Creates a transform that only translates.</summary>
        /// <param name="translationX">The horizontal translation.</param>
        /// <param name="translationY">The vertical translation.</param>
        /// <returns>The resulting transform.</returns>
        public static TwoDimensionalTransform CreateTranslation(float translationX, float translationY) {
            return new TwoDimensionalTransform(1f, 1f, 0f, translationX, translationY);
        }
        
        /// <summary>Creates a transform that scales first and then translates.</summary>
        /// <param name="scaleX">The horizontal scale factor.</param>
        /// <param name="scaleY">The vertical scale factor.</param>
        /// <param name="translationX">The horizontal translation.</param>
        /// <param name="translationY">The vertical translation.</param>
        /// <returns>The resulting transform.</returns>
        public static TwoDimensionalTransform CreateScaleTranslation(float scaleX, float scaleY, float translationX, float translationY) {
            return new TwoDimensionalTransform(scaleX, scaleY, 0f, translationX, translationY);
        }
    }
}