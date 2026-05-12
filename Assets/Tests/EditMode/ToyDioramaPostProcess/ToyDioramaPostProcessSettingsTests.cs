using BC.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace BC.Rendering.Tests
{
    public sealed class ToyDioramaPostProcessSettingsTests
    {
        [Test]
        public void DefaultsProvideM9ColorGradeValues()
        {
            ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings();

            Assert.IsTrue(settings.Enabled);
            Assert.AreEqual(ToyDioramaQualityTier.Medium, settings.QualityTier);
            Assert.AreEqual(ToyDioramaDebugView.Off, settings.DebugView);
            Assert.AreEqual(0f, settings.Exposure);
            Assert.AreEqual(1f, settings.Contrast);
            Assert.AreEqual(1f, settings.Saturation);
            Assert.AreEqual(0.08f, settings.BlackLift);
            Assert.AreEqual(0.25f, settings.WhiteSoftClamp);
            Assert.AreEqual(0.30f, settings.PastelStrength);
            Assert.AreEqual(0.50f, settings.HighSaturationCompress);
            Assert.AreEqual(0.15f, settings.PastelLuminanceBias);
            Assert.AreEqual(0.35f, settings.ShadowTintStrength);
            Assert.AreEqual(0f, settings.MidTintStrength);
            Assert.AreEqual(0.20f, settings.HighlightTintStrength);
            Assert.AreEqual(0.25f, settings.CreamHighlightStrength);
            Assert.AreEqual(0.70f, settings.CreamHighlightThreshold);
            Assert.AreEqual(0.10f, settings.CreamHighlightSoftness);
            Assert.IsTrue(settings.EdgeToneEnabled);
            Assert.AreEqual(0.12f, settings.EdgeToneStrength);
            Assert.AreEqual(0.62f, settings.EdgeToneRadius);
            Assert.AreEqual(0.22f, settings.EdgeToneSoftness);
            Assert.AreEqual(0.18f, settings.EdgeSaturationFade);
            Assert.AreEqual(0f, settings.EdgeBrightnessOffset);
            Assert.IsTrue(settings.DepthHazeEnabled);
            Assert.AreEqual(0.10f, settings.DepthHazeStrength);
            Assert.AreEqual(0.45f, settings.DepthHazeStart);
            Assert.AreEqual(0.95f, settings.DepthHazeEnd);
            Assert.AreEqual(0.18f, settings.DepthHazeSaturationFade);
            Assert.AreEqual(0.04f, settings.DepthHazeBrightnessLift);
            Assert.IsTrue(settings.SoftBloomEnabled);
            Assert.AreEqual(0.82f, settings.SoftBloomThreshold);
            Assert.AreEqual(0.18f, settings.SoftBloomSoftKnee);
            Assert.AreEqual(0.14f, settings.SoftBloomIntensity);
            Assert.AreEqual(0.65f, settings.SoftBloomRadius);
            Assert.IsTrue(settings.HalationEnabled);
            Assert.AreEqual(0.04f, settings.HalationStrength);
            Assert.AreEqual(0.88f, settings.HalationThreshold);
            Assert.AreEqual(0.55f, settings.HalationRadius);
            Assert.IsTrue(settings.GrainEnabled);
            Assert.AreEqual(0.02f, settings.GrainStrength);
            Assert.AreEqual(1f, settings.GrainScale);
            Assert.AreEqual(0.60f, settings.GrainResponse);
            Assert.AreEqual(0.10f, settings.GrainTemporalStrength);
            AssertColorApproximately(new Color(1f, 0.98f, 0.93f, 1f), settings.CreamHighlightColor);
            AssertColorApproximately(new Color(1f, 0.98f, 0.95f, 1f), settings.EdgeToneColor);
            AssertColorApproximately(new Color(0.78f, 0.86f, 0.92f, 1f), settings.DepthHazeColor);
            AssertColorApproximately(new Color(1f, 0.96f, 0.92f, 1f), settings.SoftBloomTint);
            AssertColorApproximately(new Color(1f, 0.74f, 0.62f, 1f), settings.HalationColor);
        }

        [Test]
        public void DebugViewValuesRemainStable()
        {
            Assert.AreEqual(0, (int)ToyDioramaDebugView.Off);
            Assert.AreEqual(1, (int)ToyDioramaDebugView.SourceColor);
            Assert.AreEqual(2, (int)ToyDioramaDebugView.Luminance);
            Assert.AreEqual(3, (int)ToyDioramaDebugView.UV);
            Assert.AreEqual(4, (int)ToyDioramaDebugView.ShadowMask);
            Assert.AreEqual(5, (int)ToyDioramaDebugView.MidMask);
            Assert.AreEqual(6, (int)ToyDioramaDebugView.HighlightMask);
            Assert.AreEqual(7, (int)ToyDioramaDebugView.BeforeColorGrade);
            Assert.AreEqual(8, (int)ToyDioramaDebugView.AfterColorGrade);
            Assert.AreEqual(9, (int)ToyDioramaDebugView.PastelMask);
            Assert.AreEqual(10, (int)ToyDioramaDebugView.HighSaturationMask);
            Assert.AreEqual(11, (int)ToyDioramaDebugView.CreamHighlightMask);
            Assert.AreEqual(12, (int)ToyDioramaDebugView.BeforePastel);
            Assert.AreEqual(13, (int)ToyDioramaDebugView.AfterPastel);
            Assert.AreEqual(14, (int)ToyDioramaDebugView.EdgeMask);
            Assert.AreEqual(15, (int)ToyDioramaDebugView.BeforeEdgeTone);
            Assert.AreEqual(16, (int)ToyDioramaDebugView.AfterEdgeTone);
            Assert.AreEqual(17, (int)ToyDioramaDebugView.RawDepth);
            Assert.AreEqual(18, (int)ToyDioramaDebugView.LinearDepth);
            Assert.AreEqual(19, (int)ToyDioramaDebugView.DepthHazeMask);
            Assert.AreEqual(20, (int)ToyDioramaDebugView.BeforeDepthHaze);
            Assert.AreEqual(21, (int)ToyDioramaDebugView.AfterDepthHaze);
            Assert.AreEqual(22, (int)ToyDioramaDebugView.Grain);
            Assert.AreEqual(23, (int)ToyDioramaDebugView.BeforeGrain);
            Assert.AreEqual(24, (int)ToyDioramaDebugView.AfterGrain);
            Assert.AreEqual(25, (int)ToyDioramaDebugView.BloomPrefilter);
            Assert.AreEqual(26, (int)ToyDioramaDebugView.BloomBlur);
            Assert.AreEqual(27, (int)ToyDioramaDebugView.BloomComposite);
            Assert.AreEqual(28, (int)ToyDioramaDebugView.HalationMask);
            Assert.AreEqual(29, (int)ToyDioramaDebugView.BeforeBloom);
            Assert.AreEqual(30, (int)ToyDioramaDebugView.AfterBloom);
        }

        [Test]
        public void QualityTierValuesRemainStable()
        {
            Assert.AreEqual(0, (int)ToyDioramaQualityTier.Low);
            Assert.AreEqual(1, (int)ToyDioramaQualityTier.Medium);
            Assert.AreEqual(2, (int)ToyDioramaQualityTier.High);
            Assert.AreEqual(3, (int)ToyDioramaQualityTier.Cinematic);
        }

        [Test]
        public void ApplyToMaterialBindsM8Properties()
        {
            Shader shader = Shader.Find("BC/PostProcess/ToyDioramaComposite");
            Assert.IsNotNull(shader);

            Texture2D blueNoiseTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Material material = new Material(shader);

            try
            {
                ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings
                {
                    Enabled = false,
                    QualityTier = ToyDioramaQualityTier.High,
                    DebugView = ToyDioramaDebugView.HighlightMask,
                    Exposure = 0.75f,
                    Contrast = 1.35f,
                    Saturation = 1.1f,
                    BlackLift = 0.22f,
                    WhiteSoftClamp = 0.42f,
                    PastelStrength = 0.31f,
                    HighSaturationCompress = 0.63f,
                    PastelLuminanceBias = 0.18f,
                    ShadowTint = new Color(0.2f, 0.3f, 0.4f, 1f),
                    MidTint = new Color(0.9f, 0.8f, 0.7f, 1f),
                    HighlightTint = new Color(1f, 0.95f, 0.7f, 1f),
                    CreamHighlightColor = new Color(0.98f, 0.94f, 0.86f, 1f),
                    EdgeToneEnabled = false,
                    EdgeToneColor = new Color(0.92f, 0.95f, 1f, 1f),
                    DepthHazeEnabled = false,
                    DepthHazeColor = new Color(0.72f, 0.82f, 0.9f, 1f),
                    SoftBloomEnabled = false,
                    SoftBloomTint = new Color(0.96f, 0.90f, 0.82f, 1f),
                    HalationEnabled = false,
                    HalationColor = new Color(1f, 0.68f, 0.54f, 1f),
                    BlueNoiseTex = blueNoiseTexture,
                    GrainEnabled = false,
                    ShadowTintStrength = 0.5f,
                    MidTintStrength = 0.15f,
                    HighlightTintStrength = 0.3f,
                    CreamHighlightStrength = 0.27f,
                    CreamHighlightThreshold = 0.72f,
                    CreamHighlightSoftness = 0.11f,
                    EdgeToneStrength = 0.14f,
                    EdgeToneRadius = 0.66f,
                    EdgeToneSoftness = 0.19f,
                    EdgeSaturationFade = 0.24f,
                    EdgeBrightnessOffset = -0.03f,
                    DepthHazeStrength = 0.16f,
                    DepthHazeStart = 0.41f,
                    DepthHazeEnd = 0.88f,
                    DepthHazeSaturationFade = 0.22f,
                    DepthHazeBrightnessLift = 0.07f,
                    SoftBloomThreshold = 0.76f,
                    SoftBloomSoftKnee = 0.24f,
                    SoftBloomIntensity = 0.19f,
                    SoftBloomRadius = 0.72f,
                    HalationStrength = 0.06f,
                    HalationThreshold = 0.83f,
                    HalationRadius = 0.61f,
                    GrainStrength = 0.03f,
                    GrainScale = 1.8f,
                    GrainResponse = 0.45f,
                    GrainTemporalStrength = 0.12f
                };

                settings.ApplyToMaterial(material);

                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaEnabled"));
                Assert.AreEqual((float)ToyDioramaQualityTier.High, material.GetFloat("_ToyDioramaQualityTier"));
                Assert.AreEqual((float)ToyDioramaDebugView.HighlightMask, material.GetFloat("_ToyDioramaDebugView"));
                Assert.AreEqual(0.75f, material.GetFloat("_ToyDioramaExposure"));
                Assert.AreEqual(1.35f, material.GetFloat("_ToyDioramaContrast"));
                Assert.AreEqual(1.1f, material.GetFloat("_ToyDioramaSaturation"));
                Assert.AreEqual(0.22f, material.GetFloat("_ToyDioramaBlackLift"));
                Assert.AreEqual(0.42f, material.GetFloat("_ToyDioramaWhiteSoftClamp"));
                Assert.AreEqual(0.31f, material.GetFloat("_ToyDioramaPastelStrength"));
                Assert.AreEqual(0.63f, material.GetFloat("_ToyDioramaHighSaturationCompress"));
                Assert.AreEqual(0.18f, material.GetFloat("_ToyDioramaPastelLuminanceBias"));
                Assert.AreEqual(0.5f, material.GetFloat("_ToyDioramaShadowTintStrength"));
                Assert.AreEqual(0.15f, material.GetFloat("_ToyDioramaMidTintStrength"));
                Assert.AreEqual(0.3f, material.GetFloat("_ToyDioramaHighlightTintStrength"));
                Assert.AreEqual(0.27f, material.GetFloat("_ToyDioramaCreamHighlightStrength"));
                Assert.AreEqual(0.72f, material.GetFloat("_ToyDioramaCreamHighlightThreshold"));
                Assert.AreEqual(0.11f, material.GetFloat("_ToyDioramaCreamHighlightSoftness"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaEdgeToneEnabled"));
                Assert.AreEqual(0.14f, material.GetFloat("_ToyDioramaEdgeToneStrength"));
                Assert.AreEqual(0.66f, material.GetFloat("_ToyDioramaEdgeToneRadius"));
                Assert.AreEqual(0.19f, material.GetFloat("_ToyDioramaEdgeToneSoftness"));
                Assert.AreEqual(0.24f, material.GetFloat("_ToyDioramaEdgeSaturationFade"));
                Assert.AreEqual(-0.03f, material.GetFloat("_ToyDioramaEdgeBrightnessOffset"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaDepthHazeEnabled"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaDepthAvailable"));
                Assert.AreEqual(0.16f, material.GetFloat("_ToyDioramaDepthHazeStrength"));
                Assert.AreEqual(0.41f, material.GetFloat("_ToyDioramaDepthHazeStart"));
                Assert.AreEqual(0.88f, material.GetFloat("_ToyDioramaDepthHazeEnd"));
                Assert.AreEqual(0.22f, material.GetFloat("_ToyDioramaDepthHazeSaturationFade"));
                Assert.AreEqual(0.07f, material.GetFloat("_ToyDioramaDepthHazeBrightnessLift"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaSoftBloomEnabled"));
                Assert.AreEqual(0.76f, material.GetFloat("_ToyDioramaSoftBloomThreshold"));
                Assert.AreEqual(0.24f, material.GetFloat("_ToyDioramaSoftBloomSoftKnee"));
                Assert.AreEqual(0.19f, material.GetFloat("_ToyDioramaSoftBloomIntensity"));
                Assert.AreEqual(0.72f, material.GetFloat("_ToyDioramaSoftBloomRadius"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaHalationEnabled"));
                Assert.AreEqual(0.06f, material.GetFloat("_ToyDioramaHalationStrength"));
                Assert.AreEqual(0.83f, material.GetFloat("_ToyDioramaHalationThreshold"));
                Assert.AreEqual(0.61f, material.GetFloat("_ToyDioramaHalationRadius"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaGrainEnabled"));
                Assert.AreEqual(0.03f, material.GetFloat("_ToyDioramaGrainStrength"));
                Assert.AreEqual(1.8f, material.GetFloat("_ToyDioramaGrainScale"));
                Assert.AreEqual(0.45f, material.GetFloat("_ToyDioramaGrainResponse"));
                Assert.AreEqual(0.12f, material.GetFloat("_ToyDioramaGrainTemporalStrength"));
                AssertColorApproximately(new Color(0.2f, 0.3f, 0.4f, 1f), material.GetColor("_ToyDioramaShadowTint"));
                AssertColorApproximately(new Color(0.9f, 0.8f, 0.7f, 1f), material.GetColor("_ToyDioramaMidTint"));
                AssertColorApproximately(new Color(1f, 0.95f, 0.7f, 1f), material.GetColor("_ToyDioramaHighlightTint"));
                AssertColorApproximately(new Color(0.98f, 0.94f, 0.86f, 1f), material.GetColor("_ToyDioramaCreamHighlightColor"));
                AssertColorApproximately(new Color(0.92f, 0.95f, 1f, 1f), material.GetColor("_ToyDioramaEdgeToneColor"));
                AssertColorApproximately(new Color(0.72f, 0.82f, 0.9f, 1f), material.GetColor("_ToyDioramaDepthHazeColor"));
                AssertColorApproximately(new Color(0.96f, 0.90f, 0.82f, 1f), material.GetColor("_ToyDioramaSoftBloomTint"));
                AssertColorApproximately(new Color(1f, 0.68f, 0.54f, 1f), material.GetColor("_ToyDioramaHalationColor"));
                Assert.AreEqual(blueNoiseTexture, material.GetTexture("_ToyDioramaBlueNoiseTex"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(blueNoiseTexture);
            }
        }

        [Test]
        public void LowQualityDisablesExpensiveEffectsAndPreBloomDebugSkipsFinalComposite()
        {
            Shader shader = Shader.Find("BC/PostProcess/ToyDioramaComposite");
            Assert.IsNotNull(shader);

            Texture2D blueNoiseTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Material material = new Material(shader);

            try
            {
                ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings
                {
                    QualityTier = ToyDioramaQualityTier.Low,
                    DebugView = ToyDioramaDebugView.BeforeBloom,
                    DepthHazeEnabled = true,
                    SoftBloomEnabled = true,
                    HalationEnabled = true,
                    GrainEnabled = true,
                    BlueNoiseTex = blueNoiseTexture
                };

                settings.ApplyToMaterial(material);

                Assert.IsFalse(settings.IsDepthHazeEnabledForQuality());
                Assert.IsFalse(settings.IsSoftBloomEnabledForQuality());
                Assert.IsFalse(settings.IsHalationEnabledForQuality());
                Assert.IsFalse(settings.IsGrainEnabledForQuality());
                Assert.IsFalse(settings.RequiresBloomPass());
                Assert.IsFalse(settings.RequiresFinalCompositePass());
                Assert.AreEqual(4, settings.GetBloomDownsampleDivisor());
                Assert.AreEqual(1, settings.GetBloomBlurPassPairCount());
                Assert.AreEqual((float)ToyDioramaDebugView.BeforeBloom, material.GetFloat("_ToyDioramaDebugView"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaDepthHazeEnabled"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaSoftBloomEnabled"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaHalationEnabled"));
                Assert.AreEqual(0f, material.GetFloat("_ToyDioramaGrainEnabled"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(blueNoiseTexture);
            }
        }

        [Test]
        public void MediumAndHighQualityAdjustBloomAndHalationRequirements()
        {
            ToyDioramaPostProcessSettings mediumSettings = new ToyDioramaPostProcessSettings
            {
                QualityTier = ToyDioramaQualityTier.Medium,
                SoftBloomEnabled = true,
                SoftBloomIntensity = 0.14f,
                HalationEnabled = true,
                HalationStrength = 0.04f,
                DebugView = ToyDioramaDebugView.Off
            };

            Assert.IsTrue(mediumSettings.IsSoftBloomEnabledForQuality());
            Assert.IsFalse(mediumSettings.IsHalationEnabledForQuality());
            Assert.IsTrue(mediumSettings.RequiresBloomPass());
            Assert.IsTrue(mediumSettings.RequiresFinalCompositePass());
            Assert.AreEqual(4, mediumSettings.GetBloomDownsampleDivisor());
            Assert.AreEqual(1, mediumSettings.GetBloomBlurPassPairCount());

            ToyDioramaPostProcessSettings highSettings = new ToyDioramaPostProcessSettings
            {
                QualityTier = ToyDioramaQualityTier.High,
                SoftBloomEnabled = false,
                SoftBloomIntensity = 0f,
                HalationEnabled = true,
                HalationStrength = 0.04f,
                DebugView = ToyDioramaDebugView.BloomComposite
            };

            Assert.IsFalse(highSettings.IsSoftBloomEnabledForQuality());
            Assert.IsTrue(highSettings.IsHalationEnabledForQuality());
            Assert.IsTrue(highSettings.RequiresBloomPass());
            Assert.IsTrue(highSettings.RequiresFinalCompositePass());
            Assert.AreEqual(2, highSettings.GetBloomDownsampleDivisor());
            Assert.AreEqual(2, highSettings.GetBloomBlurPassPairCount());

            ToyDioramaPostProcessSettings cinematicSettings = new ToyDioramaPostProcessSettings
            {
                QualityTier = ToyDioramaQualityTier.Cinematic,
                HalationEnabled = true,
                HalationStrength = 0.03f
            };

            Assert.IsTrue(cinematicSettings.IsHalationEnabledForQuality());
            Assert.AreEqual(2, cinematicSettings.GetBloomDownsampleDivisor());
            Assert.AreEqual(3, cinematicSettings.GetBloomBlurPassPairCount());
        }

        [Test]
        public void ApplyPresetCopiesAuthoringValuesWithoutOverwritingRuntimeState()
        {
            ToyDioramaPostProcessPreset preset = ScriptableObject.CreateInstance<ToyDioramaPostProcessPreset>();

            try
            {
                preset.Settings.Enabled = true;
                preset.Settings.QualityTier = ToyDioramaQualityTier.Low;
                preset.Settings.DebugView = ToyDioramaDebugView.BloomComposite;
                preset.Settings.Exposure = 0.35f;
                preset.Settings.PastelStrength = 0.44f;
                preset.Settings.EdgeToneEnabled = false;
                preset.Settings.SoftBloomEnabled = false;
                preset.Settings.GrainEnabled = false;

                ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings
                {
                    Enabled = false,
                    QualityTier = ToyDioramaQualityTier.Cinematic,
                    DebugView = ToyDioramaDebugView.AfterBloom,
                    Exposure = -0.2f,
                    PastelStrength = 0.1f,
                    EdgeToneEnabled = true,
                    SoftBloomEnabled = true,
                    GrainEnabled = true
                };

                settings.ApplyPreset(preset);

                Assert.IsFalse(settings.Enabled);
                Assert.AreEqual(ToyDioramaQualityTier.Cinematic, settings.QualityTier);
                Assert.AreEqual(ToyDioramaDebugView.AfterBloom, settings.DebugView);
                Assert.AreEqual(0.35f, settings.Exposure);
                Assert.AreEqual(0.44f, settings.PastelStrength);
                Assert.IsFalse(settings.EdgeToneEnabled);
                Assert.IsFalse(settings.SoftBloomEnabled);
                Assert.IsFalse(settings.GrainEnabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preset);
            }
        }

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.0001f);
            Assert.AreEqual(expected.g, actual.g, 0.0001f);
            Assert.AreEqual(expected.b, actual.b, 0.0001f);
            Assert.AreEqual(expected.a, actual.a, 0.0001f);
        }
    }
}