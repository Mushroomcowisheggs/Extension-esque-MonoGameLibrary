using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Fna.ThreeDimensionalGraphics {
    /// <summary>
    /// Loads a platform-neutral mesh from a FNA model whose vertex declaration is
    /// <see cref="VertexPositionNormalTexture"/>.
    /// </summary>
    public sealed class MeshLoader : IMeshLoader {
        /// <inheritdoc />
        public IMesh Load(IContentBackend backendContent, string nameAsset) {
            if (backendContent == null) {
                throw new ArgumentNullException(nameof(backendContent));
            }
            Model model = backendContent.LoadNative<Model>(nameAsset);
            List<MeshVertex> listVertices = new List<MeshVertex>();
            List<int> listIndices = new List<int>();
            foreach (ModelMesh meshModel in model.Meshes) {
                foreach (ModelMeshPart part in meshModel.MeshParts) {
                    VertexPositionNormalTexture[] arrayNative =
                        new VertexPositionNormalTexture[part.VertexBuffer.VertexCount];
                    part.VertexBuffer.GetData(arrayNative);
                    int offsetBase = listVertices.Count;
                    for (int index = 0; index < part.NumVertices; index += 1) {
                        VertexPositionNormalTexture vertex = arrayNative[part.VertexOffset + index];
                        listVertices.Add(new MeshVertex(
                            new System.Numerics.Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z),
                            new System.Numerics.Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z),
                            new System.Numerics.Vector2(vertex.TextureCoordinate.X, vertex.TextureCoordinate.Y)
                        ));
                    }
                    int countIndex = part.PrimitiveCount * 3;
                    if (part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits) {
                        short[] arrayIndex = new short[part.IndexBuffer.IndexCount];
                        part.IndexBuffer.GetData(arrayIndex);
                        for (int index = 0; index < countIndex; index += 1) {
                            listIndices.Add(offsetBase + arrayIndex[part.StartIndex + index]);
                        }
                    } else {
                        int[] arrayIndex = new int[part.IndexBuffer.IndexCount];
                        part.IndexBuffer.GetData(arrayIndex);
                        for (int index = 0; index < countIndex; index += 1) {
                            listIndices.Add(offsetBase + arrayIndex[part.StartIndex + index]);
                        }
                    }
                }
            }
            if (listVertices.Count == 0 || listIndices.Count == 0) {
                throw new InvalidOperationException("The model contains no renderable mesh data.");
            }
            return new Mesh(new MeshData(
                listVertices.ToArray(),
                listIndices.ToArray(),
                MeshPrimitiveType.TriangleList
            ));
        }
    }
}
