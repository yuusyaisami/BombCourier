using System.Collections.Generic;
using UnityEngine;

namespace BC.Utility
{
    /// <summary>
    /// LineRenderer の表示色と発光を shader ごとに吸収して制御するランタイム向けコンポーネントです。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LineRendererVisualControllerMB : MonoBehaviour
    {
        private enum ShaderFamily
        {
            Unknown = 0,
            TrailUnlit = 1,
            EnvironmentStylizedLit = 2,
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int SimpleBoostEnabledId = Shader.PropertyToID("_SimpleBoostEmissionEnabled");
        private static readonly int SimpleBoostColorId = Shader.PropertyToID("_SimpleBoostEmissionColor");
        private static readonly int SimpleBoostIntensityId = Shader.PropertyToID("_SimpleBoostEmissionIntensity");

        private const float EnvironmentEmissionStrengthMax = 10.0f;
        private const float TrailEmissionStrengthMax = 16.0f;
        private const float SimpleBoostEmissionStrengthMax = 25.0f;

        [Header("Target")]
        [Tooltip("制御対象の LineRenderer です。未設定時は必要に応じて自動生成します。")]
        [SerializeField] private LineRenderer targetRenderer;

        [Tooltip("targetRenderer が未設定の時に子オブジェクトを自動生成します。")]
        [SerializeField] private bool autoCreateRenderer = true;

        [Tooltip("自動生成する LineRenderer オブジェクト名です。")]
        [SerializeField] private string autoCreatedRendererName = "Runtime Line Visual";

        [Header("Material")]
        [Tooltip("LineRenderer に Material がない時に使う fallback です。")]
        [SerializeField] private Material fallbackMaterial;

        [Header("Emission")]
        [Tooltip("発光制御を有効化します。")]
        [SerializeField] private bool enableEmission = true;

        [Tooltip("発光色です。")]
        [SerializeField] private Color emissionColor = Color.white;

        [Tooltip("有効状態の発光強度です。")]
        [SerializeField, Min(0.0f)] private float activeEmissionStrength = 2.0f;

        [Tooltip("無効状態の発光強度です。")]
        [SerializeField, Min(0.0f)] private float inactiveEmissionStrength;

        [Tooltip("EnvironmentStylizedLit 使用時に SimpleBoostEmission も同期します。")]
        [SerializeField] private bool syncEnvironmentSimpleBoost = true;

        [Tooltip("有効状態の SimpleBoost 発光強度です。")]
        [SerializeField, Min(0.0f)] private float activeSimpleBoostIntensity = 4.0f;

        [Tooltip("無効状態の SimpleBoost 発光強度です。")]
        [SerializeField, Min(0.0f)] private float inactiveSimpleBoostIntensity;

        [Header("Inactive Style")]
        [Tooltip("無効状態のラインを薄く表示します。")]
        [SerializeField] private bool dimInactiveLine = true;

        [Tooltip("無効状態で乗算するアルファです。")]
        [SerializeField, Range(0.0f, 1.0f)] private float inactiveAlphaMultiplier = 0.35f;

        private MaterialPropertyBlock propertyBlock;

        private Material runtimeMaterial;
        private Material ownedFallbackMaterial;
        private ShaderFamily shaderFamily = ShaderFamily.Unknown;
        private bool rendererWasAutoCreated;
        private Color lastAppliedColor = new(-1.0f, -1.0f, -1.0f, -1.0f);
        private bool lastActiveState;
        private float lastAppliedWidth = -1.0f;
        private bool lastVisibleState;

        public LineRenderer TargetRenderer => targetRenderer;

        public void ConfigureRenderer(LineRenderer renderer)
        {
            targetRenderer = renderer;
            rendererWasAutoCreated = false;
            InvalidateVisualCache();
            RefreshMaterialBinding(forceRefreshFamily: true);
        }

        public void SetMaterial(Material material)
        {
            EnsureTargetRenderer();
            if (targetRenderer == null)
                return;

            Material resolvedMaterial = material != null ? material : fallbackMaterial;
            if (resolvedMaterial == null)
                return;

            if (targetRenderer.sharedMaterial != resolvedMaterial)
                targetRenderer.sharedMaterial = resolvedMaterial;

            RefreshMaterialBinding(forceRefreshFamily: true);
        }

        public void SetVisible(bool visible)
        {
            EnsureTargetRenderer();
            if (targetRenderer == null)
                return;

            if (lastVisibleState == visible && targetRenderer.enabled == visible)
                return;

            targetRenderer.enabled = visible;
            if (!visible)
                targetRenderer.positionCount = 0;

            lastVisibleState = visible;
        }

        public void SetLineWidth(float lineWidth)
        {
            EnsureTargetRenderer();
            if (targetRenderer == null)
                return;

            float clampedWidth = Mathf.Max(0.0001f, lineWidth);
            if (Mathf.Approximately(lastAppliedWidth, clampedWidth))
                return;

            targetRenderer.startWidth = clampedWidth;
            targetRenderer.endWidth = clampedWidth;
            lastAppliedWidth = clampedWidth;
        }

        public void SetLinePoints(IReadOnlyList<Vector3> points)
        {
            EnsureTargetRenderer();
            if (targetRenderer == null)
                return;

            int pointCount = points != null ? points.Count : 0;
            targetRenderer.positionCount = pointCount;
            for (int i = 0; i < pointCount; i++)
                targetRenderer.SetPosition(i, points[i]);
        }

        public void SetEmissionSettings(
            bool enabled,
            Color nextEmissionColor,
            float nextActiveEmissionStrength,
            float nextInactiveEmissionStrength,
            bool nextSyncEnvironmentSimpleBoost,
            float nextActiveSimpleBoostIntensity,
            float nextInactiveSimpleBoostIntensity)
        {
            enableEmission = enabled;
            emissionColor = nextEmissionColor;
            activeEmissionStrength = Mathf.Max(0.0f, nextActiveEmissionStrength);
            inactiveEmissionStrength = Mathf.Max(0.0f, nextInactiveEmissionStrength);
            syncEnvironmentSimpleBoost = nextSyncEnvironmentSimpleBoost;
            activeSimpleBoostIntensity = Mathf.Max(0.0f, nextActiveSimpleBoostIntensity);
            inactiveSimpleBoostIntensity = Mathf.Max(0.0f, nextInactiveSimpleBoostIntensity);
        }

        public void SetInactiveStyle(bool nextDimInactiveLine, float nextInactiveAlphaMultiplier)
        {
            dimInactiveLine = nextDimInactiveLine;
            inactiveAlphaMultiplier = Mathf.Clamp01(nextInactiveAlphaMultiplier);
        }

        public void ApplyVisual(Color layerColor, bool isActive)
        {
            EnsureTargetRenderer();
            if (targetRenderer == null)
                return;

            RefreshMaterialBinding(forceRefreshFamily: false);

            Color appliedColor = layerColor;
            if (!isActive && dimInactiveLine)
                appliedColor.a *= inactiveAlphaMultiplier;

            appliedColor.a = Mathf.Clamp01(appliedColor.a);

            if (appliedColor != lastAppliedColor || isActive != lastActiveState)
            {
                targetRenderer.startColor = appliedColor;
                targetRenderer.endColor = appliedColor;
                lastAppliedColor = appliedColor;
                lastActiveState = isActive;
            }

            ApplyMaterialProperties(appliedColor, isActive);
        }

        public void DisposeOwnedResources()
        {
            if (rendererWasAutoCreated && targetRenderer != null)
            {
                GameObject targetObject = targetRenderer.gameObject;
                if (Application.isPlaying)
                    Destroy(targetObject);
                else
                    DestroyImmediate(targetObject);
            }

            targetRenderer = null;
            runtimeMaterial = null;
            if (ownedFallbackMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(ownedFallbackMaterial);
                else
                    DestroyImmediate(ownedFallbackMaterial);

                ownedFallbackMaterial = null;
            }

            rendererWasAutoCreated = false;
            InvalidateVisualCache();
        }

        private void EnsureTargetRenderer()
        {
            if (targetRenderer != null)
                return;

            if (!autoCreateRenderer)
                return;

            GameObject rendererObject = new(string.IsNullOrWhiteSpace(autoCreatedRendererName)
                ? "Runtime Line Visual"
                : autoCreatedRendererName);
            rendererObject.transform.SetParent(transform, false);
            targetRenderer = rendererObject.AddComponent<LineRenderer>();
            targetRenderer.useWorldSpace = true;
            targetRenderer.loop = false;
            targetRenderer.numCapVertices = 4;
            targetRenderer.numCornerVertices = 4;
            rendererWasAutoCreated = true;

            EnsureMaterialAssigned();

            RefreshMaterialBinding(forceRefreshFamily: true);
        }

        private void RefreshMaterialBinding(bool forceRefreshFamily)
        {
            if (targetRenderer == null)
                return;

            EnsureMaterialAssigned();

            Material nextMaterial = targetRenderer.sharedMaterial;
            if (nextMaterial == null)
            {
                runtimeMaterial = null;
                shaderFamily = ShaderFamily.Unknown;
                return;
            }

            if (runtimeMaterial == nextMaterial && !forceRefreshFamily)
                return;

            runtimeMaterial = nextMaterial;
            shaderFamily = DetectShaderFamily(runtimeMaterial);
            InvalidateVisualCache();
        }

        private void EnsureMaterialAssigned()
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial != null)
                return;

            if (fallbackMaterial != null)
            {
                targetRenderer.sharedMaterial = fallbackMaterial;
                return;
            }

            if (ownedFallbackMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Unlit");

                if (shader != null)
                    ownedFallbackMaterial = new Material(shader);
            }

            if (ownedFallbackMaterial != null)
                targetRenderer.sharedMaterial = ownedFallbackMaterial;
        }

