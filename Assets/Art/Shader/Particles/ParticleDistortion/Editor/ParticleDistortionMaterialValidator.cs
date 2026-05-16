using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal enum ParticleDistortionBlendMode
    {
        Alpha = 0,
        Additive = 1,
        Premultiply = 2
    }

    internal static class ParticleDistortionMaterialValidator
    {
        private const string ShaderName = "BC/Particles/ParticleDistortion";
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

        internal static bool TryGetOpaqueTextureDependencyWarning(Material material, out string warningMessage)
        {
            if (material == null || !IsParticleDistortionMaterial(material))
            {
                warningMessage = null;
                return false;
            }

            warningMessage = "ParticleDistortion depends on URP Opaque Texture. Keep it out of the standard WebGL path, and verify the active renderer actually captures the scene color before shipping.";
            return true;
        }

        internal static bool TryGetPerformanceWarning(Material material, out string warningMessage)
        {
            if (material == null || !IsParticleDistortionMaterial(material))
            {
                warningMessage = null;
                return false;
            }

            bool strongDistortion = material.HasProperty("_DistortionStrength") && material.GetFloat("_DistortionStrength") > 0.18f;
            bool strongNoise = material.HasProperty("_NoiseStrength") && material.GetFloat("_NoiseStrength") > 0.45f;
            bool strongAlpha = material.HasProperty("_Alpha") && material.GetFloat("_Alpha") > 0.65f;
            bool largeQueueOffset = material.HasProperty("_QueueOffset") && Mathf.Abs(material.GetFloat("_QueueOffset")) > 10f;
            if (!strongDistortion && !strongNoise && !strongAlpha && !largeQueueOffset)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = "This distortion material is heavy enough to amplify overdraw or opaque-texture cost. Avoid full-screen coverage, avoid mass spawning, and review it outside the standard WebGL path.";
            return true;
        }

        private static bool Normalize(Material material, bool applyChanges)
        {
            if (!IsParticleDistortionMaterial(material))
            {
                return false;
            }

            bool changed = false;
            changed |= ClampFloat(material, "_BlendMode", 0f, 2f, applyChanges, true);
            changed |= ClampFloat(material, "_Cull", 0f, 2f, applyChanges, true);
            changed |= ClampFloat(material, "_ZTest", 0f, 8f, applyChanges, true);
            changed |= ClampFloat(material, "_DistortionStrength", 0f, 2f, applyChanges);
            changed |= ClampFloat(material, "_DistortionScale", 0.01f, 8f, applyChanges);
            changed |= ClampFloat(material, "_Alpha", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_UseVertexColor", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_EdgeFadePower", 0.1f, 8f, applyChanges);
            changed |= ClampFloat(material, "_EdgeFadeStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_NoiseStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_NoiseScale", 0.01f, 8f, applyChanges);
            changed |= ClampFloat(material, "_QueueOffset", -50f, 50f, applyChanges, true);

            changed |= SetFloatIfNeeded(material, "_ZWrite", 0f, applyChanges);
            changed |= SetFloatIfNeeded(material, "_ColorMask", 15f, applyChanges);
            changed |= SyncBlendState(material, applyChanges);
            changed |= SyncRenderQueue(material, applyChanges);
            changed |= SyncRenderTypeTag(material, applyChanges);
            changed |= ClearShaderKeywords(material, applyChanges);
            return changed;
        }

        private static bool SyncBlendState(Material material, bool applyChanges)
        {
            ParticleDistortionBlendMode blendMode = GetBlendMode(material);
            BlendMode sourceBlend;
            BlendMode destinationBlend;

            switch (blendMode)
            {
                case ParticleDistortionBlendMode.Additive:
                    sourceBlend = BlendMode.SrcAlpha;
                    destinationBlend = BlendMode.One;
                    break;
                case ParticleDistortionBlendMode.Premultiply:
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

        private static ParticleDistortionBlendMode GetBlendMode(Material material)
        {
            if (material == null || !material.HasProperty("_BlendMode"))
            {
                return ParticleDistortionBlendMode.Alpha;
            }

            int blendMode = Mathf.RoundToInt(material.GetFloat("_BlendMode"));
            return (ParticleDistortionBlendMode)Mathf.Clamp(blendMode, 0, 2);
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
                material.shaderKeywords = System.Array.Empty<string>();
            }

            return true;
        }

        private static bool IsParticleDistortionMaterial(Material material)
        {
            return material != null && material.shader != null && material.shader.name == ShaderName;
        }
    }
}