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
            if (!highBrightness && !queueOffsetActive)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = "This particle material uses a strong brightness boost or queue offset. Review overdraw and sorting with several overlapping particles visible.";
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
            changed |= ClampFloat(material, "_SoftCircleStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EdgeFadePower", 0.1f, 8f, applyChanges);
            changed |= ClampFloat(material, "_EdgeFadeStrength", 0f, 1f, applyChanges);
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