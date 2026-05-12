using BC.Rendering.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering.Tests
{
    public sealed class ToyDioramaPostProcessFeatureTests
    {
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";

        [Test]
        public void CameraPolicyDefaultsToGameCameraOnly()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.SceneViewEnabled = false;

                Assert.IsTrue(feature.ShouldApplyToCameraType(CameraType.Game));
                Assert.IsFalse(feature.ShouldApplyToCameraType(CameraType.SceneView));
                Assert.IsFalse(feature.ShouldApplyToCameraType(CameraType.Preview));
                Assert.IsFalse(feature.ShouldApplyToCameraType(CameraType.Reflection));
                StringAssert.Contains("Game Camera only", feature.GetCameraPolicySummary());
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void CameraPolicyCanOptIntoSceneView()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.SceneViewEnabled = true;

                Assert.IsTrue(feature.ShouldApplyToCameraType(CameraType.Game));
                Assert.IsTrue(feature.ShouldApplyToCameraType(CameraType.SceneView));
                Assert.IsFalse(feature.ShouldApplyToCameraType(CameraType.Preview));
                StringAssert.Contains("Scene View", feature.GetCameraPolicySummary());
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void ResolvedQualityTierDefaultsToAuthoredValue()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.QualityTier = ToyDioramaQualityTier.High;

                Assert.AreEqual(ToyDioramaQualityTier.High, feature.GetResolvedQualityTier());
                Assert.AreEqual(ToyDioramaQualityTier.High, feature.Settings.QualityTier);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void ForceLowQualityTierOverridesResolvedRuntimeTierWithoutMutatingAuthoringSettings()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.QualityTier = ToyDioramaQualityTier.Cinematic;
                feature.ForceLowQualityTier = true;

                Assert.AreEqual(ToyDioramaQualityTier.Low, feature.GetResolvedQualityTier());
                Assert.AreEqual(ToyDioramaQualityTier.Cinematic, feature.Settings.QualityTier);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void CreateResolvesRequiredRuntimeResourcesForConfiguredProjectAssets()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Create();

                Assert.IsFalse(
                    feature.TryGetRuntimeResourceError(out string errorMessage),
                    errorMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RuntimeResourceValidationSurfacesMissingInitializationState()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                Assert.IsTrue(
                    feature.TryGetRuntimeResourceError(out string errorMessage),
                    "A fresh feature instance without Create() should surface its missing runtime resources.");
                StringAssert.Contains("Missing composite shader", errorMessage);
                StringAssert.Contains("Missing bloom material", errorMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void CanonicalPcRendererUsesToyDioramaAsOnlyFullScreenOwner()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PcRendererPath);
            ToyDioramaPostProcessFeature toyDioramaFeature = null;
            int activeLegacyFullScreenPassCount = 0;

            foreach (Object asset in assets)
            {
                if (asset is ToyDioramaPostProcessFeature feature)
                {
                    toyDioramaFeature = feature;
                    continue;
                }

                if (asset is FullScreenPassRendererFeature fullScreenPassFeature && fullScreenPassFeature.isActive)
                {
                    activeLegacyFullScreenPassCount++;
                }
            }

            Assert.IsNotNull(toyDioramaFeature, "PC_Renderer.asset must register ToyDioramaPostProcessFeature.");
            Assert.IsTrue(toyDioramaFeature.isActive, "ToyDiorama renderer feature must stay active on PC_Renderer.asset.");
            Assert.IsTrue(toyDioramaFeature.Settings.Enabled, "ToyDiorama settings.Enabled must stay on for the canonical PC renderer.");
            Assert.IsFalse(toyDioramaFeature.ForceLowQualityTier, "PC_Renderer.asset must not force Low Quality Tier on the canonical desktop path.");
            Assert.AreEqual(toyDioramaFeature.Settings.QualityTier, toyDioramaFeature.GetResolvedQualityTier(), "PC_Renderer.asset must preserve the authored quality tier at runtime.");
            Assert.Zero(activeLegacyFullScreenPassCount, "PC_Renderer.asset must not keep an active FullScreenPassRendererFeature alongside ToyDiorama.");
            Assert.IsFalse(
                ToyDioramaPostProcessBuildValidator.TryGetRendererOwnershipWarning(toyDioramaFeature, PcRendererPath, out string warningMessage),
                warningMessage);
            Assert.IsFalse(
                ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(toyDioramaFeature, PcRendererPath, out string policyErrorMessage),
                policyErrorMessage);
        }

        [Test]
        public void SettingsProfilesDoNotTriggerToyDioramaOverlapWarning()
        {
            Assert.IsFalse(
                ToyDioramaPostProcessBuildValidator.TryGetProjectPostProcessOverlapWarning(out string warningMessage),
                warningMessage);
        }

        [Test]
        public void MobileRendererRegistersToyDioramaWithForcedLowQualityTier()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(MobileRendererPath);
            ToyDioramaPostProcessFeature toyDioramaFeature = null;

            foreach (Object asset in assets)
            {
                if (asset is ToyDioramaPostProcessFeature feature)
                {
                    toyDioramaFeature = feature;
                    break;
                }
            }

            Assert.IsNotNull(toyDioramaFeature, "Mobile_Renderer.asset must register ToyDioramaPostProcessFeature.");
            Assert.IsTrue(toyDioramaFeature.isActive, "ToyDiorama renderer feature must stay active on Mobile_Renderer.asset.");
            Assert.IsTrue(toyDioramaFeature.Settings.Enabled, "ToyDiorama settings.Enabled must stay on for the supported mobile renderer path.");
            Assert.IsTrue(toyDioramaFeature.ForceLowQualityTier, "Mobile_Renderer.asset must force Low Quality Tier at runtime.");
            Assert.AreEqual(ToyDioramaQualityTier.Low, toyDioramaFeature.GetResolvedQualityTier());
            Assert.IsNotNull(toyDioramaFeature.SelectedPreset, "Mobile_Renderer.asset should keep a mobile-specific preset selected for authoring guidance.");
            Assert.AreEqual("MobileOptimized", toyDioramaFeature.SelectedPreset.name);
            Assert.IsFalse(
                ToyDioramaPostProcessBuildValidator.TryGetMobileQualityPolicyWarning(toyDioramaFeature, MobileRendererPath, out string warningMessage),
                warningMessage);
        }
    }
}