using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal static class ParticleDistortionPresetUtility
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
                "HeatHaze",
                "Low-amplitude refractive shimmer intended for floor heat or exhaust haze.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)ParticleDistortionBlendMode.Alpha);
                    SetFloat(material, "_DistortionStrength", 0.08f);
                    SetFloat(material, "_DistortionScale", 1.25f);
                    SetVector(material, "_DistortionScrollSpeed", new Vector4(0.06f, 0.015f, 0f, 0f));
                    SetFloat(material, "_Alpha", 0.34f);
                    SetFloat(material, "_EdgeFadePower", 2.8f);
                    SetFloat(material, "_EdgeFadeStrength", 1f);
                    SetFloat(material, "_NoiseStrength", 0.12f);
                    SetFloat(material, "_NoiseScale", 1.5f);
                }),
            new PresetDefinition(
                "AirWarp",
                "Broader and softer background warp for lingering shock-air or pressure waves.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)ParticleDistortionBlendMode.Alpha);
                    SetFloat(material, "_DistortionStrength", 0.13f);
                    SetFloat(material, "_DistortionScale", 0.85f);
                    SetVector(material, "_DistortionScrollSpeed", new Vector4(0.03f, -0.02f, 0f, 0f));
                    SetFloat(material, "_Alpha", 0.48f);
                    SetFloat(material, "_EdgeFadePower", 2.1f);
                    SetFloat(material, "_EdgeFadeStrength", 0.92f);
                    SetFloat(material, "_NoiseStrength", 0.22f);
                    SetFloat(material, "_NoiseScale", 0.9f);
                }),
            new PresetDefinition(
                "MagicWarp",
                "Stronger, shorter-lived distortion for magical bursts and portal-like pulses.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)ParticleDistortionBlendMode.Premultiply);
                    SetFloat(material, "_DistortionStrength", 0.22f);
                    SetFloat(material, "_DistortionScale", 1.65f);
                    SetVector(material, "_DistortionScrollSpeed", new Vector4(0.14f, 0.09f, 0f, 0f));
                    SetFloat(material, "_Alpha", 0.58f);
                    SetFloat(material, "_EdgeFadePower", 1.6f);
                    SetFloat(material, "_EdgeFadeStrength", 0.78f);
                    SetFloat(material, "_NoiseStrength", 0.42f);
                    SetFloat(material, "_NoiseScale", 2.1f);
                })
        };

        internal static string[] GetPresetNames()
        {
            string[] names = new string[Presets.Length];
            for (int index = 0; index < Presets.Length; index++)
            {
                names[index] = Presets[index].Name;
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

        internal static void ApplyPreset(Material material, int presetIndex)
        {
            if (material == null || !IsValidPresetIndex(presetIndex))
            {
                return;
            }

            Presets[presetIndex].Apply(material);
            ParticleDistortionMaterialValidator.Normalize(material);
        }

        private static bool IsValidPresetIndex(int presetIndex)
        {
            return presetIndex >= 0 && presetIndex < Presets.Length;
        }

        private static void ApplyCommonBaseline(Material material)
        {
            SetFloat(material, "_BlendMode", (float)ParticleDistortionBlendMode.Alpha);
            SetFloat(material, "_Cull", 0f);
            SetFloat(material, "_ZTest", (float)CompareFunction.LessEqual);
            SetFloat(material, "_DistortionStrength", 0.12f);
            SetFloat(material, "_DistortionScale", 1f);
            SetVector(material, "_DistortionScrollSpeed", new Vector4(0.08f, 0.02f, 0f, 0f));
            SetFloat(material, "_Alpha", 0.45f);
            SetFloat(material, "_UseVertexColor", 1f);
            SetFloat(material, "_EdgeFadePower", 2.2f);
            SetFloat(material, "_EdgeFadeStrength", 0.85f);
            SetFloat(material, "_NoiseStrength", 0f);
            SetFloat(material, "_NoiseScale", 1f);
            SetFloat(material, "_QueueOffset", 0f);
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
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