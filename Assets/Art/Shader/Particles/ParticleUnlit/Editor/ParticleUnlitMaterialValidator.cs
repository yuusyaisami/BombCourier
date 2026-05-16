using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal enum ParticleUnlitBlendMode
    {
        Alpha = 0,
        Additive = 1,
        Premultiply = 2
    }

    internal static class ParticleUnlitMaterialValidator
    {
        private const string ShaderName = "BC/Particles/ParticleUnlit";
        private const int TransparentQueue = (int)RenderQueue.Transparent;
        private const string RenderTypeTag = "RenderType";
        private const string TransparentRenderType = "Transparent";

        internal static bool Normalize(Material material)
        {
            return Normalize(material, true);
        }

        internal static bool NeedsNormalization(Material material)
        {
            return Normalize(material, false);
        }

        internal static bool TryGetPerformanceWarning(Material material, out string warningMessage)
        {
            if (material == null || !IsParticleUnlitMaterial(material))
            {
                warningMessage = null;
                return false;
            }

            bool highBrightness = material.HasProperty("_Brightness") && material.GetFloat("_Brightness") > 2.5f;
            bool queueOffsetActive = material.HasProperty("_QueueOffset") && Mathf.Abs(material.GetFloat("_QueueOffset")) > 10f;
            bool heavyNoise = material.HasProperty("_NoiseStrength") && material.GetFloat("_NoiseStrength") > 0.75f;
            bool strongDissolve = material.HasProperty("_DissolveAmount") && material.GetFloat("_DissolveAmount") > 0.75f;
            bool strongEmission = material.HasProperty("_EmissionStrength") && material.GetFloat("_EmissionStrength") > 8f;
            if (!highBrightness && !queueOffsetActive && !heavyNoise && !strongDissolve && !strongEmission)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = "This particle material pushes Medium-tier brightness, queue offset, noise, dissolve, or emission. Review overdraw, sorting, readability, and the M7 WebGL load-test cases before treating it as WebGL-standard safe.";
            return true;
        }

        internal static bool TryGetTierSummary(Material material, out string summary)
        {
            if (material == null || !IsParticleUnlitMaterial(material))
            {
                summary = null;
                return false;
            }

            bool hasAuthoredTier = ParticleUnlitQualityTierUtility.TryGetAuthoredTier(material, out ParticleUnlitQualityTier authoredTier);
            bool hasInferredTier = ParticleUnlitQualityTierUtility.TryInferTier(material, out ParticleUnlitQualityTier inferredTier);
            if (!hasAuthoredTier && !hasInferredTier)
            {
                summary = null;
                return false;
            }

            string authoredLabel = hasAuthoredTier ? ParticleUnlitQualityTierUtility.GetTierName(authoredTier) : "Unset";
            string inferredLabel = hasInferredTier ? ParticleUnlitQualityTierUtility.GetTierName(inferredTier) : "Unclassified";
            summary = $"Authored Tier: {authoredLabel} / Inferred Tier: {inferredLabel}";
            return true;
        }

        internal static bool TryGetTierMismatchWarning(Material material, out string warningMessage)
        {
            if (material == null || !IsParticleUnlitMaterial(material))
            {
                warningMessage = null;
                return false;
            }

            if (!ParticleUnlitQualityTierUtility.TryGetAuthoredTier(material, out ParticleUnlitQualityTier authoredTier))
            {
                warningMessage = null;
                return false;
            }

            if (!ParticleUnlitQualityTierUtility.TryInferTier(material, out ParticleUnlitQualityTier inferredTier))
            {
                warningMessage = $"Authored Quality Tier is {ParticleUnlitQualityTierUtility.GetTierName(authoredTier)}, but the current material state does not match any canonical tier. Re-apply the intended tier or finish the missing feature set before shipping.";
                return true;
            }

            if (authoredTier == inferredTier)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = $"Authored Quality Tier is {ParticleUnlitQualityTierUtility.GetTierName(authoredTier)}, but the current material state infers {ParticleUnlitQualityTierUtility.GetTierName(inferredTier)}. Re-apply the intended tier or adjust the feature mix so authoring and runtime policy stay aligned.";
            return true;
        }

        internal static bool TryGetWebGlTierWarning(Material material, out string warningMessage)
        {
            if (material == null || !IsParticleUnlitMaterial(material))
            {
                warningMessage = null;
                return false;
            }

            if (!ParticleUnlitQualityTierUtility.RequiresHighTierPath(material))
            {
                warningMessage = null;
                return false;
            }

            warningMessage = "High-tier ParticleUnlit boundary features are enabled. Treat this material as opt-in for PC/high-quality validation and keep it out of the standard WebGL path unless the split/build policy is updated explicitly.";
            return true;
        }

        internal static bool TryGetDebugViewAuthoringWarning(Material material, out string warningMessage)
        {
            if (material == null || !IsParticleUnlitMaterial(material) || !material.HasProperty("_DebugMode"))
            {
                warningMessage = null;
                return false;
            }

            int debugMode = Mathf.RoundToInt(material.GetFloat("_DebugMode"));
            if (debugMode == 0)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = $"Debug Mode is set to {GetDebugModeName(debugMode)}. Reset Debug Mode to Final before regular authoring review.";
            return true;
        }

        internal static bool TryGetDepthInteractionWarning(Material material, out string warningMessage)
        {
            if (material == null || !IsParticleUnlitMaterial(material))
            {
                warningMessage = null;
                return false;
            }

            bool usesSoftParticles = material.HasProperty("_UseSoftParticles") && material.GetFloat("_UseSoftParticles") > 0.5f;
            bool usesCameraFade = material.HasProperty("_UseCameraFade") && material.GetFloat("_UseCameraFade") > 0.5f;
            if (!usesSoftParticles && !usesCameraFade)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = "Depth interaction is enabled. Verify URP depth texture availability, keep WebGL as optional/default-off, and confirm ON/OFF behavior in the ParticleMaterialTestScene before shipping.";
            return true;
        }

        private static bool Normalize(Material material, bool applyChanges)
        {
            // Editor utility から直接呼ばれても、対象外 shader の material を巻き込まない。
            if (!IsParticleUnlitMaterial(material))
            {
                return false;
            }

            bool changed = false;
            changed |= ClampFloat(material, "_BlendMode", 0f, 2f, applyChanges, true);
            changed |= ClampFloat(material, "_Cull", 0f, 2f, applyChanges, true);
            changed |= ClampFloat(material, "_ZTest", 0f, 8f, applyChanges, true);
            changed |= ClampFloat(material, "_Alpha", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_Brightness", 0f, 8f, applyChanges);
            changed |= ClampFloat(material, "_UseVertexColor", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_MaskStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_NoiseStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_NoiseScale", 0.01f, 16f, applyChanges);
            changed |= ClampFloat(material, "_NoiseSpace", 0f, 0f, applyChanges, true);
            changed |= ClampFloat(material, "_DissolveAmount", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_DissolveSoftness", 0.001f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EmissionStrength", 0f, 16f, applyChanges);
            changed |= ClampFloat(material, "_EmissionAlphaInfluence", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_DebugMode", 0f, 15f, applyChanges, true);
            changed |= ClampFloat(material, "_UseSoftParticles", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_SoftParticleDistance", 0.001f, 4f, applyChanges);
            changed |= ClampFloat(material, "_UseCameraFade", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_CameraFadeNear", 0f, 5f, applyChanges);
            changed |= ClampFloat(material, "_CameraFadeFar", 0.001f, 8f, applyChanges);
            changed |= ClampFloat(material, "_SoftCircleStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EdgeFadePower", 0.1f, 8f, applyChanges);
            changed |= ClampFloat(material, "_EdgeFadeStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_QueueOffset", -50f, 50f, applyChanges, true);
            changed |= ClampFloat(material, "_QualityTier", 0f, 2f, applyChanges, true);
            changed |= NormalizeCameraFadeRange(material, applyChanges);

            changed |= SetFloatIfNeeded(material, "_ZWrite", 0f, applyChanges);
            changed |= SetFloatIfNeeded(material, "_ColorMask", 15f, applyChanges);
            changed |= SyncBlendState(material, applyChanges);
            changed |= SyncRenderQueue(material, applyChanges);
            changed |= SyncRenderTypeTag(material, applyChanges);
            changed |= ClearShaderKeywords(material, applyChanges);
            return changed;
        }

        private static bool NormalizeCameraFadeRange(Material material, bool applyChanges)
        {
            if (!material.HasProperty("_CameraFadeNear") || !material.HasProperty("_CameraFadeFar"))
            {
                return false;
            }

            float currentNear = material.GetFloat("_CameraFadeNear");
            float currentFar = material.GetFloat("_CameraFadeFar");
            float normalizedNear = Mathf.Max(currentNear, 0f);
            float normalizedFar = Mathf.Max(currentFar, normalizedNear + 0.001f);
            if (Mathf.Approximately(currentNear, normalizedNear) && Mathf.Approximately(currentFar, normalizedFar))
            {
                return false;
            }

            if (applyChanges)
            {
                material.SetFloat("_CameraFadeNear", normalizedNear);
                material.SetFloat("_CameraFadeFar", normalizedFar);
            }

            return true;
        }

        private static bool SyncBlendState(Material material, bool applyChanges)
        {
            ParticleUnlitBlendMode blendMode = GetBlendMode(material);
            BlendMode sourceBlend;
            BlendMode destinationBlend;

            switch (blendMode)
            {
                case ParticleUnlitBlendMode.Additive:
                    sourceBlend = BlendMode.SrcAlpha;
                    destinationBlend = BlendMode.One;
                    break;
                case ParticleUnlitBlendMode.Premultiply:
                    sourceBlend = BlendMode.One;
                    destinationBlend = BlendMode.OneMinusSrcAlpha;
                    break;
                default:
                    sourceBlend = BlendMode.SrcAlpha;
                    destinationBlend = BlendMode.OneMinusSrcAlpha;
                    break;
            }

            bool changed = false;
            changed |= SetFloatIfNeeded(material, "_SrcBlend", (float)sourceBlend, applyChanges);
            changed |= SetFloatIfNeeded(material, "_DstBlend", (float)destinationBlend, applyChanges);
            return changed;
        }

        private static bool SyncRenderQueue(Material material, bool applyChanges)
        {
            int queueOffset = material.HasProperty("_QueueOffset") ? Mathf.RoundToInt(material.GetFloat("_QueueOffset")) : 0;
            int targetQueue = TransparentQueue + queueOffset;
            if (material.renderQueue == targetQueue)
            {
                return false;
            }

            if (applyChanges)
            {
                material.renderQueue = targetQueue;
            }

            return true;
        }

        private static bool SyncRenderTypeTag(Material material, bool applyChanges)
        {
            string currentTag = material.GetTag(RenderTypeTag, false, string.Empty);
            if (currentTag == TransparentRenderType)
            {
                return false;
            }

            if (applyChanges)
            {
                material.SetOverrideTag(RenderTypeTag, TransparentRenderType);
            }

            return true;
        }

        private static ParticleUnlitBlendMode GetBlendMode(Material material)
        {
            if (material == null || !material.HasProperty("_BlendMode"))
            {
                return ParticleUnlitBlendMode.Alpha;
            }

            int blendMode = Mathf.RoundToInt(material.GetFloat("_BlendMode"));
            return (ParticleUnlitBlendMode)Mathf.Clamp(blendMode, 0, 2);
        }

        private static string GetDebugModeName(int debugMode)
        {
            switch (Mathf.Clamp(debugMode, 0, 15))
            {
                case 1:
                    return "Base RGB";
                case 2:
                    return "Base Alpha";
                case 3:
                    return "Vertex Color";
                case 4:
                    return "Vertex Alpha";
                case 5:
                    return "MaskMap R / Dissolve";
                case 6:
                    return "MaskMap G / Emission";
                case 7:
                    return "MaskMap B / Variation";
                case 8:
                    return "MaskMap A / Shape";
                case 9:
                    return "Noise";
                case 10:
                    return "Dissolve Result";
                case 11:
                    return "Emission Result";
                case 12:
                    return "Soft Circle";
                case 13:
                    return "Custom1";
                case 14:
                    return "Custom2";
                case 15:
                    return "UV";
                default:
                    return "Final";
            }
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

        private static bool SetFloatIfNeeded(Material material, string propertyName, float targetValue, bool applyChanges)
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

        private static bool ClearShaderKeywords(Material material, bool applyChanges)
        {
            if (material.shaderKeywords == null || material.shaderKeywords.Length == 0)
            {
                return false;
            }

            if (applyChanges)
            {
                material.shaderKeywords = new string[0];
            }

            return true;
        }

        private static bool IsParticleUnlitMaterial(Material material)
        {
            return material != null && material.shader != null && material.shader.name == ShaderName;
        }
    }
}