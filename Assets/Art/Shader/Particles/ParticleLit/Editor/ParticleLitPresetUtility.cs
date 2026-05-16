using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BC.Rendering
{
    internal static class ParticleLitPresetUtility
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
                "Raindrop",
                "Lean alpha-lit droplet with sharper specular and restrained emission.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)ParticleLitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.72f, 0.86f, 1.0f, 0.56f));
                    SetFloat(material, "_Alpha", 0.72f);
                    SetFloat(material, "_Smoothness", 0.82f);
                    SetFloat(material, "_Metallic", 0.0f);
                    SetFloat(material, "_LightInfluence", 1f);
                    SetColor(material, "_EmissionColor", new Color(0.12f, 0.26f, 0.42f, 1f));
                    SetFloat(material, "_EmissionStrength", 0.2f);
                }),
            new PresetDefinition(
                "Bubble",
                "Premultiplied lit bubble with softer highlights and slightly lifted emission.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)ParticleLitBlendMode.Premultiply);
                    SetColor(material, "_BaseColor", new Color(0.88f, 0.96f, 1.0f, 0.44f));
                    SetFloat(material, "_Alpha", 0.58f);
                    SetFloat(material, "_Smoothness", 0.92f);
                    SetFloat(material, "_Metallic", 0.0f);
                    SetFloat(material, "_LightInfluence", 0.9f);
                    SetColor(material, "_EmissionColor", new Color(0.24f, 0.48f, 0.74f, 1f));
                    SetFloat(material, "_EmissionStrength", 0.35f);
                }),
            new PresetDefinition(
                "Debris",
                "Opaque-lean lit debris shard for mesh particles with stronger metallic response.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetFloat(material, "_BlendMode", (float)ParticleLitBlendMode.Alpha);
                    SetColor(material, "_BaseColor", new Color(0.62f, 0.58f, 0.52f, 0.92f));
                    SetFloat(material, "_Alpha", 0.92f);
                    SetFloat(material, "_Smoothness", 0.36f);
                    SetFloat(material, "_Metallic", 0.3f);
                    SetFloat(material, "_LightInfluence", 1f);
                    SetColor(material, "_EmissionColor", Color.black);
                    SetFloat(material, "_EmissionStrength", 0f);
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
            ParticleLitMaterialValidator.Normalize(material);
        }

        private static bool IsValidPresetIndex(int presetIndex)
        {
            return presetIndex >= 0 && presetIndex < Presets.Length;
        }

        private static void ApplyCommonBaseline(Material material)
        {
            SetFloat(material, "_BlendMode", (float)ParticleLitBlendMode.Alpha);
            SetFloat(material, "_Cull", 0f);
            SetFloat(material, "_ZTest", (float)CompareFunction.LessEqual);
            SetColor(material, "_BaseColor", Color.white);
            SetFloat(material, "_Alpha", 1f);
            SetFloat(material, "_UseVertexColor", 1f);
            SetFloat(material, "_NormalScale", 1f);
            SetFloat(material, "_Smoothness", 0.35f);
            SetFloat(material, "_Metallic", 0f);
            SetFloat(material, "_LightInfluence", 1f);
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
    }
}