using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal enum TrailUnlitBlendMode
    {
        Alpha = 0,
        Additive = 1,
        Premultiply = 2,
        Multiply = 3
    }

    internal static class TrailUnlitMaterialValidator
    {
        private const string ShaderName = "BC/Particles/TrailUnlit";
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

        private static bool Normalize(Material material, bool applyChanges)
        {
            // 外部から呼ばれても、別ShaderのMaterial設定を巻き込んで変えない。
            if (!IsTrailUnlitMaterial(material))
            {
                return false;
            }

            bool changed = false;
            changed |= ClampFloat(material, "_BlendMode", 0f, 3f, applyChanges, true);
            changed |= ClampFloat(material, "_Cull", 0f, 2f, applyChanges, true);
            changed |= ClampFloat(material, "_ZTest", 0f, 8f, applyChanges, true);
            changed |= ClampFloat(material, "_Alpha", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_Brightness", 0f, 8f, applyChanges);
            changed |= ClampFloat(material, "_UseVertexColor", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_NoiseStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_NoiseScale", 0.01f, 16f, applyChanges);
            changed |= ClampFloat(material, "_DissolveAmount", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_DissolveSoftness", 0.001f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EdgeFadeAxis", 0f, 1f, applyChanges, true);
            changed |= ClampFloat(material, "_EdgeFadePower", 0.1f, 8f, applyChanges);
            changed |= ClampFloat(material, "_EdgeFadeStrength", 0f, 1f, applyChanges);
            changed |= ClampFloat(material, "_EmissionStrength", 0f, 16f, applyChanges);
            changed |= ClampFloat(material, "_QueueOffset", -50f, 50f, applyChanges, true);

            changed |= SetFloatIfNeeded(material, "_ZWrite", 0f, applyChanges);
            changed |= SetFloatIfNeeded(material, "_ColorMask", 15f, applyChanges);
            changed |= SyncBlendState(material, applyChanges);
            changed |= SyncRenderQueue(material, applyChanges);
            changed |= SyncRenderTypeTag(material, applyChanges);
            changed |= ClearShaderKeywords(material, applyChanges);

            return changed;
        }

        internal static bool TryGetPerformanceWarning(Material material, out string warningMessage)
        {
            if (material == null)
            {
                warningMessage = null;
                return false;
            }

            bool heavyNoise = material.HasProperty("_NoiseStrength") && material.GetFloat("_NoiseStrength") > 0.75f;
            bool strongDissolve = material.HasProperty("_DissolveAmount") && material.GetFloat("_DissolveAmount") > 0.75f;
            bool brightEmission = material.HasProperty("_EmissionStrength") && material.GetFloat("_EmissionStrength") > 8f;

            if (!heavyNoise && !strongDissolve && !brightEmission)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = "This trail material uses strong noise, dissolve, or emission. Review overdraw and WebGL cost with several particle systems visible.";
            return true;
        }

        private static bool SyncBlendState(Material material, bool applyChanges)
        {
            TrailUnlitBlendMode blendMode = GetBlendMode(material);

            BlendMode sourceBlend;
            BlendMode destinationBlend;

            // BlendMode はShader keywordを増やさず、MaterialのBlend stateだけで切り替える。
            switch (blendMode)
            {
                case TrailUnlitBlendMode.Additive:
                    sourceBlend = BlendMode.SrcAlpha;
                    destinationBlend = BlendMode.One;
                    break;
                case TrailUnlitBlendMode.Premultiply:
                    sourceBlend = BlendMode.One;
                    destinationBlend = BlendMode.OneMinusSrcAlpha;
                    break;
                case TrailUnlitBlendMode.Multiply:
                    sourceBlend = BlendMode.DstColor;
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

        private static TrailUnlitBlendMode GetBlendMode(Material material)
        {
            if (material == null || !material.HasProperty("_BlendMode"))
            {
                return TrailUnlitBlendMode.Alpha;
            }

            int blendMode = Mathf.RoundToInt(material.GetFloat("_BlendMode"));
            return (TrailUnlitBlendMode)Mathf.Clamp(blendMode, 0, 3);
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
            if (!IsTrailUnlitMaterial(material))
            {
                return false;
            }

            // TrailUnlitはvariantを増やさない方針なので、Inspector属性由来の不要keywordも残さない。
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

        private static bool IsTrailUnlitMaterial(Material material)
        {
            return material != null && material.shader != null && material.shader.name == ShaderName;
        }
    }
}