        private void ApplyMaterialProperties(Color baseColor, bool isActive)
        {
            if (targetRenderer == null)
                return;

            MaterialPropertyBlock resolvedPropertyBlock = EnsurePropertyBlock();
            targetRenderer.GetPropertyBlock(resolvedPropertyBlock);

            resolvedPropertyBlock.SetColor(BaseColorId, baseColor);
            resolvedPropertyBlock.SetColor(ColorId, baseColor);

            float emissionStrength = enableEmission
                ? (isActive ? activeEmissionStrength : inactiveEmissionStrength)
                : 0.0f;

            if (runtimeMaterial != null)
                emissionStrength = ClampEmissionStrength(shaderFamily, emissionStrength);

            resolvedPropertyBlock.SetColor(EmissionColorId, emissionColor);
            resolvedPropertyBlock.SetFloat(EmissionStrengthId, emissionStrength);

            if (shaderFamily == ShaderFamily.EnvironmentStylizedLit && syncEnvironmentSimpleBoost)
            {
                float simpleBoostIntensity = enableEmission
                    ? (isActive ? activeSimpleBoostIntensity : inactiveSimpleBoostIntensity)
                    : 0.0f;

                simpleBoostIntensity = Mathf.Clamp(simpleBoostIntensity, 0.0f, SimpleBoostEmissionStrengthMax);
                resolvedPropertyBlock.SetFloat(SimpleBoostEnabledId, simpleBoostIntensity > 0.0001f ? 1.0f : 0.0f);
                resolvedPropertyBlock.SetColor(SimpleBoostColorId, emissionColor);
                resolvedPropertyBlock.SetFloat(SimpleBoostIntensityId, simpleBoostIntensity);
            }

            targetRenderer.SetPropertyBlock(resolvedPropertyBlock);
        }

        private MaterialPropertyBlock EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
            return propertyBlock;
        }

        private static float ClampEmissionStrength(ShaderFamily family, float value)
        {
            float clamped = Mathf.Max(0.0f, value);
            switch (family)
            {
                case ShaderFamily.EnvironmentStylizedLit:
                    return Mathf.Min(clamped, EnvironmentEmissionStrengthMax);
                case ShaderFamily.TrailUnlit:
                    return Mathf.Min(clamped, TrailEmissionStrengthMax);
                default:
                    return clamped;
            }
        }

        private static ShaderFamily DetectShaderFamily(Material material)
        {
            if (material == null || material.shader == null)
                return ShaderFamily.Unknown;

            string shaderName = material.shader.name;
            if (shaderName.Contains("TrailUnlit"))
                return ShaderFamily.TrailUnlit;

            if (shaderName.Contains("EnvironmentStylizedLit"))
                return ShaderFamily.EnvironmentStylizedLit;

            return ShaderFamily.Unknown;
        }

        private void InvalidateVisualCache()
        {
            lastAppliedColor = new Color(-1.0f, -1.0f, -1.0f, -1.0f);
            lastAppliedWidth = -1.0f;
            lastVisibleState = false;
        }
    }
}
