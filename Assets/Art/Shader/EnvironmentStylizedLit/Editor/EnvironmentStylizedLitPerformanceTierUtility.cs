using UnityEngine;

namespace BC.Rendering
{
    internal enum EnvironmentStylizedLitPerformanceTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    internal static class EnvironmentStylizedLitPerformanceTierUtility
    {
        private const string TriplanarBaseKeyword = "_ESL_TRIPLANAR_BASEMAP";
        private const string TriplanarNormalKeyword = "_ESL_TRIPLANAR_NORMALMAP";
        private const string TriplanarNoiseKeyword = "_ESL_TRIPLANAR_NOISE";

        private static readonly TierDefinition[] Definitions =
        {
            new TierDefinition(
                "Low",
                "Main Light only. Triplanar, band noise, and additional lights stay off, while specular remains simple.",
                ApplyLowTier,
                MatchesLowTier),
            new TierDefinition(
                "Medium",
                "Adds FillOnly additional lights, world noise, soft specular, and AO while keeping triplanar and band noise disabled.",
                ApplyMediumTier,
                MatchesMediumTier),
            new TierDefinition(
                "High",
                "Enables triplanar, band noise, stylized specular, vertex color response, and strong AO for the heaviest authored path.",
                ApplyHighTier,
                MatchesHighTier),
        };

        internal static string[] GetTierNames()
        {
            string[] tierNames = new string[Definitions.Length];

            for (int index = 0; index < Definitions.Length; index++)
            {
                tierNames[index] = Definitions[index].Name;
            }

            return tierNames;
        }

        internal static string GetTierDescription(int tierIndex)
        {
            return IsValidTierIndex(tierIndex)
                ? Definitions[tierIndex].Description
                : null;
        }

        internal static bool TryGetTierIndex(string tierName, out int tierIndex)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                if (Definitions[index].Name == tierName)
                {
                    tierIndex = index;
                    return true;
                }
            }

