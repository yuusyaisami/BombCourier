using BC.Base;
using UnityEngine;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class FaceMeshExpressionPresenterMB : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private MeshFilter meshFilter;

        [Header("UV Set")]
        [SerializeField] private FaceExpressionUvSet uvSet;

        private ValueStoreService valueStore;
        private EntityRef entityRef;

        private Mesh runtimeMesh;
        private Vector2[] sourceUvs;
        private Vector2[] workingUvs;
        private Rect sourceUvRect;

        private bool hasLastExpression;
        private FaceExpressionId lastExpression;

        private void Reset()
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }

        private void Awake()
        {
            if (skinnedMeshRenderer == null)
                skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (uvSet == null)
            {
                Debug.LogError($"{nameof(FaceMeshExpressionPresenterMB)}: UV Set is not assigned.", this);
                enabled = false;
                return;
            }

            Mesh sourceMesh = GetSourceMesh();

            if (sourceMesh == null)
            {
                Debug.LogError($"{nameof(FaceMeshExpressionPresenterMB)}: Mesh is missing.", this);
                enabled = false;
                return;
            }

            runtimeMesh = Instantiate(sourceMesh);
            runtimeMesh.name = $"{sourceMesh.name}_RuntimeFaceExpression";

            AssignRuntimeMesh(runtimeMesh);

            sourceUvs = runtimeMesh.uv;

            if (sourceUvs == null || sourceUvs.Length == 0)
            {
                Debug.LogError($"{nameof(FaceMeshExpressionPresenterMB)}: Mesh has no UV.", this);
                enabled = false;
                return;
            }

            workingUvs = new Vector2[sourceUvs.Length];

            sourceUvRect = CalculateUvBounds(sourceUvs);

            ResolveValueStore();

            ApplyExpression(FaceExpressionId.Neutral);
        }

        private void Update()
        {
            if (valueStore == null || !entityRef.IsValid)
                return;

            FaceExpressionId expression = valueStore.Get(entityRef, ValueKeys.Runtime.FaceExpression);

            if (hasLastExpression && expression == lastExpression)
                return;

            hasLastExpression = true;
            lastExpression = expression;

            ApplyExpression(expression);
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

        private void ApplyExpression(FaceExpressionId expression)
        {
            if (!uvSet.TryGetExpressionUvRect(expression, out Rect targetUvRect))
            {
                Debug.LogWarning(
                    $"{nameof(FaceMeshExpressionPresenterMB)}: UV rect is not registered. Expression={expression}. Fallback to Neutral.",
                    this);

                if (!uvSet.TryGetExpressionUvRect(FaceExpressionId.Neutral, out targetUvRect))
                {
                    Debug.LogError($"{nameof(FaceMeshExpressionPresenterMB)}: Neutral UV rect is not registered.", this);
                    return;
                }
            }

            RemapUvs(sourceUvRect, targetUvRect);
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

        private void ResolveValueStore()
        {
            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null || kernelMB.Kernel == null || kernelMB.Kernel.ValueStore == null)
            {
                Debug.LogError($"{nameof(FaceMeshExpressionPresenterMB)}: ValueStore is not found.", this);
                enabled = false;
                return;
            }

            valueStore = kernelMB.Kernel.ValueStore;

            EntityMB entityMB = GetComponentInParent<EntityMB>();

            if (entityMB == null || !entityMB.HasEntity)
            {
                Debug.LogError($"{nameof(FaceMeshExpressionPresenterMB)}: EntityMB is not found or not bound.", this);
                enabled = false;
                return;
            }

            entityRef = entityMB.Entity;
        }

        private void OnDestroy()
        {
            if (runtimeMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(runtimeMesh);
            else
                DestroyImmediate(runtimeMesh);
        }
    }
}