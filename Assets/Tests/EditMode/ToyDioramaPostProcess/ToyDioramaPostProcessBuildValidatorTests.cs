using BC.Rendering.Editor;
using NUnit.Framework;
using UnityEngine;

namespace BC.Rendering.Tests
{
    public sealed class ToyDioramaPostProcessBuildValidatorTests
    {
        [Test]
        public void DebugViewAuthoringWarningIsAbsentWhenDebugViewIsOff()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.Enabled = true;
                feature.Settings.DebugView = ToyDioramaDebugView.Off;

                Assert.IsFalse(ToyDioramaPostProcessBuildValidator.TryGetDebugViewAuthoringWarning(feature, out string warningMessage));
                Assert.IsNull(warningMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void DebugViewAuthoringWarningIsPresentWhenDebugViewIsNonOff()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.Enabled = false;
                feature.Settings.DebugView = ToyDioramaDebugView.BloomComposite;

                Assert.IsTrue(ToyDioramaPostProcessBuildValidator.TryGetDebugViewAuthoringWarning(feature, out string warningMessage));
                StringAssert.Contains("BloomComposite", warningMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void NonDevelopmentBuildErrorRequiresEnabledFeatureWithDebugView()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.Enabled = false;
                feature.Settings.DebugView = ToyDioramaDebugView.AfterBloom;

                Assert.IsFalse(ToyDioramaPostProcessBuildValidator.TryGetNonDevelopmentBuildError(feature, "Assets/Settings/Test_Renderer.asset", out string disabledMessage));
                Assert.IsNull(disabledMessage);

                feature.Settings.Enabled = true;

                Assert.IsTrue(ToyDioramaPostProcessBuildValidator.TryGetNonDevelopmentBuildError(feature, "Assets/Settings/Test_Renderer.asset", out string enabledMessage));
                StringAssert.Contains("Assets/Settings/Test_Renderer.asset", enabledMessage);
                StringAssert.Contains("AfterBloom", enabledMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void MobileQualityPolicyWarningRequiresForceLowQualityTierOnMobileRenderer()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.Enabled = true;
                feature.ForceLowQualityTier = false;

                Assert.IsTrue(
                    ToyDioramaPostProcessBuildValidator.TryGetMobileQualityPolicyWarning(feature, "Assets/Settings/Mobile_Renderer.asset", out string warningMessage),
                    "Mobile renderer should warn when Force Low Quality Tier is disabled.");
                StringAssert.Contains("Force Low Quality Tier", warningMessage);

                feature.ForceLowQualityTier = true;

                Assert.IsFalse(
                    ToyDioramaPostProcessBuildValidator.TryGetMobileQualityPolicyWarning(feature, "Assets/Settings/Mobile_Renderer.asset", out warningMessage),
                    warningMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void MobileRendererPolicyBuildErrorRequiresSupportedRuntimeConfiguration()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.Enabled = true;
                feature.ForceLowQualityTier = false;

                Assert.IsTrue(
                    ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(feature, "Assets/Settings/Mobile_Renderer.asset", out string errorMessage),
                    "Mobile renderer should block builds when Force Low Quality Tier is disabled.");
                StringAssert.Contains("Force Low Quality Tier", errorMessage);

                feature.ForceLowQualityTier = true;

                Assert.IsFalse(
                    ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(feature, "Assets/Settings/Mobile_Renderer.asset", out errorMessage),
                    errorMessage);

                feature.Settings.Enabled = false;

                Assert.IsTrue(
                    ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(feature, "Assets/Settings/Mobile_Renderer.asset", out errorMessage),
                    "Mobile renderer should block builds when the feature is effectively disabled.");
                StringAssert.Contains("settings.Enabled", errorMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void CanonicalPcRendererPolicyBuildErrorRejectsForceLowQualityTier()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.Enabled = true;
                feature.ForceLowQualityTier = true;

                Assert.IsTrue(
                    ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(feature, "Assets/Settings/PC_Renderer.asset", out string errorMessage),
                    "Canonical PC renderer should block Force Low Quality Tier regressions.");
                StringAssert.Contains("canonical PC renderer", errorMessage);

                feature.ForceLowQualityTier = false;

                Assert.IsFalse(
                    ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(feature, "Assets/Settings/PC_Renderer.asset", out errorMessage),
                    errorMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void MobileAuthoredQualityTierWarningHighlightsRuntimeAuthoringDrift()
        {
            ToyDioramaPostProcessFeature feature = ScriptableObject.CreateInstance<ToyDioramaPostProcessFeature>();

            try
            {
                feature.Settings.Enabled = true;
                feature.Settings.QualityTier = ToyDioramaQualityTier.High;
                feature.ForceLowQualityTier = true;

                Assert.IsTrue(
                    ToyDioramaPostProcessBuildValidator.TryGetMobileAuthoredQualityTierWarning(feature, "Assets/Settings/Mobile_Renderer.asset", out string warningMessage),
                    "Mobile renderer should warn when the authored tier no longer matches the forced-low runtime path.");
                StringAssert.Contains("authored Quality Tier is High", warningMessage);

                feature.Settings.QualityTier = ToyDioramaQualityTier.Low;

                Assert.IsFalse(
                    ToyDioramaPostProcessBuildValidator.TryGetMobileAuthoredQualityTierWarning(feature, "Assets/Settings/Mobile_Renderer.asset", out warningMessage),
                    warningMessage);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }
    }
}