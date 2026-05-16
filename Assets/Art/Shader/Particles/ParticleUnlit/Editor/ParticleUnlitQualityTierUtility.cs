using UnityEngine;

namespace BC.Rendering
{
    internal enum ParticleUnlitQualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    internal static class ParticleUnlitQualityTierUtility
    {
        private const string QualityTierPropertyName = "_QualityTier";

        private static readonly TierDefinition[] Definitions =
        {
            new TierDefinition(
                "Low",
                "BaseMap, Vertex Color, and Alpha only. Mask, noise, dissolve, emission, depth interaction, and authored high-tier paths stay off.",
                ApplyLowTier,
                MatchesLowTier),
            new TierDefinition(
                "Medium",
                "Canonical ParticleUnlit shipping path. Mask, noise, dissolve, and emission are available while depth interaction stays off.",
                ApplyMediumTier,
                MatchesMediumTier),
            new TierDefinition(
                "High",
                "Heavy ParticleUnlit path. Medium features stay on, and depth interaction is enabled as the representative material-side high-tier contract.",
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

        internal static string GetTierName(ParticleUnlitQualityTier tier)
        {
            int tierIndex = (int)tier;
            return IsValidTierIndex(tierIndex)
                ? Definitions[tierIndex].Name
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

            bool changed = Definitions[tierIndex].Apply(material);
            changed |= SetFloat(material, QualityTierPropertyName, tierIndex);
            return changed;
        }

        internal static bool ApplyTier(Material material, string tierName)
        {
            return TryGetTierIndex(tierName, out int tierIndex) && ApplyTier(material, tierIndex);
        }

        internal static bool TryInferTier(Material material, out ParticleUnlitQualityTier tier)
        {
            if (material != null)
            {
                for (int index = Definitions.Length - 1; index >= 0; index--)
                {
                    if (Definitions[index].Matches(material))
                    {
                        tier = (ParticleUnlitQualityTier)index;
                        return true;
                    }
                }
            }

            tier = default;
            return false;
        }

        internal static bool MatchesTier(Material material, ParticleUnlitQualityTier tier)
        {
            int tierIndex = (int)tier;
            return IsValidTierIndex(tierIndex) && Definitions[tierIndex].Matches(material);
        }

        internal static bool TryGetAuthoredTier(Material material, out ParticleUnlitQualityTier tier)
        {
            if (material != null && material.HasProperty(QualityTierPropertyName))
            {
                int tierIndex = Mathf.RoundToInt(material.GetFloat(QualityTierPropertyName));
                if (IsValidTierIndex(tierIndex))
                {
                    tier = (ParticleUnlitQualityTier)tierIndex;
                    return true;
                }
            }

            tier = default;
            return false;
        }

        internal static bool RequiresHighTierPath(Material material)
        {
            return material != null
                && (IsEnabled(material, "_UseSoftParticles") || IsEnabled(material, "_UseCameraFade"));
        }

        private static bool ApplyLowTier(Material material)
        {
            bool changed = false;
            changed |= SetFloat(material, "_MaskStrength", 0f);
            changed |= SetFloat(material, "_NoiseStrength", 0f);
            changed |= SetFloat(material, "_DissolveAmount", 0f);
            changed |= SetFloat(material, "_EmissionStrength", 0f);
            changed |= SetFloat(material, "_UseSoftParticles", 0f);
            changed |= SetFloat(material, "_UseCameraFade", 0f);
            changed |= SetFloat(material, "_DebugMode", 0f);
            return changed;
        }

        private static bool ApplyMediumTier(Material material)
        {
            bool changed = false;
            changed |= SetFloat(material, "_MaskStrength", 0.7f);
            changed |= SetFloat(material, "_NoiseStrength", 0.55f);
            changed |= SetFloat(material, "_NoiseScale", 2f);
            changed |= SetFloat(material, "_DissolveAmount", 0.15f);
            changed |= SetFloat(material, "_DissolveSoftness", 0.35f);
            changed |= EnsureEmissionColor(material);
            changed |= SetFloat(material, "_EmissionStrength", 1.6f);
            changed |= SetFloat(material, "_UseSoftParticles", 0f);
            changed |= SetFloat(material, "_UseCameraFade", 0f);
            changed |= SetFloat(material, "_DebugMode", 0f);
            return changed;
        }

        private static bool ApplyHighTier(Material material)
        {
            bool changed = false;
            changed |= ApplyMediumTier(material);
            changed |= SetFloat(material, "_UseSoftParticles", 1f);
            changed |= SetFloat(material, "_SoftParticleDistance", 0.75f);
            changed |= SetFloat(material, "_UseCameraFade", 1f);
            changed |= SetFloat(material, "_CameraFadeNear", 0.15f);
            changed |= SetFloat(material, "_CameraFadeFar", 0.75f);
            return changed;
        }

        private static bool MatchesLowTier(Material material)
        {
            return material != null
                && GetFloat(material, "_MaskStrength") <= 1e-4f
                && GetFloat(material, "_NoiseStrength") <= 1e-4f
                && GetFloat(material, "_DissolveAmount") <= 1e-4f
                && GetFloat(material, "_EmissionStrength") <= 1e-4f
                && !IsEnabled(material, "_UseSoftParticles")
                && !IsEnabled(material, "_UseCameraFade");
        }

        private static bool MatchesMediumTier(Material material)
        {
            return material != null
                && HasMediumTierFeature(material)
                && !IsEnabled(material, "_UseSoftParticles")
                && !IsEnabled(material, "_UseCameraFade");
        }

        private static bool MatchesHighTier(Material material)
        {
            return material != null
                && HasMediumTierFeature(material)
                && RequiresHighTierPath(material);
        }

        private static bool HasMediumTierFeature(Material material)
        {
            bool usesMaskAndNoise = GetFloat(material, "_MaskStrength") > 1e-4f
                && GetFloat(material, "_NoiseStrength") > 1e-4f;
            bool usesDissolve = GetFloat(material, "_DissolveAmount") > 1e-4f;
            bool usesEmission = GetFloat(material, "_EmissionStrength") > 1e-4f;
            return usesMaskAndNoise || usesDissolve || usesEmission;
        }

        private static bool EnsureEmissionColor(Material material)
        {
            if (material == null || !material.HasProperty("_EmissionColor"))
            {
                return false;
            }

            Color currentColor = material.GetColor("_EmissionColor");
            if (currentColor.maxColorComponent > 1e-4f)
            {
                return false;
            }

            material.SetColor("_EmissionColor", Color.white);
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

        private static bool IsValidTierIndex(int tierIndex)
        {
            return tierIndex >= 0 && tierIndex < Definitions.Length;
        }

        private readonly struct TierDefinition
        {
            internal TierDefinition(string name, string description, System.Func<Material, bool> apply, System.Func<Material, bool> matches)
            {
                Name = name;
                Description = description;
                Apply = apply;
                Matches = matches;
            }

            internal string Name { get; }

            internal string Description { get; }

            internal System.Func<Material, bool> Apply { get; }

            internal System.Func<Material, bool> Matches { get; }
        }
    }
}