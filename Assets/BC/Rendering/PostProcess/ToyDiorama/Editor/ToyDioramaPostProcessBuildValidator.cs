using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering.Editor
{
    internal static class ToyDioramaPostProcessBuildValidator
    {
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
        private static readonly string[] VolumeSearchFolders = { "Assets/Settings" };

        internal static bool TryGetDebugViewAuthoringWarning(ToyDioramaPostProcessFeature feature, out string warningMessage)
        {
            if (feature == null ||
                feature.Settings == null ||
                feature.Settings.DebugView == ToyDioramaDebugView.Off)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = feature.Settings.Enabled
                ? $"Debug View is set to {feature.Settings.DebugView}. Non-development builds will fail until Debug View is reset to Off."
                : $"Debug View is set to {feature.Settings.DebugView}. Non-development builds will fail if this feature is enabled without resetting Debug View to Off.";
            return true;
        }

        internal static bool TryGetProjectPostProcessOverlapWarning(out string warningMessage)
        {
            List<string> conflicts = new List<string>();
            string[] profileGuids = AssetDatabase.FindAssets("t:VolumeProfile", VolumeSearchFolders);

            foreach (string profileGuid in profileGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(profileGuid);
                VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

                if (profile == null || !TryGetOverlappingComponents(profile, out string componentSummary))
                {
                    continue;
                }

                conflicts.Add($"{profile.name} ({componentSummary})");
            }

            if (conflicts.Count == 0)
            {
                warningMessage = null;
                return false;
            }

            warningMessage =
                "ToyDiorama owns Color Grade and Bloom. Disable URP Bloom / ColorAdjustments in these VolumeProfiles: " +
                string.Join(", ", conflicts);
            return true;
        }

        internal static bool TryGetRendererOwnershipWarning(ToyDioramaPostProcessFeature feature, out string warningMessage)
        {
            if (feature == null)
            {
                warningMessage = null;
                return false;
            }

            string rendererPath = AssetDatabase.GetAssetPath(feature);

            if (string.IsNullOrEmpty(rendererPath))
            {
                warningMessage = null;
                return false;
            }

            return TryGetRendererOwnershipWarning(feature, rendererPath, out warningMessage);
        }

        internal static bool TryGetRendererOwnershipWarning(
            ToyDioramaPostProcessFeature feature,
            string rendererPath,
            out string warningMessage)
        {
            if (feature == null)
            {
                warningMessage = null;
                return false;
            }

            List<string> issues = new List<string>();

            if (!feature.isActive)
            {
                issues.Add("ToyDiorama is registered on this renderer but the renderer feature itself is inactive.");
            }

            if (feature.Settings == null || !feature.Settings.Enabled)
            {
                issues.Add("ToyDiorama is registered on this renderer but settings.Enabled is off, so Game Camera will not receive the effect.");
            }

            if (HasActiveFullScreenPassRendererFeature(rendererPath))
            {
                issues.Add("An active FullScreenPassRendererFeature is still present on this renderer. Disable the legacy full-screen owner to keep ToyDiorama as the single final-look path.");
            }

            if (issues.Count == 0)
            {
                warningMessage = null;
                return false;
            }

            warningMessage = string.Join(" ", issues);
            return true;
        }

        internal static bool TryGetMobileQualityPolicyWarning(ToyDioramaPostProcessFeature feature, out string warningMessage)
        {
            if (feature == null)
            {
                warningMessage = null;
                return false;
            }

            string rendererPath = AssetDatabase.GetAssetPath(feature);

            if (string.IsNullOrEmpty(rendererPath))
            {
                warningMessage = null;
                return false;
            }

            return TryGetMobileQualityPolicyWarning(feature, rendererPath, out warningMessage);
        }

        internal static bool TryGetMobileAuthoredQualityTierWarning(ToyDioramaPostProcessFeature feature, out string warningMessage)
        {
            if (feature == null)
            {
                warningMessage = null;
                return false;
            }

            string rendererPath = AssetDatabase.GetAssetPath(feature);

            if (string.IsNullOrEmpty(rendererPath))
            {
                warningMessage = null;
                return false;
            }

            return TryGetMobileAuthoredQualityTierWarning(feature, rendererPath, out warningMessage);
        }

        internal static bool TryGetMobileQualityPolicyWarning(
            ToyDioramaPostProcessFeature feature,
            string rendererPath,
            out string warningMessage)
        {
            if (feature == null ||
                !string.Equals(rendererPath, MobileRendererPath, System.StringComparison.OrdinalIgnoreCase) ||
                !feature.isActive ||
                feature.Settings == null ||
                !feature.Settings.Enabled ||
                feature.ForceLowQualityTier)
            {
                warningMessage = null;
                return false;
            }

            warningMessage =
                "Mobile_Renderer supports ToyDiorama in Low tier only. Enable Force Low Quality Tier on this feature instance to keep the mobile path within the supported runtime budget.";
            return true;
        }

        internal static bool TryGetMobileAuthoredQualityTierWarning(
            ToyDioramaPostProcessFeature feature,
            string rendererPath,
            out string warningMessage)
        {
            if (feature == null ||
                !string.Equals(rendererPath, MobileRendererPath, System.StringComparison.OrdinalIgnoreCase) ||
                !feature.isActive ||
                feature.Settings == null ||
                !feature.Settings.Enabled ||
                !feature.ForceLowQualityTier ||
                feature.Settings.QualityTier == ToyDioramaQualityTier.Low)
            {
                warningMessage = null;
                return false;
            }

            warningMessage =
                $"Mobile_Renderer resolves runtime Quality Tier to Low, but the authored Quality Tier is {feature.Settings.QualityTier}. Keep the authored tier at Low on this renderer so the inspector summary matches the supported mobile runtime path.";
            return true;
        }

        internal static bool TryGetRendererPolicyBuildError(ToyDioramaPostProcessFeature feature, out string errorMessage)
        {
            if (feature == null)
            {
                errorMessage = null;
                return false;
            }

            string rendererPath = AssetDatabase.GetAssetPath(feature);

            if (string.IsNullOrEmpty(rendererPath))
            {
                errorMessage = null;
                return false;
            }

            return TryGetRendererPolicyBuildError(feature, rendererPath, out errorMessage);
        }

        internal static bool TryGetRendererPolicyBuildError(out string errorMessage)
        {
            List<string> invalidFeatures = new List<string>();
            string[] rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData");

            foreach (string rendererGuid in rendererGuids)
            {
                string rendererPath = AssetDatabase.GUIDToAssetPath(rendererGuid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rendererPath);

                foreach (Object asset in assets)
                {
                    if (asset is not ToyDioramaPostProcessFeature feature ||
                        !TryGetRendererPolicyBuildError(feature, rendererPath, out string featureError))
                    {
                        continue;
                    }

                    invalidFeatures.Add(featureError);
                }
            }

            if (invalidFeatures.Count == 0)
            {
                errorMessage = null;
                return false;
            }

            errorMessage =
                "ToyDiorama renderer policy must stay within the supported canonical paths before building: " +
                string.Join(" | ", invalidFeatures);
            return true;
        }

        internal static bool TryGetRendererPolicyBuildError(
            ToyDioramaPostProcessFeature feature,
            string rendererPath,
            out string errorMessage)
        {
            if (feature == null)
            {
                errorMessage = null;
                return false;
            }

            if (string.Equals(rendererPath, MobileRendererPath, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!feature.isActive)
                {
                    errorMessage = $"{rendererPath}: {feature.name} must stay active on Mobile_Renderer.asset.";
                    return true;
                }

                if (feature.Settings == null || !feature.Settings.Enabled)
                {
                    errorMessage = $"{rendererPath}: {feature.name} must keep settings.Enabled on for the supported mobile renderer path.";
                    return true;
                }

                if (!feature.ForceLowQualityTier)
                {
                    errorMessage = $"{rendererPath}: {feature.name} must enable Force Low Quality Tier for the supported mobile renderer path.";
                    return true;
                }
            }

            if (string.Equals(rendererPath, PcRendererPath, System.StringComparison.OrdinalIgnoreCase) &&
                feature.isActive &&
                feature.Settings != null &&
                feature.Settings.Enabled &&
                feature.ForceLowQualityTier)
            {
                errorMessage = $"{rendererPath}: {feature.name} must not enable Force Low Quality Tier on the canonical PC renderer.";
                return true;
            }

            errorMessage = null;
            return false;
        }

        internal static bool TryGetNonDevelopmentBuildError(out string errorMessage)
        {
            List<string> invalidFeatures = new List<string>();
            string[] rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData");

            foreach (string rendererGuid in rendererGuids)
            {
                string rendererPath = AssetDatabase.GUIDToAssetPath(rendererGuid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rendererPath);

                foreach (Object asset in assets)
                {
                    if (asset is not ToyDioramaPostProcessFeature feature ||
                        !TryGetNonDevelopmentBuildError(feature, rendererPath, out string featureError))
                    {
                        continue;
                    }

                    invalidFeatures.Add(featureError);
                }
            }

            if (invalidFeatures.Count == 0)
            {
                errorMessage = null;
                return false;
            }

            errorMessage =
                "ToyDiorama Debug View must be Off for non-development builds. Reset these features before building: " +
                string.Join(" | ", invalidFeatures);
            return true;
        }

        internal static bool TryGetNonDevelopmentBuildError(
            ToyDioramaPostProcessFeature feature,
            string rendererPath,
            out string errorMessage)
        {
            if (feature == null ||
                feature.Settings == null ||
                !feature.Settings.Enabled ||
                feature.Settings.DebugView == ToyDioramaDebugView.Off)
            {
                errorMessage = null;
                return false;
            }

            errorMessage = $"{rendererPath}: {feature.name} uses Debug View {feature.Settings.DebugView}";
            return true;
        }

        private static bool TryGetOverlappingComponents(VolumeProfile profile, out string componentSummary)
        {
            List<string> overlappingComponents = new List<string>();

            if (profile.TryGet(out Bloom bloom) && bloom.active && bloom.AnyPropertiesIsOverridden() && bloom.IsActive())
            {
                overlappingComponents.Add("Bloom");
            }

            if (profile.TryGet(out ColorAdjustments colorAdjustments) &&
                colorAdjustments.active &&
                colorAdjustments.AnyPropertiesIsOverridden() &&
                colorAdjustments.IsActive())
            {
                overlappingComponents.Add("ColorAdjustments");
            }

            componentSummary = string.Join(" + ", overlappingComponents);
            return overlappingComponents.Count > 0;
        }

        private static bool HasActiveFullScreenPassRendererFeature(string rendererPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rendererPath);

            foreach (Object asset in assets)
            {
                if (asset is FullScreenPassRendererFeature fullScreenPassFeature && fullScreenPassFeature.isActive)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class ToyDioramaPostProcessBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(out string rendererPolicyError))
            {
                throw new BuildFailedException(rendererPolicyError);
            }

            if ((report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            if (ToyDioramaPostProcessBuildValidator.TryGetNonDevelopmentBuildError(out string errorMessage))
            {
                throw new BuildFailedException(errorMessage);
            }
        }
    }
}