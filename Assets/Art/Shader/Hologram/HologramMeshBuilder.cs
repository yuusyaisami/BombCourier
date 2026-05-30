using System;
using UnityEngine;

namespace BombCourier.Rendering.Hologram
{
    /// <summary>
    /// Builds a mesh suitable for hologram rendering by duplicating each triangle
    /// and encoding barycentric coordinates into the vertex colors. The generated
    /// mesh contains unique vertices per triangle so that wireframe edges can be
    /// detected in a shader via barycentric interpolation. This class does not
    /// mutate the source mesh.
    /// </summary>
    public static class HologramMeshBuilder
    {
        /// <summary>
        /// Converts a source mesh into a hologram mesh with barycentric coordinates.
        /// </summary>
        /// <param name="sourceMesh">The original mesh to convert. Must be non-null.</param>
        /// <returns>A new mesh with duplicated vertices and barycentric colors, or null if no mesh was provided.</returns>
        public static Mesh Build(Mesh sourceMesh)
        {
            if (sourceMesh == null)
            {
                Debug.LogWarning("HologramMeshBuilder.Build called with null sourceMesh");
                return null;
            }

            // Unity meshes may contain multiple submeshes. Flatten all submesh indices
            // into a single index list so the hologram is treated as one transparent object.
            int totalIndexCount = 0;
            for (int sub = 0; sub < sourceMesh.subMeshCount; sub++)
            {
                var desc = sourceMesh.GetSubMesh(sub);
                totalIndexCount += (int)desc.indexCount;
            }

            // Collect indices from all submeshes sequentially
            int[] srcIndices = new int[totalIndexCount];
            int destOffset = 0;
            for (int sub = 0; sub < sourceMesh.subMeshCount; sub++)
            {
                int[] subIndices = sourceMesh.GetIndices(sub);
                Array.Copy(subIndices, 0, srcIndices, destOffset, subIndices.Length);
                destOffset += subIndices.Length;
            }

            // Source attributes
            Vector3[] srcVertices = sourceMesh.vertices;
            Vector3[] srcNormals = sourceMesh.normals;
            Vector4[] srcTangents = sourceMesh.tangents;
            Vector2[] srcUV = sourceMesh.uv;

            int triangleCount = srcIndices.Length / 3;
            int vertexCount = triangleCount * 3;

            // Prepare output arrays
            Vector3[] outVertices = new Vector3[vertexCount];
            Vector3[] outNormals = srcNormals != null && srcNormals.Length > 0 ? new Vector3[vertexCount] : null;
            Vector4[] outTangents = srcTangents != null && srcTangents.Length > 0 ? new Vector4[vertexCount] : null;
            Vector2[] outUV = srcUV != null && srcUV.Length > 0 ? new Vector2[vertexCount] : null;
            Color[] outColors = new Color[vertexCount];
            int[] outIndices = new int[vertexCount];

            // For each triangle, duplicate its vertices and assign barycentric coordinates
            for (int i = 0; i < triangleCount; i++)
            {
                int srcIndex0 = srcIndices[i * 3 + 0];
                int srcIndex1 = srcIndices[i * 3 + 1];
                int srcIndex2 = srcIndices[i * 3 + 2];

                int destIndex0 = i * 3 + 0;
                int destIndex1 = i * 3 + 1;
                int destIndex2 = i * 3 + 2;

                // Copy positions
                outVertices[destIndex0] = srcVertices[srcIndex0];
                outVertices[destIndex1] = srcVertices[srcIndex1];
                outVertices[destIndex2] = srcVertices[srcIndex2];

                // Copy normals if available
                if (outNormals != null)
                {
                    outNormals[destIndex0] = srcNormals[srcIndex0];
                    outNormals[destIndex1] = srcNormals[srcIndex1];
                    outNormals[destIndex2] = srcNormals[srcIndex2];
                }

                // Copy tangents if available
                if (outTangents != null)
                {
                    outTangents[destIndex0] = srcTangents[srcIndex0];
                    outTangents[destIndex1] = srcTangents[srcIndex1];
                    outTangents[destIndex2] = srcTangents[srcIndex2];
                }

                // Copy UVs if available
                if (outUV != null)
                {
                    outUV[destIndex0] = srcUV[srcIndex0];
                    outUV[destIndex1] = srcUV[srcIndex1];
                    outUV[destIndex2] = srcUV[srcIndex2];
                }

                // Assign barycentric coordinates to vertex colors
                outColors[destIndex0] = new Color(1f, 0f, 0f, 1f);
                outColors[destIndex1] = new Color(0f, 1f, 0f, 1f);
                outColors[destIndex2] = new Color(0f, 0f, 1f, 1f);

                // Build index list sequentially
                outIndices[destIndex0] = destIndex0;
                outIndices[destIndex1] = destIndex1;
                outIndices[destIndex2] = destIndex2;
            }

            var hologramMesh = new Mesh
            {
                name = sourceMesh.name + "_Hologram"
            };

            // If vertex count exceeds UInt16 limit, switch to 32-bit indices
            if (vertexCount > 65535)
            {
                hologramMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            hologramMesh.vertices = outVertices;
            if (outNormals != null) hologramMesh.normals = outNormals;
            if (outTangents != null) hologramMesh.tangents = outTangents;
            if (outUV != null) hologramMesh.uv = outUV;
            hologramMesh.colors = outColors;
            hologramMesh.SetIndices(outIndices, MeshTopology.Triangles, 0);
            hologramMesh.RecalculateBounds();

            return hologramMesh;
        }
    }
}