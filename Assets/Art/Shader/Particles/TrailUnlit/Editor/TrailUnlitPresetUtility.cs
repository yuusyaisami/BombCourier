using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal static class TrailUnlitPresetUtility
    {
        private sealed class PresetDefinition
        {
            internal PresetDefinition(string name, string description, Action<Material> apply)
            {
                Name = name;
                Description = description;
                Apply = apply;
            }

            internal string Name { get; }

            internal string Description { get; }

            internal Action<Material> Apply { get; }
        }

        private static readonly PresetDefinition[] Presets =
        {
            new PresetDefinition(
                "Dust",
                "Soft alpha trail for dust, smoke puffs, and thrown-object ground accents.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)TrailUnlitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.72f, 0.66f, 0.56f, 0.55f));
                    SetFloat(material, "_Alpha", 0.55f);
                    SetFloat(material, "_Brightness", 0.9f);
                    SetVector(material, "_UVScrollSpeed", new Vector4(0.08f, 0.0f, 0.0f, 0.0f));
                    SetFloat(material, "_NoiseStrength", 0.35f);
                    SetFloat(material, "_NoiseScale", 1.8f);
                    SetVector(material, "_NoiseScrollSpeed", new Vector4(0.05f, 0.02f, 0.0f, 0.0f));
                    SetFloat(material, "_DissolveAmount", 0.08f);
                    SetFloat(material, "_DissolveSoftness", 0.45f);
                    SetFloat(material, "_EdgeFadeAxis", 1f);
                    SetFloat(material, "_EdgeFadePower", 1.4f);
                    SetFloat(material, "_EdgeFadeStrength", 0.8f);
                }),
            new PresetDefinition(
                "Wind",
                "Directional alpha-scroll streak for wind, air pressure, and speed-line motion.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)TrailUnlitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.74f, 0.90f, 1.0f, 0.45f));
                    SetFloat(material, "_Alpha", 0.62f);
                    SetFloat(material, "_Brightness", 1.15f);
                    SetVector(material, "_UVScrollSpeed", new Vector4(0.55f, 0.0f, 0.0f, 0.0f));
                    SetFloat(material, "_NoiseStrength", 0.5f);
                    SetFloat(material, "_NoiseScale", 2.6f);
                    SetVector(material, "_NoiseScrollSpeed", new Vector4(0.25f, 0.03f, 0.0f, 0.0f));
                    SetFloat(material, "_DissolveAmount", 0.18f);
                    SetFloat(material, "_DissolveSoftness", 0.25f);
                    SetFloat(material, "_EdgeFadeAxis", 1f);
                    SetFloat(material, "_EdgeFadePower", 2.2f);
                    SetFloat(material, "_EdgeFadeStrength", 0.9f);
                }),
            new PresetDefinition(
                "LightBeam",
                "Additive light ribbon for short beams, glints, and readable pickup trails.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)TrailUnlitBlendMode.Additive);
                    SetColor(material, "_BaseColor", new Color(0.92f, 0.96f, 1.0f, 0.72f));
                    SetFloat(material, "_Alpha", 0.72f);
                    SetFloat(material, "_Brightness", 1.25f);
                    SetColor(material, "_EmissionColor", new Color(0.45f, 0.72f, 1.0f, 1f));
                    SetFloat(material, "_EmissionStrength", 1.6f);
                    SetVector(material, "_UVScrollSpeed", new Vector4(0.18f, 0.0f, 0.0f, 0.0f));
                    SetFloat(material, "_NoiseStrength", 0.15f);
                    SetFloat(material, "_NoiseScale", 1.2f);
                    SetFloat(material, "_DissolveAmount", 0.03f);
                    SetFloat(material, "_DissolveSoftness", 0.55f);
                    SetFloat(material, "_EdgeFadePower", 1.8f);
                    SetFloat(material, "_EdgeFadeStrength", 0.7f);
                }),
            new PresetDefinition(
                "Magic",
                "High-energy additive trail with stronger dissolve breakup and colored emission.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)TrailUnlitBlendMode.Additive);
                    SetColor(material, "_BaseColor", new Color(0.70f, 0.45f, 1.0f, 0.68f));
                    SetFloat(material, "_Alpha", 0.7f);
                    SetFloat(material, "_Brightness", 1.35f);
                    SetColor(material, "_EmissionColor", new Color(0.55f, 0.30f, 1.0f, 1f));
                    SetFloat(material, "_EmissionStrength", 2.2f);
                    SetVector(material, "_UVScrollSpeed", new Vector4(0.28f, 0.12f, 0.0f, 0.0f));
                    SetFloat(material, "_NoiseStrength", 0.65f);
                    SetFloat(material, "_NoiseScale", 3.2f);
                    SetVector(material, "_NoiseScrollSpeed", new Vector4(0.12f, 0.18f, 0.0f, 0.0f));
                    SetFloat(material, "_DissolveAmount", 0.24f);
                    SetFloat(material, "_DissolveSoftness", 0.2f);
                    SetFloat(material, "_EdgeFadePower", 2.6f);
                    SetFloat(material, "_EdgeFadeStrength", 0.85f);
                }),
            new PresetDefinition(
                "Smoke",
                "Low-brightness alpha trail for lingering smoke and soft exhaust wisps.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)TrailUnlitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.48f, 0.49f, 0.48f, 0.5f));
                    SetFloat(material, "_Alpha", 0.48f);
                    SetFloat(material, "_Brightness", 0.72f);
                    SetFloat(material, "_NoiseStrength", 0.58f);
                    SetFloat(material, "_NoiseScale", 2.0f);
                    SetVector(material, "_NoiseScrollSpeed", new Vector4(0.02f, 0.05f, 0.0f, 0.0f));
                    SetFloat(material, "_DissolveAmount", 0.12f);
                    SetFloat(material, "_DissolveSoftness", 0.5f);
                    SetFloat(material, "_EdgeFadePower", 1.1f);
                    SetFloat(material, "_EdgeFadeStrength", 0.95f);
                }),
            new PresetDefinition(
                "SpeedLine",
                "Readable alpha streak for fast movement without additive bloom buildup.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)TrailUnlitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.95f, 0.98f, 1.0f, 0.55f));
                    SetFloat(material, "_Alpha", 0.58f);
                    SetFloat(material, "_Brightness", 1.35f);
                    SetVector(material, "_UVScrollSpeed", new Vector4(0.85f, 0.0f, 0.0f, 0.0f));
                    SetFloat(material, "_NoiseStrength", 0.2f);
                    SetFloat(material, "_NoiseScale", 4.0f);
                    SetFloat(material, "_DissolveAmount", 0.08f);
                    SetFloat(material, "_DissolveSoftness", 0.18f);
                    SetFloat(material, "_EdgeFadePower", 3.0f);
                    SetFloat(material, "_EdgeFadeStrength", 0.9f);
                })
        };

        internal static string[] GetPresetNames()
        {
            string[] names = new string[Presets.Length];
            for (int presetIndex = 0; presetIndex < Presets.Length; presetIndex++)
            {
                names[presetIndex] = Presets[presetIndex].Name;
            }

            return names;
        }

        internal static string GetPresetDescription(int presetIndex)
        {
            if (!IsValidPresetIndex(presetIndex))
            {
                return string.Empty;
            }

            return Presets[presetIndex].Description;
        }

        internal static bool TryGetPresetIndex(string presetName, out int presetIndex)
        {
            for (int currentIndex = 0; currentIndex < Presets.Length; currentIndex++)
            {
                if (string.Equals(Presets[currentIndex].Name, presetName, StringComparison.Ordinal))
                {
                    presetIndex = currentIndex;
                    return true;
                }
            }

            presetIndex = -1;
            return false;
        }

        internal static void ApplyPreset(Material material, int presetIndex)
        {
            if (material == null || !IsValidPresetIndex(presetIndex))
            {
                return;
            }

            Presets[presetIndex].Apply(material);
            TrailUnlitMaterialValidator.Normalize(material);
        }

        internal static void ApplyPreset(Material material, string presetName)
        {
            if (TryGetPresetIndex(presetName, out int presetIndex))
            {
                ApplyPreset(material, presetIndex);
            }
        }

        private static bool IsValidPresetIndex(int presetIndex)
        {
            return presetIndex >= 0 && presetIndex < Presets.Length;
        }

        private static void ApplyCommonBaseline(Material material)
        {
            SetFloat(material, "_BlendMode", (float)TrailUnlitBlendMode.Alpha);
            SetFloat(material, "_Cull", 0f);
            SetFloat(material, "_ZTest", (float)CompareFunction.LessEqual);
            SetColor(material, "_BaseColor", Color.white);
            SetFloat(material, "_Alpha", 1f);
            SetFloat(material, "_Brightness", 1f);
            SetFloat(material, "_UseVertexColor", 1f);
            SetVector(material, "_UVScrollSpeed", Vector4.zero);
            SetFloat(material, "_NoiseStrength", 0f);
            SetFloat(material, "_NoiseScale", 1f);
            SetVector(material, "_NoiseScrollSpeed", Vector4.zero);
            SetFloat(material, "_DissolveAmount", 0f);
            SetFloat(material, "_DissolveSoftness", 0.25f);
            SetFloat(material, "_EdgeFadeAxis", 1f);
            SetFloat(material, "_EdgeFadePower", 1f);
            SetFloat(material, "_EdgeFadeStrength", 0.6f);
            SetColor(material, "_EmissionColor", Color.black);
            SetFloat(material, "_EmissionStrength", 0f);
            SetFloat(material, "_QueueOffset", 0f);
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetColor(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetVector(Material material, string propertyName, Vector4 value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetVector(propertyName, value);
            }
        }
    }
}
