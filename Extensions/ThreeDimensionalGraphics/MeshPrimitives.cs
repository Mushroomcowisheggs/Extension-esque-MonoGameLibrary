using System;
using System.Numerics;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Builds common primitive meshes (box, plane, cylinder, sphere, torus) in
    /// platform-neutral data with normals, texture coordinates and the shared clockwise
    /// front-face winding convention. The generated meshes are plain geometry and can be
    /// submitted to any adapter through <see cref="IMeshRenderService"/>.
    /// </summary>
    public static class MeshPrimitives {
        /// <summary>
        /// Creates a box centered on the origin, aligned to the axes.
        /// </summary>
        /// <param name="width">The extent along the X axis.</param>
        /// <param name="height">The extent along the Y axis.</param>
        /// <param name="depth">The extent along the Z axis.</param>
        /// <param name="material">The material, or null for the default material.</param>
        /// <returns>The box mesh.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an extent is not positive.</exception>
        public static Mesh CreateBox(
            float width,
            float height,
            float depth,
            MeshMaterial? material = null
        ) {
            if (width <= 0f || height <= 0f || depth <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(width), "Every extent must be positive."
                );
            }
            MeshBuilder builder = new MeshBuilder();
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float halfDepth = depth * 0.5f;
            AddBoxFace(builder, new Vector3(halfWidth, 0f, 0f), Vector3.UnitX,
                new Vector3(0f, 0f, -halfDepth), new Vector3(0f, halfHeight, 0f));
            AddBoxFace(builder, new Vector3(-halfWidth, 0f, 0f), -Vector3.UnitX,
                new Vector3(0f, 0f, halfDepth), new Vector3(0f, halfHeight, 0f));
            AddBoxFace(builder, new Vector3(0f, halfHeight, 0f), Vector3.UnitY,
                new Vector3(halfWidth, 0f, 0f), new Vector3(0f, 0f, halfDepth));
            AddBoxFace(builder, new Vector3(0f, -halfHeight, 0f), -Vector3.UnitY,
                new Vector3(halfWidth, 0f, 0f), new Vector3(0f, 0f, -halfDepth));
            AddBoxFace(builder, new Vector3(0f, 0f, halfDepth), Vector3.UnitZ,
                new Vector3(halfWidth, 0f, 0f), new Vector3(0f, halfHeight, 0f));
            AddBoxFace(builder, new Vector3(0f, 0f, -halfDepth), -Vector3.UnitZ,
                new Vector3(-halfWidth, 0f, 0f), new Vector3(0f, halfHeight, 0f));
            return new Mesh(builder.BuildMeshData(material));
        }
        
        /// <summary>
        /// Creates a horizontal plane in the XZ plane, centered on the origin, facing up.
        /// </summary>
        /// <param name="width">The extent along the X axis.</param>
        /// <param name="depth">The extent along the Z axis.</param>
        /// <param name="material">The material, or null for the default material.</param>
        /// <returns>The plane mesh.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an extent is not positive.</exception>
        public static Mesh CreatePlane(float width, float depth, MeshMaterial? material = null) {
            if (width <= 0f || depth <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(width), "Every extent must be positive."
                );
            }
            MeshBuilder builder = new MeshBuilder();
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;
            Vector3 normal = Vector3.UnitY;
            int indexFirst = builder.AddVertex(
                new Vector3(-halfWidth, 0f, -halfDepth), normal, new Vector2(0f, 0f));
            int indexSecond = builder.AddVertex(
                new Vector3(halfWidth, 0f, -halfDepth), normal, new Vector2(1f, 0f));
            int indexThird = builder.AddVertex(
                new Vector3(halfWidth, 0f, halfDepth), normal, new Vector2(1f, 1f));
            int indexFourth = builder.AddVertex(
                new Vector3(-halfWidth, 0f, halfDepth), normal, new Vector2(0f, 1f));
            builder.AddOrientedTriangle(indexFirst, indexSecond, indexThird);
            builder.AddOrientedTriangle(indexFirst, indexThird, indexFourth);
            return new Mesh(builder.BuildMeshData(material));
        }
        
        /// <summary>
        /// Creates a cylinder centered on the origin, extending along the Y axis,
        /// with side wall and end caps.
        /// </summary>
        /// <param name="radius">The radius of the end caps.</param>
        /// <param name="height">The extent along the Y axis.</param>
        /// <param name="segments">The number of segments around the circumference (3 or more).</param>
        /// <param name="material">The material, or null for the default material.</param>
        /// <returns>The cylinder mesh.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is out of range.</exception>
        public static Mesh CreateCylinder(
            float radius,
            float height,
            int segments,
            MeshMaterial? material = null
        ) {
            if (radius <= 0f || height <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(radius), "The radius and height must be positive."
                );
            }
            if (segments < 3) {
                throw new ArgumentOutOfRangeException(nameof(segments));
            }
            MeshBuilder builder = new MeshBuilder();
            float halfHeight = height * 0.5f;
            // Side wall: one ring of vertices per end.
            int[] arrayBottom = new int[segments];
            int[] arrayTop = new int[segments];
            for (int index = 0; index < segments; index += 1) {
                float angle = (float)(index * 2.0 * Math.PI / segments);
                float x = (float)Math.Cos(angle) * radius;
                float z = (float)Math.Sin(angle) * radius;
                Vector3 normal = new Vector3(x, 0f, z);
                normal = Vector3.Normalize(normal);
                float textureU = (float)index / segments;
                arrayBottom[index] = builder.AddVertex(
                    new Vector3(x, -halfHeight, z), normal, new Vector2(textureU, 1f));
                arrayTop[index] = builder.AddVertex(
                    new Vector3(x, halfHeight, z), normal, new Vector2(textureU, 0f));
            }
            for (int index = 0; index < segments; index += 1) {
                int indexNext = (index + 1) % segments;
                builder.AddOrientedTriangle(arrayBottom[index], arrayTop[index], arrayTop[indexNext]);
                builder.AddOrientedTriangle(arrayBottom[index], arrayTop[indexNext], arrayBottom[indexNext]);
            }
            // End caps: a center vertex fanned to each ring.
            int indexCenterTop = builder.AddVertex(
                new Vector3(0f, halfHeight, 0f), Vector3.UnitY, new Vector2(0.5f, 0.5f));
            int indexCenterBottom = builder.AddVertex(
                new Vector3(0f, -halfHeight, 0f), -Vector3.UnitY, new Vector2(0.5f, 0.5f));
            for (int index = 0; index < segments; index += 1) {
                int indexNext = (index + 1) % segments;
                builder.AddOrientedTriangle(indexCenterTop, arrayTop[indexNext], arrayTop[index]);
                builder.AddOrientedTriangle(indexCenterBottom, arrayBottom[index], arrayBottom[indexNext]);
            }
            return new Mesh(builder.BuildMeshData(material));
        }
        
        /// <summary>
        /// Creates a sphere centered on the origin.
        /// </summary>
        /// <param name="radius">The sphere radius.</param>
        /// <param name="segments">The number of segments around the equator (3 or more).</param>
        /// <param name="rings">The number of latitude rings (2 or more).</param>
        /// <param name="material">The material, or null for the default material.</param>
        /// <returns>The sphere mesh.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is out of range.</exception>
        public static Mesh CreateSphere(
            float radius,
            int segments,
            int rings,
            MeshMaterial? material = null
        ) {
            if (radius <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }
            if (segments < 3) {
                throw new ArgumentOutOfRangeException(nameof(segments));
            }
            if (rings < 2) {
                throw new ArgumentOutOfRangeException(nameof(rings));
            }
            MeshBuilder builder = new MeshBuilder();
            int[,] arrayIndices = new int[rings + 1, segments + 1];
            for (int ring = 0; ring <= rings; ring += 1) {
                float phi = (float)(ring * Math.PI / rings);
                float y = (float)Math.Cos(phi);
                float radiusRing = (float)Math.Sin(phi);
                for (int segment = 0; segment <= segments; segment += 1) {
                    float theta = (float)(segment * 2.0 * Math.PI / segments);
                    Vector3 normal = new Vector3(
                        (float)Math.Cos(theta) * radiusRing,
                        y,
                        (float)Math.Sin(theta) * radiusRing
                    );
                    arrayIndices[ring, segment] = builder.AddVertex(
                        normal * radius,
                        normal,
                        new Vector2(
                            (float)segment / segments,
                            (float)ring / rings
                        )
                    );
                }
            }
            for (int ring = 0; ring < rings; ring += 1) {
                for (int segment = 0; segment < segments; segment += 1) {
                    int indexTopLeft = arrayIndices[ring, segment];
                    int indexTopRight = arrayIndices[ring, segment + 1];
                    int indexBottomLeft = arrayIndices[ring + 1, segment];
                    int indexBottomRight = arrayIndices[ring + 1, segment + 1];
                    builder.AddOrientedTriangle(indexTopLeft, indexBottomLeft, indexBottomRight);
                    builder.AddOrientedTriangle(indexTopLeft, indexBottomRight, indexTopRight);
                }
            }
            return new Mesh(builder.BuildMeshData(material));
        }
        
        /// <summary>
        /// Creates a torus lying in the XZ plane, centered on the origin.
        /// </summary>
        /// <param name="radius">The distance from the origin to the tube center.</param>
        /// <param name="radiusTube">The radius of the tube (3 or more segments around it).</param>
        /// <param name="segments">The number of segments around the main ring (3 or more).</param>
        /// <param name="segmentsTube">The number of segments around the tube (3 or more).</param>
        /// <param name="material">The material, or null for the default material.</param>
        /// <returns>The torus mesh.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is out of range.</exception>
        public static Mesh CreateTorus(
            float radius,
            float radiusTube,
            int segments,
            int segmentsTube,
            MeshMaterial? material = null
        ) {
            if (radius <= 0f || radiusTube <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(radius), "Both radii must be positive."
                );
            }
            if (segments < 3) {
                throw new ArgumentOutOfRangeException(nameof(segments));
            }
            if (segmentsTube < 3) {
                throw new ArgumentOutOfRangeException(nameof(segmentsTube));
            }
            MeshBuilder builder = new MeshBuilder();
            int[,] arrayIndices = new int[segments + 1, segmentsTube + 1];
            for (int segment = 0; segment <= segments; segment += 1) {
                float angleMain = (float)(segment * 2.0 * Math.PI / segments);
                float cosMain = (float)Math.Cos(angleMain);
                float sinMain = (float)Math.Sin(angleMain);
                for (int segmentTube = 0; segmentTube <= segmentsTube; segmentTube += 1) {
                    float angleTube = (float)(segmentTube * 2.0 * Math.PI / segmentsTube);
                    float cosTube = (float)Math.Cos(angleTube);
                    float sinTube = (float)Math.Sin(angleTube);
                    Vector3 normal = new Vector3(cosMain * cosTube, sinTube, sinMain * cosTube);
                    Vector3 position = new Vector3(
                        cosMain * (radius + radiusTube * cosTube),
                        radiusTube * sinTube,
                        sinMain * (radius + radiusTube * cosTube)
                    );
                    arrayIndices[segment, segmentTube] = builder.AddVertex(
                        position,
                        normal,
                        new Vector2(
                            (float)segment / segments,
                            (float)segmentTube / segmentsTube
                        )
                    );
                }
            }
            for (int segment = 0; segment < segments; segment += 1) {
                for (int segmentTube = 0; segmentTube < segmentsTube; segmentTube += 1) {
                    int indexFirst = arrayIndices[segment, segmentTube];
                    int indexSecond = arrayIndices[segment, segmentTube + 1];
                    int indexThird = arrayIndices[segment + 1, segmentTube + 1];
                    int indexFourth = arrayIndices[segment + 1, segmentTube];
                    builder.AddOrientedTriangle(indexFirst, indexSecond, indexThird);
                    builder.AddOrientedTriangle(indexFirst, indexThird, indexFourth);
                }
            }
            return new Mesh(builder.BuildMeshData(material));
        }
        
        private static void AddBoxFace(
            MeshBuilder builder,
            Vector3 center,
            Vector3 normal,
            Vector3 directionU,
            Vector3 directionV
        ) {
            int indexFirst = builder.AddVertex(
                center - directionU - directionV, normal, new Vector2(0f, 1f));
            int indexSecond = builder.AddVertex(
                center + directionU - directionV, normal, new Vector2(1f, 1f));
            int indexThird = builder.AddVertex(
                center + directionU + directionV, normal, new Vector2(1f, 0f));
            int indexFourth = builder.AddVertex(
                center - directionU + directionV, normal, new Vector2(0f, 0f));
            builder.AddOrientedTriangle(indexFirst, indexSecond, indexThird);
            builder.AddOrientedTriangle(indexFirst, indexThird, indexFourth);
        }
    }
}