            tierIndex = -1;
            return false;
        }

        internal static bool ApplyTier(Material material, int tierIndex)
        {
            if (material == null || !IsValidTierIndex(tierIndex))
            {
                return false;
            }

            return Definitions[tierIndex].Apply(material);
        }

        internal static bool ApplyTier(Material material, string tierName)
        {
            return TryGetTierIndex(tierName, out int tierIndex) && ApplyTier(material, tierIndex);
        }

        internal static bool TryInferTier(Material material, out EnvironmentStylizedLitPerformanceTier tier)
        {
            if (material != null)
            {
                for (int index = Definitions.Length - 1; index >= 0; index--)
                {
                    if (Definitions[index].Matches(material))
                    {
                        tier = (EnvironmentStylizedLitPerformanceTier)index;
                        return true;
                    }
                }
            }

            tier = default;
            return false;
        }

        internal static bool MatchesTier(Material material, EnvironmentStylizedLitPerformanceTier tier)
        {
            int tierIndex = (int)tier;
            return IsValidTierIndex(tierIndex) && Definitions[tierIndex].Matches(material);
        }

        private static bool ApplyLowTier(Material material)
        {
            bool changed = false;
            changed |= SetFloat(material, "_AdditionalLightMode", 0f);
            changed |= SetFloat(material, "_AdditionalLightIntensity", 0f);
            changed |= SetTriplanar(material, false, false, false);
            changed |= SetFloat(material, "_AlbedoNoiseStrength", 0f);
            changed |= SetFloat(material, "_WorldNoiseStrength", 0f);
            changed |= SetFloat(material, "_LightBandNoiseStrength", 0f);
            changed |= SetFloat(material, "_SpecularMode", 1f);
            changed |= SetFloat(material, "_SpecularStrength", 0.16f);
            changed |= SetFloat(material, "_OcclusionStrength", 0f);
            changed |= SetFloat(material, "_CavityStrength", 0f);
            changed |= SetFloat(material, "_VertexColorEnabled", 0f);
            changed |= SetFloat(material, "_DebugView", 0f);
            return changed;
        }

        private static bool ApplyMediumTier(Material material)
        {
            bool changed = false;
            changed |= SetFloat(material, "_AdditionalLightMode", 1f);
            changed |= SetFloat(material, "_AdditionalLightIntensity", 0.45f);
            changed |= SetFloat(material, "_AdditionalLightShadowInfluence", 0.65f);
            changed |= SetFloat(material, "_AdditionalLightColorInfluence", 0.7f);
            changed |= SetTriplanar(material, false, false, false);
            changed |= SetFloat(material, "_AlbedoNoiseStrength", 0.16f);
            changed |= SetFloat(material, "_WorldNoiseScale", 0.42f);
            changed |= SetFloat(material, "_WorldNoiseStrength", 0.26f);
            changed |= SetFloat(material, "_WorldNoiseContrast", 1.2f);
            changed |= SetFloat(material, "_LightBandNoiseStrength", 0f);
            changed |= SetFloat(material, "_SpecularMode", 1f);
            changed |= SetFloat(material, "_SpecularStrength", 0.2f);
            changed |= SetFloat(material, "_OcclusionStrength", 0.55f);
            changed |= SetFloat(material, "_CavityStrength", 0.35f);
            changed |= SetFloat(material, "_VertexColorEnabled", 0f);
            changed |= SetFloat(material, "_DebugView", 0f);
            return changed;
        }

        private static bool ApplyHighTier(Material material)
        {
            bool changed = false;
            changed |= SetFloat(material, "_AdditionalLightMode", 1f);
            changed |= SetFloat(material, "_AdditionalLightIntensity", 0.5f);
            changed |= SetFloat(material, "_AdditionalLightShadowInfluence", 0.65f);
            changed |= SetFloat(material, "_AdditionalLightColorInfluence", 0.75f);
            changed |= SetTriplanar(material, true, true, true);
            changed |= SetFloat(material, "_TriplanarScale", 1.35f);
            changed |= SetFloat(material, "_TriplanarBlendSharpness", 5f);
            changed |= SetFloat(material, "_AlbedoNoiseStrength", 0.18f);
            changed |= SetFloat(material, "_WorldNoiseScale", 0.42f);
            changed |= SetFloat(material, "_WorldNoiseStrength", 0.26f);
            changed |= SetFloat(material, "_WorldNoiseContrast", 1.2f);
            changed |= SetFloat(material, "_LightBandNoiseStrength", 0.1f);
            changed |= SetFloat(material, "_LightBandNoiseScale", 0.9f);
            changed |= SetFloat(material, "_SpecularMode", 3f);
            changed |= SetFloat(material, "_SpecularStrength", 0.32f);
            changed |= SetFloat(material, "_OcclusionStrength", 0.75f);
            changed |= SetFloat(material, "_CavityStrength", 0.55f);
            changed |= SetFloat(material, "_VertexColorEnabled", 1f);
            changed |= SetFloat(material, "_DebugView", 0f);
            return changed;
        }

        private static bool MatchesLowTier(Material material)
        {
            return material != null
                && IsApproximately(material, "_AdditionalLightMode", 0f)
                && !IsEnabled(material, "_TriplanarBaseMapEnabled")
                && !IsEnabled(material, "_TriplanarNormalMapEnabled")
                && !IsEnabled(material, "_TriplanarNoiseEnabled")
                && GetFloat(material, "_LightBandNoiseStrength") <= 1e-4f
                && GetFloat(material, "_WorldNoiseStrength") <= 1e-4f
                && GetFloat(material, "_OcclusionStrength") <= 1e-4f
                && GetFloat(material, "_CavityStrength") <= 1e-4f
                && !IsEnabled(material, "_VertexColorEnabled")
                && GetFloat(material, "_SpecularMode") <= 1.01f;
        }

        private static bool MatchesMediumTier(Material material)
        {
            return material != null
                && IsApproximately(material, "_AdditionalLightMode", 1f)
                && GetFloat(material, "_AdditionalLightIntensity") > 1e-4f
                && !IsEnabled(material, "_TriplanarBaseMapEnabled")
                && !IsEnabled(material, "_TriplanarNormalMapEnabled")
                && !IsEnabled(material, "_TriplanarNoiseEnabled")
                && GetFloat(material, "_WorldNoiseStrength") > 1e-4f
                && GetFloat(material, "_LightBandNoiseStrength") <= 1e-4f
                && GetFloat(material, "_OcclusionStrength") > 1e-4f
                && IsApproximately(material, "_SpecularMode", 1f)
                && !IsEnabled(material, "_VertexColorEnabled");
        }

        private static bool MatchesHighTier(Material material)
        {
            return material != null
                && IsApproximately(material, "_AdditionalLightMode", 1f)
                && GetFloat(material, "_AdditionalLightIntensity") > 1e-4f
                && IsEnabled(material, "_TriplanarBaseMapEnabled")
                && IsEnabled(material, "_TriplanarNormalMapEnabled")
                && IsEnabled(material, "_TriplanarNoiseEnabled")
                && GetFloat(material, "_LightBandNoiseStrength") > 1e-4f
                && GetFloat(material, "_WorldNoiseStrength") > 1e-4f
                && IsApproximately(material, "_SpecularMode", 3f)
                && IsEnabled(material, "_VertexColorEnabled")
                && GetFloat(material, "_OcclusionStrength") > 1e-4f;
        }

        private static bool SetTriplanar(Material material, bool baseEnabled, bool normalEnabled, bool noiseEnabled)
        {
            bool changed = false;
            changed |= SetKeywordToggle(material, "_TriplanarBaseMapEnabled", TriplanarBaseKeyword, baseEnabled);
            changed |= SetKeywordToggle(material, "_TriplanarNormalMapEnabled", TriplanarNormalKeyword, normalEnabled);
            changed |= SetKeywordToggle(material, "_TriplanarNoiseEnabled", TriplanarNoiseKeyword, noiseEnabled);
            return changed;
        }

        private static bool SetKeywordToggle(Material material, string propertyName, string keywordName, bool enabled)
        {
            bool changed = SetFloat(material, propertyName, enabled ? 1f : 0f);
            bool keywordEnabled = material.IsKeywordEnabled(keywordName);

            if (keywordEnabled == enabled)
            {
                return changed;
            }

            if (enabled)
            {
                material.EnableKeyword(keywordName);
            }
            else
            {
                material.DisableKeyword(keywordName);
            }

            return true;
        }

        private static bool SetFloat(Material material, string propertyName, float value)
        {
            if (material == null || !material.HasProperty(propertyName))
            {
                return false;
            }

            if (Mathf.Approximately(material.GetFloat(propertyName), value))
            {
                return false;
            }

            material.SetFloat(propertyName, value);
            return true;
        }

        private static float GetFloat(Material material, string propertyName)
        {
            return material != null && material.HasProperty(propertyName)
                ? material.GetFloat(propertyName)
                : 0f;
        }

        private static bool IsEnabled(Material material, string propertyName)
        {
            return GetFloat(material, propertyName) > 0.5f;
        }

        private static bool IsApproximately(Material material, string propertyName, float value)
        {
            return Mathf.Abs(GetFloat(material, propertyName) - value) <= 1e-4f;
        }

        private static bool IsValidTierIndex(int tierIndex)
        {
            return tierIndex >= 0 && tierIndex < Definitions.Length;
        }

        private readonly struct TierDefinition
        {
            internal TierDefinition(string name, string description, TierApply apply, TierMatch matches)
            {
                Name = name;
                Description = description;
                Apply = apply;
                Matches = matches;
            }

            internal string Name { get; }

            internal string Description { get; }

            internal TierApply Apply { get; }

            internal TierMatch Matches { get; }
        }

        private delegate bool TierApply(Material material);

        private delegate bool TierMatch(Material material);
    }
}