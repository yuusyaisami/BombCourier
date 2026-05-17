using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Utility
{
    // 三角形ごとに頂点を複製して UV4 に barycentric を書き込み、ESL の EdgeOnly 表示で辺を安定して出す。
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class MeshBarycentricEdgeMB : MonoBehaviour
    {
        [SerializeField] private MeshFilter targetMeshFilter;
        [SerializeField] private bool rebuildInEditMode = true;

        [SerializeField, HideInInspector] private Mesh sourceMesh;
        [SerializeField, HideInInspector] private Mesh generatedMesh;

        public Mesh SourceMesh => sourceMesh;
        public bool HasGeneratedMesh => generatedMesh != null;

        private void Reset()
        {
            targetMeshFilter = GetComponent<MeshFilter>();
            CaptureSourceMesh();
        }

        private void OnEnable()
        {
            EnsureMeshFilter();
            CaptureSourceMesh();
            RebuildGeneratedMesh();
        }

        private void OnDisable()
        {
            RestoreSourceMesh();
            ReleaseGeneratedMesh();
        }

        private void OnDestroy()
        {
            RestoreSourceMesh();
            ReleaseGeneratedMesh();
        }

        private void OnValidate()
        {
            EnsureMeshFilter();
            CaptureSourceMesh();

            if (!rebuildInEditMode || !isActiveAndEnabled || Application.isPlaying)
            {
                return;
            }

            RebuildGeneratedMesh();
        }

        [ContextMenu("Rebuild Barycentric Mesh")]
        public void RebuildGeneratedMesh()
        {
            EnsureMeshFilter();
            CaptureSourceMesh();

            if (targetMeshFilter == null || sourceMesh == null)
            {
                return;
            }

            Mesh newMesh = BuildBarycentricMesh(sourceMesh);
            newMesh.hideFlags = HideFlags.HideAndDontSave;

            ReleaseGeneratedMesh();
            generatedMesh = newMesh;
            targetMeshFilter.sharedMesh = generatedMesh;
        }

        [ContextMenu("Restore Source Mesh")]
        public void RestoreSourceMesh()
        {
            if (targetMeshFilter == null || sourceMesh == null || generatedMesh == null)
            {
                return;
            }

            if (targetMeshFilter.sharedMesh == generatedMesh)
            {
                targetMeshFilter.sharedMesh = sourceMesh;
            }
        }

        private void EnsureMeshFilter()
        {
            if (targetMeshFilter == null)
            {
                targetMeshFilter = GetComponent<MeshFilter>();
            }
        }

        private void CaptureSourceMesh()
        {
            if (targetMeshFilter == null)
            {
                return;
            }

            Mesh currentMesh = targetMeshFilter.sharedMesh;
            if (currentMesh != null && currentMesh != generatedMesh)
            {
                sourceMesh = currentMesh;
            }
        }

        private void ReleaseGeneratedMesh()
        {
            if (generatedMesh == null)
            {
                return;
            }

            Mesh meshToRelease = generatedMesh;
            generatedMesh = null;

            if (Application.isPlaying)
            {
                Destroy(meshToRelease);
            }
            else
            {
                DestroyImmediate(meshToRelease);
            }
        }

        private static Mesh BuildBarycentricMesh(Mesh sourceMesh)
        {
            Vector3[] sourceVertices = sourceMesh.vertices;
            Vector3[] sourceNormals = sourceMesh.normals;
            Vector4[] sourceTangents = sourceMesh.tangents;
            Color32[] sourceColors = sourceMesh.colors32;

            List<Vector2> uv0 = new List<Vector2>();
            List<Vector2> uv1 = new List<Vector2>();
            List<Vector2> uv2 = new List<Vector2>();
            sourceMesh.GetUVs(0, uv0);
            sourceMesh.GetUVs(1, uv1);
            sourceMesh.GetUVs(2, uv2);

            int totalIndexCount = 0;
            for (int subMeshIndex = 0; subMeshIndex < sourceMesh.subMeshCount; subMeshIndex++)
            {
                totalIndexCount += sourceMesh.GetTriangles(subMeshIndex).Length;
            }

            List<Vector3> rebuiltVertices = new List<Vector3>(totalIndexCount);
            List<Vector3> rebuiltNormals = sourceNormals != null && sourceNormals.Length == sourceVertices.Length
                ? new List<Vector3>(totalIndexCount)
                : null;
            List<Vector4> rebuiltTangents = sourceTangents != null && sourceTangents.Length == sourceVertices.Length
                ? new List<Vector4>(totalIndexCount)
                : null;
            List<Color32> rebuiltColors = sourceColors != null && sourceColors.Length == sourceVertices.Length
                ? new List<Color32>(totalIndexCount)
                : null;
            List<Vector2> rebuiltUv0 = uv0.Count == sourceVertices.Length ? new List<Vector2>(totalIndexCount) : null;
            List<Vector2> rebuiltUv1 = uv1.Count == sourceVertices.Length ? new List<Vector2>(totalIndexCount) : null;
            List<Vector2> rebuiltUv2 = uv2.Count == sourceVertices.Length ? new List<Vector2>(totalIndexCount) : null;
            List<Vector3> rebuiltBarycentric = new List<Vector3>(totalIndexCount);

            List<int[]> rebuiltSubMeshTriangles = new List<int[]>(sourceMesh.subMeshCount);

            for (int subMeshIndex = 0; subMeshIndex < sourceMesh.subMeshCount; subMeshIndex++)
            {
                int[] sourceTriangles = sourceMesh.GetTriangles(subMeshIndex);
                int[] rebuiltTriangles = new int[sourceTriangles.Length];

                for (int triangleIndex = 0; triangleIndex < sourceTriangles.Length; triangleIndex += 3)
                {
                    int rebuiltStart = rebuiltVertices.Count;
                    AppendVertex(sourceTriangles[triangleIndex], new Vector3(1f, 0f, 0f));
                    AppendVertex(sourceTriangles[triangleIndex + 1], new Vector3(0f, 1f, 0f));
                    AppendVertex(sourceTriangles[triangleIndex + 2], new Vector3(0f, 0f, 1f));

                    rebuiltTriangles[triangleIndex] = rebuiltStart;
                    rebuiltTriangles[triangleIndex + 1] = rebuiltStart + 1;
                    rebuiltTriangles[triangleIndex + 2] = rebuiltStart + 2;
                }

                rebuiltSubMeshTriangles.Add(rebuiltTriangles);
            }

            Mesh rebuiltMesh = new Mesh
            {
                name = sourceMesh.name + "_Barycentric"
            };
            rebuiltMesh.indexFormat = rebuiltVertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            rebuiltMesh.SetVertices(rebuiltVertices);

            if (rebuiltNormals != null)
            {
                rebuiltMesh.SetNormals(rebuiltNormals);
            }

            if (rebuiltTangents != null)
            {
                rebuiltMesh.SetTangents(rebuiltTangents);
            }

            if (rebuiltColors != null)
            {
                rebuiltMesh.SetColors(rebuiltColors);
            }

            if (rebuiltUv0 != null)
            {
                rebuiltMesh.SetUVs(0, rebuiltUv0);
            }

            if (rebuiltUv1 != null)
            {
                rebuiltMesh.SetUVs(1, rebuiltUv1);
            }

            if (rebuiltUv2 != null)
            {
                rebuiltMesh.SetUVs(2, rebuiltUv2);
            }

            rebuiltMesh.SetUVs(3, rebuiltBarycentric);
            rebuiltMesh.subMeshCount = rebuiltSubMeshTriangles.Count;

            for (int subMeshIndex = 0; subMeshIndex < rebuiltSubMeshTriangles.Count; subMeshIndex++)
            {
                rebuiltMesh.SetTriangles(rebuiltSubMeshTriangles[subMeshIndex], subMeshIndex, false);
            }

            if (rebuiltNormals == null)
            {
                rebuiltMesh.RecalculateNormals();
            }

            if (rebuiltTangents == null)
            {
                rebuiltMesh.RecalculateTangents();
            }

            rebuiltMesh.bounds = sourceMesh.bounds;
            return rebuiltMesh;

            void AppendVertex(int sourceIndex, Vector3 barycentric)
            {
                rebuiltVertices.Add(sourceVertices[sourceIndex]);

                if (rebuiltNormals != null)
                {
                    rebuiltNormals.Add(sourceNormals[sourceIndex]);
                }

                if (rebuiltTangents != null)
                {
                    rebuiltTangents.Add(sourceTangents[sourceIndex]);
                }

                if (rebuiltColors != null)
                {
                    rebuiltColors.Add(sourceColors[sourceIndex]);
                }

                if (rebuiltUv0 != null)
                {
                    rebuiltUv0.Add(uv0[sourceIndex]);
                }

                if (rebuiltUv1 != null)
                {
                    rebuiltUv1.Add(uv1[sourceIndex]);
                }

                if (rebuiltUv2 != null)
                {
                    rebuiltUv2.Add(uv2[sourceIndex]);
                }

                rebuiltBarycentric.Add(barycentric);
            }
        }
    }
}