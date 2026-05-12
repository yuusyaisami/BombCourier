using System;
using System.Collections.Generic;
using System.Linq;
using BC.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Rendering.Tests
{
    public sealed class ToyDioramaPostProcessPresetAssetTests
    {
        private const string PresetsFolder = "Assets/BC/Rendering/PostProcess/ToyDiorama/Presets";

        [Test]
        public void RequiredPresetAssetsExist()
        {
            Dictionary<ToyDioramaPresetKind, ToyDioramaPostProcessPreset> presets = LoadPresetDictionary();

            Array expectedKinds = Enum.GetValues(typeof(ToyDioramaPresetKind));
            CollectionAssert.AreEquivalent(expectedKinds, presets.Keys);
            Assert.AreEqual("SoftToy", presets[ToyDioramaPresetKind.SoftToy].name);
            Assert.AreEqual("ClayDiorama", presets[ToyDioramaPresetKind.ClayDiorama].name);
            Assert.AreEqual("MattePlastic", presets[ToyDioramaPresetKind.MattePlastic].name);
            Assert.AreEqual("PictureBook", presets[ToyDioramaPresetKind.PictureBook].name);
            Assert.AreEqual("CleanDebug", presets[ToyDioramaPresetKind.CleanDebug].name);
        }

        [Test]
        public void SoftToyPresetMatchesDefaultAuthoringValues()
        {
            ToyDioramaPostProcessSettings expectedDefaults = new ToyDioramaPostProcessSettings();
            ToyDioramaPostProcessPreset softToyPreset = LoadPresetDictionary()[ToyDioramaPresetKind.SoftToy];

            AssertSettingsAuthoringValuesMatch(expectedDefaults, softToyPreset.Settings);
        }

        [Test]
        public void CleanDebugPresetDisablesSecondaryEffects()
        {
            ToyDioramaPostProcessPreset cleanDebugPreset = LoadPresetDictionary()[ToyDioramaPresetKind.CleanDebug];
            ToyDioramaPostProcessSettings settings = cleanDebugPreset.Settings;

            Assert.AreEqual(0f, settings.BlackLift);
            Assert.AreEqual(0f, settings.WhiteSoftClamp);
            Assert.AreEqual(0f, settings.PastelStrength);
            Assert.AreEqual(0f, settings.HighSaturationCompress);
            Assert.AreEqual(0f, settings.CreamHighlightStrength);
            Assert.IsFalse(settings.EdgeToneEnabled);
            Assert.IsFalse(settings.DepthHazeEnabled);
            Assert.IsFalse(settings.SoftBloomEnabled);
            Assert.IsFalse(settings.HalationEnabled);
            Assert.IsFalse(settings.GrainEnabled);
            Assert.AreEqual(0f, settings.ShadowTintStrength);
            Assert.AreEqual(0f, settings.MidTintStrength);
            Assert.AreEqual(0f, settings.HighlightTintStrength);
        }

        private static Dictionary<ToyDioramaPresetKind, ToyDioramaPostProcessPreset> LoadPresetDictionary()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:ToyDioramaPostProcessPreset", new[] { PresetsFolder });
            Assert.AreEqual(5, assetGuids.Length, "Expected 5 ToyDiorama preset assets.");

            return assetGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ToyDioramaPostProcessPreset>)
                .ToDictionary(preset => preset.PresetKind, preset => preset);
        }

        private static void AssertSettingsAuthoringValuesMatch(ToyDioramaPostProcessSettings expected, ToyDioramaPostProcessSettings actual)
        {
            Assert.AreEqual(expected.Exposure, actual.Exposure);
            Assert.AreEqual(expected.Contrast, actual.Contrast);
            Assert.AreEqual(expected.Saturation, actual.Saturation);
            Assert.AreEqual(expected.BlackLift, actual.BlackLift);
            Assert.AreEqual(expected.WhiteSoftClamp, actual.WhiteSoftClamp);
            Assert.AreEqual(expected.PastelStrength, actual.PastelStrength);
            Assert.AreEqual(expected.HighSaturationCompress, actual.HighSaturationCompress);
            Assert.AreEqual(expected.PastelLuminanceBias, actual.PastelLuminanceBias);
            Assert.AreEqual(expected.CreamHighlightStrength, actual.CreamHighlightStrength);
            Assert.AreEqual(expected.CreamHighlightThreshold, actual.CreamHighlightThreshold);
            Assert.AreEqual(expected.CreamHighlightSoftness, actual.CreamHighlightSoftness);
            Assert.AreEqual(expected.EdgeToneEnabled, actual.EdgeToneEnabled);
            Assert.AreEqual(expected.EdgeToneStrength, actual.EdgeToneStrength);
            Assert.AreEqual(expected.EdgeToneRadius, actual.EdgeToneRadius);
            Assert.AreEqual(expected.EdgeToneSoftness, actual.EdgeToneSoftness);
            Assert.AreEqual(expected.EdgeSaturationFade, actual.EdgeSaturationFade);
            Assert.AreEqual(expected.EdgeBrightnessOffset, actual.EdgeBrightnessOffset);
            Assert.AreEqual(expected.DepthHazeEnabled, actual.DepthHazeEnabled);
            Assert.AreEqual(expected.DepthHazeStrength, actual.DepthHazeStrength);
            Assert.AreEqual(expected.DepthHazeStart, actual.DepthHazeStart);
            Assert.AreEqual(expected.DepthHazeEnd, actual.DepthHazeEnd);
            Assert.AreEqual(expected.DepthHazeSaturationFade, actual.DepthHazeSaturationFade);
            Assert.AreEqual(expected.DepthHazeBrightnessLift, actual.DepthHazeBrightnessLift);
            Assert.AreEqual(expected.SoftBloomEnabled, actual.SoftBloomEnabled);
            Assert.AreEqual(expected.SoftBloomThreshold, actual.SoftBloomThreshold);
            Assert.AreEqual(expected.SoftBloomSoftKnee, actual.SoftBloomSoftKnee);
            Assert.AreEqual(expected.SoftBloomIntensity, actual.SoftBloomIntensity);
            Assert.AreEqual(expected.SoftBloomRadius, actual.SoftBloomRadius);
            Assert.AreEqual(expected.HalationEnabled, actual.HalationEnabled);
            Assert.AreEqual(expected.HalationStrength, actual.HalationStrength);
            Assert.AreEqual(expected.HalationThreshold, actual.HalationThreshold);
            Assert.AreEqual(expected.HalationRadius, actual.HalationRadius);
            Assert.AreEqual(expected.GrainEnabled, actual.GrainEnabled);
            Assert.AreEqual(expected.GrainStrength, actual.GrainStrength);
            Assert.AreEqual(expected.GrainScale, actual.GrainScale);
            Assert.AreEqual(expected.GrainResponse, actual.GrainResponse);
            Assert.AreEqual(expected.GrainTemporalStrength, actual.GrainTemporalStrength);
            Assert.AreEqual(expected.BlueNoiseTex, actual.BlueNoiseTex);

            AssertColorApproximately(expected.ShadowTint, actual.ShadowTint);
            AssertColorApproximately(expected.MidTint, actual.MidTint);
            AssertColorApproximately(expected.HighlightTint, actual.HighlightTint);
            AssertColorApproximately(expected.CreamHighlightColor, actual.CreamHighlightColor);
            AssertColorApproximately(expected.EdgeToneColor, actual.EdgeToneColor);
            AssertColorApproximately(expected.DepthHazeColor, actual.DepthHazeColor);
            AssertColorApproximately(expected.SoftBloomTint, actual.SoftBloomTint);
            AssertColorApproximately(expected.HalationColor, actual.HalationColor);
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