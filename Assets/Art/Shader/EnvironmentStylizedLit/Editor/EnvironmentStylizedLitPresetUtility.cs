using System;
using UnityEngine;

namespace BC.Rendering
{
    internal static class EnvironmentStylizedLitPresetUtility
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
                "ClayDiorama",
                "Warm ambient clay look with strong shadow stylization and restrained sheen.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetColor(material, "_BaseColor", new Color(0.96f, 0.92f, 0.88f, 1f));
                    SetColor(material, "_ShadowColor", new Color(0.63f, 0.56f, 0.50f, 1f));
                    SetColor(material, "_LightColor", new Color(1.0f, 0.93f, 0.82f, 1f));
                    SetColor(material, "_AmbientTopColor", new Color(0.75f, 0.68f, 0.60f, 1f));
                    SetColor(material, "_AmbientSideColor", new Color(0.56f, 0.49f, 0.43f, 1f));
                    SetFloat(material, "_WrapLighting", 0.22f);
                    SetFloat(material, "_BandContrast", 1.2f);
                    SetFloat(material, "_Smoothness", 0.18f);
                    SetFloat(material, "_SpecularStrength", 0.08f);
                    SetFloat(material, "_EdgeSheenStrength", 0.04f);
                    SetFloat(material, "_AlbedoNoiseStrength", 0.24f);
                }),
            new PresetDefinition(
                "PaintedPlaster",
                "Muted plaster finish with soft bounced light and shallow band contrast.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetColor(material, "_BaseColor", new Color(0.94f, 0.95f, 0.93f, 1f));
                    SetColor(material, "_ShadowColor", new Color(0.67f, 0.72f, 0.78f, 1f));
                    SetColor(material, "_MidColor", new Color(0.90f, 0.92f, 0.93f, 1f));
                    SetColor(material, "_BounceColor", new Color(0.86f, 0.82f, 0.74f, 1f));
                    SetFloat(material, "_AmbientStrength", 0.42f);
                    SetFloat(material, "_BounceStrength", 0.28f);
                    SetFloat(material, "_BandContrast", 0.85f);
                    SetFloat(material, "_SpecularStrength", 0.05f);
                    SetFloat(material, "_Smoothness", 0.12f);
                    SetFloat(material, "_CavityStrength", 0.45f);
                }),
            new PresetDefinition(
                "MatteToyPlastic",
                "Stylized toy plastic with controlled ceramic-like highlights and cleaner noise.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetColor(material, "_BaseColor", new Color(0.95f, 0.94f, 0.98f, 1f));
                    SetColor(material, "_HighlightColor", new Color(1.0f, 0.99f, 0.95f, 1f));
                    SetFloat(material, "_SpecularMode", 4f);
                    SetFloat(material, "_SpecularStrength", 0.24f);
                    SetFloat(material, "_Smoothness", 0.46f);
                    SetFloat(material, "_EdgeSheenStrength", 0.16f);
                    SetFloat(material, "_EdgeSheenPower", 3.6f);
                    SetFloat(material, "_AlbedoNoiseStrength", 0.08f);
                }),
            new PresetDefinition(
                "CeramicToy",
                "Bright glazed ceramic response with tighter highlight stepping.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetColor(material, "_BaseColor", new Color(0.98f, 0.98f, 0.97f, 1f));
                    SetColor(material, "_SpecularColor", new Color(0.96f, 0.93f, 0.88f, 1f));
                    SetFloat(material, "_SpecularMode", 3f);
                    SetFloat(material, "_SpecularStrength", 0.32f);
                    SetFloat(material, "_Smoothness", 0.62f);
                    SetFloat(material, "_SpecularStepCount", 4f);
                    SetFloat(material, "_SpecularStepSmoothness", 0.06f);
                    SetFloat(material, "_EdgeSheenStrength", 0.10f);
                    SetFloat(material, "_CavityStrength", 0.22f);
                }),
            new PresetDefinition(
                "ChalkPastel",
                "Powdery pastel look with higher ambient fill, soft contrast, and visible world gradient support.",
                material =>
                {
                    ApplyCommonBaseline(material);
                    SetColor(material, "_BaseColor", new Color(0.97f, 0.95f, 0.93f, 1f));
                    SetColor(material, "_WorldYGradientTopColor", new Color(1.0f, 0.98f, 0.95f, 1f));
                    SetColor(material, "_WorldYGradientBottomColor", new Color(0.87f, 0.83f, 0.80f, 1f));
                    SetFloat(material, "_AmbientStrength", 0.48f);
                    SetFloat(material, "_IndirectStylizeStrength", 0.48f);
                    SetFloat(material, "_BandContrast", 0.72f);
                    SetFloat(material, "_SpecularStrength", 0.02f);
                    SetFloat(material, "_WorldYGradientEnabled", 1f);
                    SetFloat(material, "_WorldYGradientStrength", 0.35f);
                    SetFloat(material, "_AlbedoNoiseStrength", 0.16f);
                })
        };

        internal static int PresetCount => Presets.Length;

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

        internal static bool TryGetPresetIndex(string presetName, out int presetIndex)
        {
            for (int index = 0; index < Presets.Length; index++)
            {
                if (string.Equals(Presets[index].Name, presetName, StringComparison.Ordinal))
                {
                    presetIndex = index;
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
            EnvironmentStylizedLitMaterialValidator.Normalize(material);
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
            SetFloat(material, "_AlphaClip", 0f);
            SetFloat(material, "_Cutoff", 0.5f);
            SetColor(material, "_BaseColor", Color.white);

            SetColor(material, "_EmissionColor", Color.black);
            SetFloat(material, "_EmissionStrength", 0f);

            SetFloat(material, "_Metallic", 0f);
            SetFloat(material, "_Smoothness", 0.28f);
            SetColor(material, "_SpecularColor", new Color(0.2f, 0.2f, 0.2f, 1f));
            SetFloat(material, "_SpecularMode", 1f);
            SetFloat(material, "_SpecularStrength", 0.12f);
            SetFloat(material, "_SpecularStepCount", 3f);
            SetFloat(material, "_SpecularStepSmoothness", 0.08f);
            SetFloat(material, "_EdgeSheenStrength", 0.08f);
            SetFloat(material, "_EdgeSheenPower", 2.8f);
            SetColor(material, "_EdgeSheenColor", new Color(1.0f, 0.97f, 0.92f, 1f));

            SetFloat(material, "_LightStepCount", 3f);
            SetFloat(material, "_LightStepSmoothness", 0.08f);
            SetFloat(material, "_WrapLighting", 0.15f);
            SetFloat(material, "_BandContrast", 1f);
            SetFloat(material, "_BandOffset", 0f);
            SetColor(material, "_DeepShadowColor", new Color(0.34f, 0.40f, 0.56f, 1f));
            SetColor(material, "_ShadowColor", new Color(0.56f, 0.63f, 0.79f, 1f));
            SetColor(material, "_MidColor", new Color(0.84f, 0.88f, 0.93f, 1f));
            SetColor(material, "_LightColor", new Color(1.0f, 0.96f, 0.90f, 1f));
            SetColor(material, "_HighlightColor", new Color(1.0f, 0.98f, 0.94f, 1f));

            SetFloat(material, "_ShadowInfluence", 1f);
            SetFloat(material, "_ShadowSoftFill", 0.2f);
            SetFloat(material, "_ShadowColorBlend", 0.6f);

            SetColor(material, "_AmbientTopColor", new Color(0.50f, 0.57f, 0.70f, 1f));
            SetColor(material, "_AmbientSideColor", new Color(0.34f, 0.38f, 0.46f, 1f));
            SetColor(material, "_AmbientBottomColor", new Color(0.22f, 0.20f, 0.18f, 1f));
            SetFloat(material, "_AmbientStrength", 0.35f);
            SetColor(material, "_BounceColor", new Color(0.92f, 0.78f, 0.62f, 1f));
            SetFloat(material, "_BounceStrength", 0.2f);
            SetVector(material, "_BounceDirection", new Vector4(0f, 1f, 0f, 0f));
            SetFloat(material, "_BounceWrap", 0.35f);
            SetColor(material, "_IndirectShadowColor", new Color(0.72f, 0.78f, 0.88f, 1f));
            SetFloat(material, "_IndirectStrength", 1f);
            SetFloat(material, "_IndirectStylizeStrength", 0.35f);
            SetFloat(material, "_CavityStrength", 0.35f);
            SetColor(material, "_CavityColor", new Color(0.68f, 0.73f, 0.82f, 1f));

            SetFloat(material, "_AdditionalLightMode", 1f);
            SetFloat(material, "_AdditionalLightIntensity", 0.5f);
            SetFloat(material, "_AdditionalLightShadowInfluence", 0.65f);
            SetFloat(material, "_AdditionalLightColorInfluence", 0.75f);

            SetFloat(material, "_TriplanarBaseMapEnabled", 0f);
            SetFloat(material, "_TriplanarNormalMapEnabled", 0f);
            SetFloat(material, "_TriplanarNoiseEnabled", 0f);
            SetFloat(material, "_TriplanarScale", 1f);
            SetFloat(material, "_TriplanarBlendSharpness", 4f);

            SetFloat(material, "_VertexColorEnabled", 0f);
            SetFloat(material, "_VertexColorCavityStrength", 1f);
            SetFloat(material, "_VertexColorBandOffsetStrength", 1f);
            SetFloat(material, "_VertexColorColorVariationStrength", 1f);

            SetFloat(material, "_WorldYGradientEnabled", 0f);
            SetColor(material, "_WorldYGradientTopColor", Color.white);
            SetColor(material, "_WorldYGradientBottomColor", Color.white);
            SetFloat(material, "_WorldYGradientMin", 0f);
            SetFloat(material, "_WorldYGradientMax", 3f);
            SetFloat(material, "_WorldYGradientStrength", 0f);

            SetFloat(material, "_NoiseSpace", 0f);
            SetFloat(material, "_AlbedoNoiseStrength", 0.18f);
            SetFloat(material, "_WorldNoiseScale", 0.4f);
            SetFloat(material, "_WorldNoiseStrength", 0.35f);
            SetFloat(material, "_WorldNoiseContrast", 1.25f);
            SetFloat(material, "_LightBandNoiseStrength", 0.08f);
            SetFloat(material, "_LightBandNoiseScale", 0.75f);
            SetFloat(material, "_NoiseDistanceFadeStart", 12f);
            SetFloat(material, "_NoiseDistanceFadeEnd", 32f);

            SetFloat(material, "_DebugView", 0f);
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetColor(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetVector(Material material, string propertyName, Vector4 value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetVector(propertyName, value);
            }
        }
    }
}