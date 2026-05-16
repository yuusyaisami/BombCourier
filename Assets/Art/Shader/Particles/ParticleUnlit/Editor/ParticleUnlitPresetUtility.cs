using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal static class ParticleUnlitPresetUtility
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
                "Soft alpha particle for dust, smoke puffs, and low-energy ambient particles.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_QualityTier", (float)ParticleUnlitQualityTier.Low);
                    SetFloat(material, "_BlendMode", (float)ParticleUnlitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.78f, 0.70f, 0.58f, 0.58f));
                    SetFloat(material, "_Alpha", 0.62f);
                    SetFloat(material, "_Brightness", 0.95f);
                    SetFloat(material, "_EdgeFadePower", 1.35f);
                    SetFloat(material, "_EdgeFadeStrength", 0.9f);
                }),
            new PresetDefinition(
                "Glow",
                "Premultiplied soft light particle for readable glows without hard additive clipping.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_QualityTier", (float)ParticleUnlitQualityTier.Medium);
                    SetFloat(material, "_BlendMode", (float)ParticleUnlitBlendMode.Premultiply);
                    SetColor(material, "_BaseColor", new Color(0.72f, 0.90f, 1.0f, 0.72f));
                    SetFloat(material, "_Alpha", 0.72f);
                    SetFloat(material, "_Brightness", 1.3f);
                    SetColor(material, "_EmissionColor", new Color(0.42f, 0.76f, 1.0f, 1f));
                    SetFloat(material, "_EmissionStrength", 1.8f);
                    SetFloat(material, "_EmissionAlphaInfluence", 0.35f);
                    SetFloat(material, "_EdgeFadePower", 1.8f);
                    SetFloat(material, "_EdgeFadeStrength", 0.8f);
                }),
            new PresetDefinition(
                "Spark",
                "Additive high-energy particle for sparks, magic flecks, and bright impact accents.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_QualityTier", (float)ParticleUnlitQualityTier.Medium);
                    SetFloat(material, "_BlendMode", (float)ParticleUnlitBlendMode.Additive);
                    SetColor(material, "_BaseColor", new Color(1.0f, 0.86f, 0.54f, 0.66f));
                    SetFloat(material, "_Alpha", 0.68f);
                    SetFloat(material, "_Brightness", 1.55f);
                    SetColor(material, "_EmissionColor", new Color(1.0f, 0.62f, 0.18f, 1f));
                    SetFloat(material, "_EmissionStrength", 2.6f);
                    SetFloat(material, "_EmissionAlphaInfluence", 0.1f);
                    SetFloat(material, "_EdgeFadePower", 2.1f);
                    SetFloat(material, "_EdgeFadeStrength", 0.75f);
                }),
            new PresetDefinition(
                "Smoke",
                "Noise-broken alpha particle for lingering smoke and soft exhaust wisps.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_QualityTier", (float)ParticleUnlitQualityTier.Medium);
                    SetFloat(material, "_BlendMode", (float)ParticleUnlitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.50f, 0.52f, 0.50f, 0.48f));
                    SetFloat(material, "_Alpha", 0.5f);
                    SetFloat(material, "_Brightness", 0.78f);
                    SetFloat(material, "_MaskStrength", 0.7f);
                    SetFloat(material, "_NoiseStrength", 0.58f);
                    SetFloat(material, "_NoiseScale", 2.0f);
                    SetVector(material, "_NoiseScrollSpeed", new Vector4(0.02f, 0.05f, 0.0f, 0.0f));
                    SetFloat(material, "_DissolveAmount", 0.12f);
                    SetFloat(material, "_DissolveSoftness", 0.5f);
                    SetFloat(material, "_EdgeFadePower", 1.1f);
                    SetFloat(material, "_EdgeFadeStrength", 0.95f);
                }),
            new PresetDefinition(
                "Magic",
                "Additive particle with stronger noise and dissolve breakup for magic accents.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_QualityTier", (float)ParticleUnlitQualityTier.Medium);
                    SetFloat(material, "_BlendMode", (float)ParticleUnlitBlendMode.Additive);
                    SetColor(material, "_BaseColor", new Color(0.72f, 0.45f, 1.0f, 0.68f));
                    SetFloat(material, "_Alpha", 0.7f);
                    SetFloat(material, "_Brightness", 1.35f);
                    SetFloat(material, "_MaskStrength", 0.8f);
                    SetFloat(material, "_NoiseStrength", 0.65f);
                    SetFloat(material, "_NoiseScale", 3.2f);
                    SetVector(material, "_NoiseScrollSpeed", new Vector4(0.12f, 0.18f, 0.0f, 0.0f));
                    SetFloat(material, "_DissolveAmount", 0.24f);
                    SetFloat(material, "_DissolveSoftness", 0.2f);
                    SetColor(material, "_EmissionColor", new Color(0.55f, 0.30f, 1.0f, 1f));
                    SetFloat(material, "_EmissionStrength", 2.2f);
                    SetFloat(material, "_EmissionAlphaInfluence", 0.25f);
                    SetFloat(material, "_EdgeFadePower", 2.6f);
                    SetFloat(material, "_EdgeFadeStrength", 0.85f);
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
            ParticleUnlitMaterialValidator.Normalize(material);
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
            SetFloat(material, "_BlendMode", (float)ParticleUnlitBlendMode.Alpha);
            SetFloat(material, "_Cull", 0f);
            SetFloat(material, "_ZTest", (float)CompareFunction.LessEqual);
            SetColor(material, "_BaseColor", Color.white);
            SetFloat(material, "_Alpha", 1f);
            SetFloat(material, "_Brightness", 1f);
            SetFloat(material, "_UseVertexColor", 1f);
            SetFloat(material, "_MaskStrength", 0f);
            SetFloat(material, "_NoiseStrength", 0f);
            SetFloat(material, "_NoiseScale", 1f);
            SetVector(material, "_NoiseScrollSpeed", Vector4.zero);
            SetFloat(material, "_NoiseSpace", 0f);
            SetFloat(material, "_DissolveAmount", 0f);
            SetFloat(material, "_DissolveSoftness", 0.25f);
            SetColor(material, "_EmissionColor", Color.black);
            SetFloat(material, "_EmissionStrength", 0f);
            SetFloat(material, "_EmissionAlphaInfluence", 0.5f);
            SetFloat(material, "_DebugMode", 0f);
            SetFloat(material, "_UseSoftParticles", 0f);
            SetFloat(material, "_SoftParticleDistance", 0.75f);
            SetFloat(material, "_UseCameraFade", 0f);
            SetFloat(material, "_CameraFadeNear", 0.15f);
            SetFloat(material, "_CameraFadeFar", 0.75f);
            SetFloat(material, "_SoftCircleStrength", 1f);
            SetFloat(material, "_EdgeFadePower", 1.5f);
            SetFloat(material, "_EdgeFadeStrength", 1f);
            SetFloat(material, "_QueueOffset", 0f);
            SetFloat(material, "_QualityTier", (float)ParticleUnlitQualityTier.Medium);
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