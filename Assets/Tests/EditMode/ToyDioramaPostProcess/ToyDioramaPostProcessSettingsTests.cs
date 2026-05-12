using BC.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace BC.Rendering.Tests
{
    public sealed class ToyDioramaPostProcessSettingsTests
    {
        [Test]
        public void DefaultsProvideM4ColorGradeValues()
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
            AssertColorApproximately(new Color(1f, 0.98f, 0.93f, 1f), settings.CreamHighlightColor);
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
        }

        [Test]
        public void ApplyToMaterialBindsM4Properties()
        {
            Shader shader = Shader.Find("BC/PostProcess/ToyDioramaComposite");
            Assert.IsNotNull(shader);

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
                    ShadowTintStrength = 0.5f,
                    MidTintStrength = 0.15f,
                    HighlightTintStrength = 0.3f,
                    CreamHighlightStrength = 0.27f,
                    CreamHighlightThreshold = 0.72f,
                    CreamHighlightSoftness = 0.11f
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
                AssertColorApproximately(new Color(0.2f, 0.3f, 0.4f, 1f), material.GetColor("_ToyDioramaShadowTint"));
                AssertColorApproximately(new Color(0.9f, 0.8f, 0.7f, 1f), material.GetColor("_ToyDioramaMidTint"));
                AssertColorApproximately(new Color(1f, 0.95f, 0.7f, 1f), material.GetColor("_ToyDioramaHighlightTint"));
                AssertColorApproximately(new Color(0.98f, 0.94f, 0.86f, 1f), material.GetColor("_ToyDioramaCreamHighlightColor"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
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