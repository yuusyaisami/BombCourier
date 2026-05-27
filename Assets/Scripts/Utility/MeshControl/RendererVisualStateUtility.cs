using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Utility
{
    [Serializable]
    public struct RendererVisualState
    {
        [LabelText("Base Color")]
        [Tooltip("この状態で適用するベースカラーです。")]
        [SerializeField]
        private Color baseColor;

        [LabelText("Enable Emission")]
        [Tooltip("この状態で Emission を有効にします。")]
        [SerializeField]
        private bool emissionEnabled;

        [ShowIf(nameof(emissionEnabled))]
        [LabelText("Emission Color")]
        [Tooltip("Emission 有効時に使用する色です。")]
        [SerializeField]
        private Color emissionColor;

        [ShowIf(nameof(emissionEnabled))]
        [LabelText("Emission Strength")]
        [Tooltip("Emission 有効時に使用する発光強度です。")]
        [SerializeField, Min(0f)]
        private float emissionStrength;

        [ShowIf(nameof(emissionEnabled))]
        [LabelText("SimpleBoost Intensity")]
        [Tooltip("EnvironmentStylizedLit の SimpleBoost に加算する発光強度です。")]
        [SerializeField, Min(0f)]
        private float simpleBoostIntensity;

        public RendererVisualState(
            Color baseColor,
            bool emissionEnabled,
            Color emissionColor,
            float emissionStrength,
            float simpleBoostIntensity)
        {
            this.baseColor = baseColor;
            this.emissionEnabled = emissionEnabled;
            this.emissionColor = emissionColor;
            this.emissionStrength = Mathf.Max(0f, emissionStrength);
            this.simpleBoostIntensity = Mathf.Max(0f, simpleBoostIntensity);
        }

        public Color BaseColor => baseColor;
        public bool EmissionEnabled => emissionEnabled;
        public Color EmissionColor => emissionColor;
        public float EmissionStrength => Mathf.Max(0f, emissionStrength);
        public float SimpleBoostIntensity => Mathf.Max(0f, simpleBoostIntensity);

        public static RendererVisualState FromBaseColor(Color baseColor)
        {
            return new RendererVisualState(baseColor, false, baseColor, 0f, 0f);
        }

        public RendererVisualState WithAlpha(float alpha)
        {
            Color nextBaseColor = baseColor;
            nextBaseColor.a = Mathf.Clamp01(alpha);

            return new RendererVisualState(
                nextBaseColor,
                emissionEnabled,
                emissionColor,
                emissionStrength,
                simpleBoostIntensity);
        }
    }

    public static class RendererVisualStateUtility
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int SimpleBoostEnabledId = Shader.PropertyToID("_SimpleBoostEmissionEnabled");
        private static readonly int SimpleBoostColorId = Shader.PropertyToID("_SimpleBoostEmissionColor");
        private static readonly int SimpleBoostIntensityId = Shader.PropertyToID("_SimpleBoostEmissionIntensity");
        private static readonly int SurfaceModeId = Shader.PropertyToID("_SurfaceMode");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        private const float OpaqueSurfaceMode = 0f;
        private const float TransparentSurfaceMode = 1f;
        private const int OpaqueSrcBlend = 1;
        private const int OpaqueDstBlend = 0;
        private const int TransparentSrcBlend = 5;
        private const int TransparentDstBlend = 10;
        private const int OpaqueRenderQueue = 2000;
        private const int TransparentRenderQueue = 3000;

        public static void Apply(
            Renderer targetRenderer,
            in RendererVisualState visualState,
            bool syncEnvironmentSimpleBoost,
            MaterialPropertyBlock propertyBlock)
        {
            if (targetRenderer == null || propertyBlock == null)
                return;

            ApplyTransparencyState(targetRenderer, visualState.BaseColor.a < 0.999f);
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, visualState.BaseColor);
            propertyBlock.SetColor(ColorId, visualState.BaseColor);

            float resolvedEmissionStrength = visualState.EmissionEnabled
                ? visualState.EmissionStrength
                : 0f;

            propertyBlock.SetColor(EmissionColorId, visualState.EmissionColor);
            propertyBlock.SetFloat(EmissionStrengthId, resolvedEmissionStrength);

            float boost = syncEnvironmentSimpleBoost && visualState.EmissionEnabled
                ? visualState.SimpleBoostIntensity
                : 0f;

            propertyBlock.SetFloat(SimpleBoostEnabledId, boost > 0.0001f ? 1f : 0f);
            propertyBlock.SetColor(SimpleBoostColorId, visualState.EmissionColor);
            propertyBlock.SetFloat(SimpleBoostIntensityId, boost);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        public static Color ResolveBaseColor(Renderer targetRenderer)
        {
            if (targetRenderer == null)
                return Color.white;

            Material sharedMaterial = targetRenderer.sharedMaterial;
            if (sharedMaterial == null)
                return Color.white;

            if (sharedMaterial.HasProperty(BaseColorId))
                return sharedMaterial.GetColor(BaseColorId);

            if (sharedMaterial.HasProperty(ColorId))
                return sharedMaterial.GetColor(ColorId);

            return Color.white;
        }

        private static void ApplyTransparencyState(Renderer targetRenderer, bool transparent)
        {
            if (targetRenderer == null)
                return;

            // Edit-time validation can run on prefab assets/prefab stage objects where
            // Renderer.materials is forbidden and mutating shared materials would dirty assets.
            if (!Application.isPlaying)
                return;

            Material[] materials = targetRenderer.materials;
            if (materials == null || materials.Length == 0)
                return;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.HasProperty(SurfaceModeId))
                    continue;

                material.SetFloat(SurfaceModeId, transparent ? TransparentSurfaceMode : OpaqueSurfaceMode);
                material.SetFloat(SrcBlendId, transparent ? TransparentSrcBlend : OpaqueSrcBlend);
                material.SetFloat(DstBlendId, transparent ? TransparentDstBlend : OpaqueDstBlend);
                material.SetFloat(ZWriteId, transparent ? 0f : 1f);
                material.renderQueue = transparent ? TransparentRenderQueue : OpaqueRenderQueue;
                material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
                material.SetOverrideTag("Queue", transparent ? "Transparent" : "Geometry");
                material.SetShaderPassEnabled("ShadowCaster", !transparent);
                material.SetShaderPassEnabled("DepthOnly", !transparent);
                material.SetShaderPassEnabled("DepthNormalsOnly", !transparent);
            }
        }
    }
}