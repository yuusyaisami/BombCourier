using UnityEngine;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class MeshUvRectRemapperMB : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private MeshFilter meshFilter;

        private Mesh sourceMesh;
        private Mesh runtimeMesh;
        private Vector2[] sourceUvs;
        private Vector2[] workingUvs;
        private Rect sourceUvRect;
        private bool initialized;

        public bool IsInitialized => initialized;
        public Rect SourceUvRect => sourceUvRect;

        private void Reset()
        {
            ResolveRendererReferences();
        }

        private void OnValidate()
        {
            ResolveRendererReferences();
        }

        private void Awake()
        {
            Initialize();
        }

        public bool TryApplyUvRect(Rect targetUvRect)
        {
            if (!Initialize())
                return false;

            RemapUvs(sourceUvRect, targetUvRect);
            return true;
        }

        public void RestoreSourceUvs()
        {
            if (!Initialize())
                return;

            runtimeMesh.uv = sourceUvs;
        }

        private void ResolveRendererReferences()
        {
            if (skinnedMeshRenderer == null)
                skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
        }

        private bool Initialize()
        {
            if (initialized)
                return true;

            ResolveRendererReferences();

            sourceMesh = GetSourceMesh();

            if (sourceMesh == null)
            {
                Debug.LogError($"{nameof(MeshUvRectRemapperMB)}: Mesh is missing.", this);
                enabled = false;
                return false;
            }

            Vector2[] initialUvs = sourceMesh.uv;

            if (initialUvs == null || initialUvs.Length == 0)
            {
                Debug.LogError($"{nameof(MeshUvRectRemapperMB)}: Mesh has no UV.", this);
                enabled = false;
                return false;
            }

            // Shared mesh を直接触ると prefab/asset 全体へ漏れるので、必ず instance を複製してから UV を差し替える。
            runtimeMesh = Instantiate(sourceMesh);
            runtimeMesh.name = $"{sourceMesh.name}_RuntimeUvRemap";

            AssignRuntimeMesh(runtimeMesh);

            sourceUvs = (Vector2[])initialUvs.Clone();
            workingUvs = new Vector2[sourceUvs.Length];
            sourceUvRect = CalculateUvBounds(sourceUvs);
            initialized = true;
            return true;
        }

        private Mesh GetSourceMesh()
        {
            if (skinnedMeshRenderer != null)
                return skinnedMeshRenderer.sharedMesh;

            if (meshFilter != null)
                return meshFilter.sharedMesh;

            return null;
        }

        private void AssignRuntimeMesh(Mesh mesh)
        {
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.sharedMesh = mesh;
                return;
            }

            if (meshFilter != null)
            {
                meshFilter.sharedMesh = mesh;
            }
        }

        private static Rect CalculateUvBounds(Vector2[] uvs)
        {
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < uvs.Length; i++)
            {
                Vector2 uv = uvs[i];

                if (uv.x < minX) minX = uv.x;
                if (uv.y < minY) minY = uv.y;
                if (uv.x > maxX) maxX = uv.x;
                if (uv.y > maxY) maxY = uv.y;
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private void RemapUvs(Rect sourceRect, Rect targetRect)
        {
            float sourceWidth = Mathf.Max(0.0001f, sourceRect.width);
            float sourceHeight = Mathf.Max(0.0001f, sourceRect.height);

            for (int i = 0; i < sourceUvs.Length; i++)
            {
                Vector2 uv = sourceUvs[i];

                float normalizedU = (uv.x - sourceRect.xMin) / sourceWidth;
                float normalizedV = (uv.y - sourceRect.yMin) / sourceHeight;

                workingUvs[i] = new Vector2(
                    targetRect.xMin + normalizedU * targetRect.width,
                    targetRect.yMin + normalizedV * targetRect.height
                );
            }

            runtimeMesh.uv = workingUvs;
        }

        private void OnDestroy()
        {
            if (runtimeMesh == null)
                return;

            AssignRuntimeMesh(sourceMesh);

            if (Application.isPlaying)
                Destroy(runtimeMesh);
            else
                DestroyImmediate(runtimeMesh);
        }
    }
}