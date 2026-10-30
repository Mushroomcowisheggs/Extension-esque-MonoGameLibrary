using System;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// A platform-neutral view frustum extracted from a view-projection matrix.
    /// </summary>
    public sealed class Frustum : IFrustum {
        private readonly Plane[] _planes;
        
        /// <summary>
        /// Initializes a frustum from a view-projection matrix.
        /// </summary>
        /// <param name="matrixViewProjection">The combined view-projection matrix.</param>
        public Frustum(Matrix4x4 matrixViewProjection) {
            _planes = new Plane[] {
                Normalize(new Plane(
                    matrixViewProjection.M14 + matrixViewProjection.M11,
                    matrixViewProjection.M24 + matrixViewProjection.M21,
                    matrixViewProjection.M34 + matrixViewProjection.M31,
                    matrixViewProjection.M44 + matrixViewProjection.M41
                )),
                Normalize(new Plane(
                    matrixViewProjection.M14 - matrixViewProjection.M11,
                    matrixViewProjection.M24 - matrixViewProjection.M21,
                    matrixViewProjection.M34 - matrixViewProjection.M31,
                    matrixViewProjection.M44 - matrixViewProjection.M41
                )),
                Normalize(new Plane(
                    matrixViewProjection.M14 + matrixViewProjection.M12,
                    matrixViewProjection.M24 + matrixViewProjection.M22,
                    matrixViewProjection.M34 + matrixViewProjection.M32,
                    matrixViewProjection.M44 + matrixViewProjection.M42
                )),
                Normalize(new Plane(
                    matrixViewProjection.M14 - matrixViewProjection.M12,
                    matrixViewProjection.M24 - matrixViewProjection.M22,
                    matrixViewProjection.M34 - matrixViewProjection.M32,
                    matrixViewProjection.M44 - matrixViewProjection.M42
                )),
                Normalize(new Plane(
                    matrixViewProjection.M14 + matrixViewProjection.M13,
                    matrixViewProjection.M24 + matrixViewProjection.M23,
                    matrixViewProjection.M34 + matrixViewProjection.M33,
                    matrixViewProjection.M44 + matrixViewProjection.M43
                )),
                Normalize(new Plane(
                    matrixViewProjection.M14 - matrixViewProjection.M13,
                    matrixViewProjection.M24 - matrixViewProjection.M23,
                    matrixViewProjection.M34 - matrixViewProjection.M33,
                    matrixViewProjection.M44 - matrixViewProjection.M43
                ))
            };
        }
        
        /// <inheritdoc />
        public bool Intersects(BoundingSphere sphereBounds) {
            foreach (Plane plane in _planes) {
                float distance = Vector3.Dot(plane.Normal, sphereBounds.Center) + plane.D;
                if (distance < -sphereBounds.Radius) {
                    return false;
                }
            }
            return true;
        }
        
        private static Plane Normalize(Plane plane) {
            float length = plane.Normal.Length();
            if (length <= float.Epsilon) {
                return plane;
            }
            return new Plane(plane.Normal / length, plane.D / length);
        }
    }
}
