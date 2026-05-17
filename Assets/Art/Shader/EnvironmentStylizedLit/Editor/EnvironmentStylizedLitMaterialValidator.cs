using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal static class EnvironmentStylizedLitMaterialValidator
    {
        private const string TriplanarBaseKeyword = "_ESL_TRIPLANAR_BASEMAP";
        private const string TriplanarNormalKeyword = "_ESL_TRIPLANAR_NORMALMAP";
        private const string TriplanarNoiseKeyword = "_ESL_TRIPLANAR_NOISE";

        internal static bool Normalize(Material material)
        {
            return Normalize(material, true);
        }

        internal static bool NeedsNormalization(Material material)
        {
            return Normalize(material, false);
        }

        private static bool Normalize(Material material, bool applyChanges)
        {
            if (material == null)
            {
                return false;
            }

            bool changed = false;
            changed |= ClampFloat(material, "_SurfaceMode", 0f, 2f, applyChanges, true);
            changed |= ClampFloat(material, "_Cull", 0f, 2f, applyChanges, true);
            changed |= ClampFloat(material, "_AlphaClip", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_Cutoff", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_FaceAlpha", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EdgeWidth", 0.25f, 8f, applyChanges);

            changed |= ClampFloat(material, "_NormalScale", 0f, 2f, applyChanges);
            changed |= ClampFloat(material, "_OcclusionStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EmissionStrength", 0f, 10f, applyChanges);

            changed |= ClampFloat(material, "_Metallic", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_Smoothness", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_SpecularMode", 0f, 4f, applyChanges, true);
            changed |= ClampFloat(material, "_SpecularStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_SpecularStepCount", 1f, 5f, applyChanges, true);
            changed |= ClampFloat(material, "_SpecularStepSmoothness", 0f, 0.5f, applyChanges);
            changed |= ClampFloat(material, "_EdgeSheenStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EdgeSheenPower", 0.5f, 8f, applyChanges);

            changed |= ClampFloat(material, "_LightStepCount", 1f, 5f, applyChanges, true);
            changed |= ClampFloat(material, "_LightStepSmoothness", 0f, 0.5f, applyChanges);
            changed |= ClampFloat(material, "_WrapLighting", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_BandContrast", 0.25f, 2f, applyChanges);
            changed |= ClampFloat(material, "_BandOffset", -1f, 1f, applyChanges);

            changed |= ClampFloat(material, "_ShadowInfluence", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_ShadowSoftFill", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_ShadowColorBlend", 0f, 1f, applyChanges);

            changed |= ClampFloat(material, "_AmbientStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_BounceStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_BounceWrap", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_IndirectStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_IndirectStylizeStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_CavityStrength", 0f, 1f, applyChanges);

            changed |= ClampFloat(material, "_AdditionalLightMode", 0f, 3f, applyChanges, true);
            changed |= ClampFloat(material, "_AdditionalLightIntensity", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_AdditionalLightShadowInfluence", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_AdditionalLightColorInfluence", 0f, 1f, applyChanges);

            changed |= ClampFloat(material, "_TriplanarBaseMapEnabled", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_TriplanarNormalMapEnabled", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_TriplanarNoiseEnabled", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_TriplanarScale", 0.01f, 8f, applyChanges);
            changed |= ClampFloat(material, "_TriplanarBlendSharpness", 1f, 8f, applyChanges);

            changed |= ClampFloat(material, "_VertexColorEnabled", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_VertexColorCavityStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_VertexColorBandOffsetStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_VertexColorColorVariationStrength", 0f, 1f, applyChanges);

            changed |= ClampFloat(material, "_WorldYGradientEnabled", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_WorldYGradientStrength", 0f, 1f, applyChanges);
            changed |= NormalizeMinMaxPair(material, "_WorldYGradientMin", "_WorldYGradientMax", applyChanges);

            changed |= ClampFloat(material, "_NoiseSpace", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_AlbedoNoiseStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_WorldNoiseScale", 0.01f, 8f, applyChanges);
            changed |= ClampFloat(material, "_WorldNoiseStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_WorldNoiseContrast", 0.25f, 4f, applyChanges);
            changed |= ClampFloat(material, "_LightBandNoiseStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_LightBandNoiseScale", 0.01f, 8f, applyChanges);
            changed |= NormalizeMinMaxPair(material, "_NoiseDistanceFadeStart", "_NoiseDistanceFadeEnd", applyChanges, 0f);

            changed |= ClampFloat(material, "_DebugView", 0f, 6f, applyChanges, true);

            changed |= SyncKeyword(material, "_TriplanarBaseMapEnabled", TriplanarBaseKeyword, applyChanges);
            changed |= SyncKeyword(material, "_TriplanarNormalMapEnabled", TriplanarNormalKeyword, applyChanges);
            changed |= SyncKeyword(material, "_TriplanarNoiseEnabled", TriplanarNoiseKeyword, applyChanges);
            changed |= SyncSurfaceState(material, applyChanges);

            return changed;
        }

        internal static bool TryGetDebugViewAuthoringWarning(Material material, out string warningMessage)
        {
            if (material == null || !material.HasProperty("_DebugView"))
            {
                warningMessage = null;
                return false;
            }

            int debugView = Mathf.RoundToInt(material.GetFloat("_DebugView"));
            if (debugView == 0)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = $"Debug View is set to {GetDebugViewName(debugView)}. Reset Debug View to Off before regular authoring review.";
            return true;
        }

        internal static bool TryGetTriplanarPerformanceWarning(Material material, out string warningMessage)
        {
            if (material == null)
            {
                warningMessage = null;
                return false;
            }

            List<string> enabledModes = new List<string>();

            if (IsEnabled(material, "_TriplanarBaseMapEnabled"))
            {
                enabledModes.Add("Base Map");
            }

            if (IsEnabled(material, "_TriplanarNormalMapEnabled"))
            {
                enabledModes.Add("Normal Map");
            }

            if (IsEnabled(material, "_TriplanarNoiseEnabled"))
            {
                enabledModes.Add("Noise");
            }

            if (enabledModes.Count == 0)
            {
                warningMessage = null;
                return false;
            }

            warningMessage =
                "Triplanar sampling increases texture and math cost. Enabled: " +
                string.Join(", ", enabledModes) +
                ". Keep it off on materials that already have stable UVs.";
            return true;
        }

        private static bool ClampFloat(Material material, string propertyName, float minValue, float maxValue, bool applyChanges, bool roundToInteger = false)
        {
            if (!material.HasProperty(propertyName))
            {
                return false;
            }

            float currentValue = material.GetFloat(propertyName);
            float clampedValue = Mathf.Clamp(currentValue, minValue, maxValue);

            if (roundToInteger)
            {
                clampedValue = Mathf.Round(clampedValue);
            }

            if (Mathf.Approximately(currentValue, clampedValue))
            {
                return false;
            }

            if (applyChanges)
            {
                material.SetFloat(propertyName, clampedValue);
            }

            return true;
        }

        private static bool NormalizeMinMaxPair(Material material, string minPropertyName, string maxPropertyName, bool applyChanges, float minimumBound = float.NegativeInfinity)
        {
            if (!material.HasProperty(minPropertyName) || !material.HasProperty(maxPropertyName))
            {
                return false;
            }

            float minValue = Mathf.Max(material.GetFloat(minPropertyName), minimumBound);
            float maxValue = Mathf.Max(material.GetFloat(maxPropertyName), minimumBound);

            if (minValue > maxValue)
            {
                (minValue, maxValue) = (maxValue, minValue);
            }

            bool changed = false;
            if (!Mathf.Approximately(material.GetFloat(minPropertyName), minValue))
            {
                if (applyChanges)
                {
                    material.SetFloat(minPropertyName, minValue);
                }

                changed = true;
            }

            if (!Mathf.Approximately(material.GetFloat(maxPropertyName), maxValue))
            {
                if (applyChanges)
                {
                    material.SetFloat(maxPropertyName, maxValue);
                }

                changed = true;
            }

            return changed;
        }

        private static bool SyncKeyword(Material material, string togglePropertyName, string keyword, bool applyChanges)
        {
            if (!material.HasProperty(togglePropertyName))
            {
                return false;
            }

            bool shouldEnable = IsEnabled(material, togglePropertyName);
            bool isEnabled = material.IsKeywordEnabled(keyword);

            if (shouldEnable == isEnabled)
            {
                return false;
            }

            if (!applyChanges)
            {
                return true;
            }

            if (shouldEnable)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }

            return true;
        }

        private static bool IsEnabled(Material material, string propertyName)
        {
            return material.HasProperty(propertyName) && material.GetFloat(propertyName) > 0.5f;
        }

        private static bool SyncSurfaceState(Material material, bool applyChanges)
        {
            if (material == null || !material.HasProperty("_SurfaceMode"))
            {
                return false;
            }

            int surfaceMode = Mathf.RoundToInt(material.GetFloat("_SurfaceMode"));
            bool isTransparent = surfaceMode == 1;
            bool isEdgeOnly = surfaceMode == 2;
            bool isBlended = isTransparent || isEdgeOnly;

            float srcBlend = isBlended ? (float)BlendMode.SrcAlpha : (float)BlendMode.One;
            float dstBlend = isBlended ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero;
            float zWrite = isBlended ? 0f : 1f;
            int renderQueue = isBlended ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;
            string renderType = isBlended ? "Transparent" : "Opaque";

            bool changed = false;
            changed |= SetFloat(material, "_SrcBlend", srcBlend, applyChanges);
            changed |= SetFloat(material, "_DstBlend", dstBlend, applyChanges);
            changed |= SetFloat(material, "_ZWrite", zWrite, applyChanges);
            changed |= SetRenderQueue(material, renderQueue, applyChanges);
            changed |= SetOverrideTag(material, "RenderType", renderType, applyChanges);
            changed |= SetOverrideTag(material, "Queue", isBlended ? "Transparent" : "Geometry", applyChanges);
            changed |= SetShaderPassEnabled(material, "ShadowCaster", !isBlended, applyChanges);
            changed |= SetShaderPassEnabled(material, "DepthOnly", !isBlended, applyChanges);
            changed |= SetShaderPassEnabled(material, "DepthNormalsOnly", !isBlended, applyChanges);
            return changed;
        }

        private static bool SetFloat(Material material, string propertyName, float targetValue, bool applyChanges)
        {
            if (!material.HasProperty(propertyName))
            {
                return false;
            }

            float currentValue = material.GetFloat(propertyName);
            if (Mathf.Approximately(currentValue, targetValue))
            {
                return false;
            }

            if (applyChanges)
            {
                material.SetFloat(propertyName, targetValue);
            }

            return true;
        }

        private static bool SetRenderQueue(Material material, int renderQueue, bool applyChanges)
        {
            if (material.renderQueue == renderQueue)
            {
                return false;
            }

            if (applyChanges)
            {
                material.renderQueue = renderQueue;
            }

            return true;
        }

        private static bool SetOverrideTag(Material material, string tagName, string tagValue, bool applyChanges)
        {
            string currentTagValue = material.GetTag(tagName, false, string.Empty);
            if (currentTagValue == tagValue)
            {
                return false;
            }

            if (applyChanges)
            {
                material.SetOverrideTag(tagName, tagValue);
            }

            return true;
        }

        private static bool SetShaderPassEnabled(Material material, string passName, bool enabled, bool applyChanges)
        {
            if (material.GetShaderPassEnabled(passName) == enabled)
            {
                return false;
            }

            if (applyChanges)
            {
                material.SetShaderPassEnabled(passName, enabled);
            }

            return true;
        }

        private static string GetDebugViewName(int debugView)
        {
            return debugView switch
            {
                1 => "NdotL",
                2 => "WrappedLight",
                3 => "SteppedLight",
                4 => "BandColor",
                5 => "WorldNoise",
                6 => "BandNoise",
                _ => "Off"
            };
        }
    }
}