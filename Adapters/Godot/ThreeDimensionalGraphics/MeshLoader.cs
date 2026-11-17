using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;
using GodotMesh = global::Godot.Mesh;
using GodotArrayMesh = global::Godot.ArrayMesh;
using GodotArray = global::Godot.Collections.Array;
using ExtensionMesh = MonoGameLibrary.Extensions.ThreeDimensionalGraphics.Mesh;

namespace MonoGameLibrary.Adapters.Godot.ThreeDimensionalGraphics {
    /// <summary>
    /// Loads a platform-neutral mesh from a Godot <see cref="GodotMesh"/> resource (an imported
    /// or constructed <see cref="GodotArrayMesh"/>). All surfaces are merged into one triangle
    /// list, which the render path consumes as a single vertex buffer. Triangle strips are
    /// expanded; lines and points cannot be represented and are rejected. Winding is preserved
    /// unchanged; the render path culls by Godot's default clockwise front faces.
    /// </summary>
    public sealed class MeshLoader : IMeshLoader {
        /// <inheritdoc />
        public IMesh Load(IContentBackend backendContent, string nameAsset) {
            if (backendContent == null) {
                throw new ArgumentNullException(nameof(backendContent));
            }
            GodotMesh meshNative = backendContent.LoadNative<GodotMesh>(nameAsset);
            List<MeshVertex> listVertices = new List<MeshVertex>();
            List<int> listIndices = new List<int>();
            int countSurfaces = meshNative.GetSurfaceCount();
            for (int indexSurface = 0; indexSurface < countSurfaces; indexSurface += 1) {
                GodotArrayMesh arrayMeshNative = meshNative as GodotArrayMesh;
                GodotMesh.PrimitiveType typePrimitive = arrayMeshNative != null
                    ? arrayMeshNative.SurfaceGetPrimitiveType(indexSurface)
                    : GodotMesh.PrimitiveType.Triangles;
                if (typePrimitive != GodotMesh.PrimitiveType.Triangles &&
                    typePrimitive != GodotMesh.PrimitiveType.TriangleStrip) {
                    throw new NotSupportedException(
                        $"A Godot mesh surface with topology {typePrimitive} cannot be " +
                        "represented as the contract's merged triangle list."
                    );
                }
                GodotArray arrays = meshNative.SurfaceGetArrays(indexSurface);
                Vector3[] arrayPosition = arrays[(int)GodotMesh.ArrayType.Vertex].As<Vector3[]>();
                if (arrayPosition == null || arrayPosition.Length == 0) {
                    continue;
                }
                Vector3[] arrayNormal = arrays[(int)GodotMesh.ArrayType.Normal].As<Vector3[]>();
                Vector2[] arrayTexture = arrays[(int)GodotMesh.ArrayType.TexUV].As<Vector2[]>();
                int[] arrayIndex = arrays[(int)GodotMesh.ArrayType.Index].As<int[]>();
                if (arrayIndex == null || arrayIndex.Length == 0) {
                    arrayIndex = new int[arrayPosition.Length];
                    for (int index = 0; index < arrayIndex.Length; index += 1) {
                        arrayIndex[index] = index;
                    }
                }
                int offsetBase = listVertices.Count;
                for (int index = 0; index < arrayPosition.Length; index += 1) {
                    Vector3 position = arrayPosition[index];
                    Vector3 normal = arrayNormal != null && index < arrayNormal.Length
                        ? arrayNormal[index]
                        : Vector3.Zero;
                    Vector2 texture = arrayTexture != null && index < arrayTexture.Length
                        ? arrayTexture[index]
                        : Vector2.Zero;
                    listVertices.Add(new MeshVertex(
                        new System.Numerics.Vector3(position.X, position.Y, position.Z),
                        new System.Numerics.Vector3(normal.X, normal.Y, normal.Z),
                        new System.Numerics.Vector2(texture.X, texture.Y)
                    ));
                }
                if (typePrimitive == GodotMesh.PrimitiveType.Triangles) {
                    for (int index = 0; index < arrayIndex.Length; index += 1) {
                        listIndices.Add(offsetBase + arrayIndex[index]);
                    }
                } else {
                    for (int index = 0; index + 2 < arrayIndex.Length; index += 1) {
                        if ((index % 2) == 0) {
                            listIndices.Add(offsetBase + arrayIndex[index]);
                            listIndices.Add(offsetBase + arrayIndex[index + 1]);
                            listIndices.Add(offsetBase + arrayIndex[index + 2]);
                        } else {
                            listIndices.Add(offsetBase + arrayIndex[index + 1]);
                            listIndices.Add(offsetBase + arrayIndex[index]);
                            listIndices.Add(offsetBase + arrayIndex[index + 2]);
                        }
                    }
                }
            }
            if (listVertices.Count == 0 || listIndices.Count == 0) {
                throw new InvalidOperationException("The model contains no renderable mesh data.");
            }
            return new ExtensionMesh(new MeshData(
                listVertices.ToArray(),
                listIndices.ToArray(),
                MeshPrimitiveType.TriangleList
            ));
        }
    }
}
