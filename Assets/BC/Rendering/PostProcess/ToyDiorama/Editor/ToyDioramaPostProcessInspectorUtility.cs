using UnityEditor;

namespace BC.Rendering.Editor
{
    internal static class ToyDioramaPostProcessInspectorUtility
    {
        internal static void DrawFeatureSettings(ToyDioramaPostProcessFeature feature, SerializedProperty settingsProperty)
        {
            DrawPropertyGroup("Pipeline", settingsProperty, "enabled", "qualityTier");
            DrawQualitySummary(feature, settingsProperty.FindPropertyRelative("qualityTier"));
            DrawAuthoringSettings(settingsProperty);
            DrawPropertyGroup("Debug", settingsProperty, "debugView");
        }

        internal static void DrawPresetSettings(SerializedProperty settingsProperty)
        {
            EditorGUILayout.HelpBox(
                "Preset applies visual values only. Feature Enabled, Quality Tier, and Debug View are preserved when the preset is applied.",
                MessageType.Info);

            DrawAuthoringSettings(settingsProperty);
        }

        private static void DrawAuthoringSettings(SerializedProperty settingsProperty)
        {
            DrawPropertyGroup(
                "Core Color Grade",
                settingsProperty,
                "exposure",
                "contrast",
                "saturation",
                "blackLift",
                "whiteSoftClamp");

            DrawPropertyGroup(
                "Tints",
                settingsProperty,
                "shadowTint",
                "shadowTintStrength",
                "midTint",
                "midTintStrength",
                "highlightTint",
                "highlightTintStrength");

            DrawPropertyGroup(
                "Pastel Compression",
                settingsProperty,
                "pastelStrength",
                "highSaturationCompress",
                "pastelLuminanceBias");

            DrawPropertyGroup(
                "Cream Highlight",
                settingsProperty,
                "creamHighlightColor",
                "creamHighlightStrength",
                "creamHighlightThreshold",
                "creamHighlightSoftness");

            DrawPropertyGroup(
                "Edge Tone",
                settingsProperty,
                "edgeToneEnabled",
                "edgeToneColor",
                "edgeToneStrength",
                "edgeToneRadius",
                "edgeToneSoftness",
                "edgeSaturationFade",
                "edgeBrightnessOffset");

            DrawPropertyGroup(
                "Depth Haze",
                settingsProperty,
                "depthHazeEnabled",
                "depthHazeColor",
                "depthHazeStrength",
                "depthHazeStart",
                "depthHazeEnd",
                "depthHazeSaturationFade",
                "depthHazeBrightnessLift");

            DrawPropertyGroup(
                "Bloom And Halation",
                settingsProperty,
                "softBloomEnabled",
                "softBloomThreshold",
                "softBloomSoftKnee",
                "softBloomIntensity",
                "softBloomRadius",
                "softBloomTint",
                "halationEnabled",
                "halationStrength",
                "halationColor",
                "halationThreshold",
                "halationRadius");

            DrawPropertyGroup(
                "Grain",
                settingsProperty,
                "blueNoiseTex",
                "grainEnabled",
                "grainStrength",
                "grainScale",
                "grainResponse",
                "grainTemporalStrength");
        }

        private static void DrawQualitySummary(ToyDioramaPostProcessFeature feature, SerializedProperty qualityTierProperty)
        {
            if (qualityTierProperty == null)
            {
                return;
            }

            ToyDioramaQualityTier qualityTier = (ToyDioramaQualityTier)qualityTierProperty.enumValueIndex;
            EditorGUILayout.HelpBox($"Authored Quality Tier: {qualityTier}. {GetQualitySummary(qualityTier)}", MessageType.None);

            if (feature == null)
            {
                return;
            }

            ToyDioramaQualityTier resolvedQualityTier = feature.GetResolvedQualityTier();

            if (resolvedQualityTier == qualityTier)
            {
                EditorGUILayout.HelpBox(
                    $"Resolved Runtime Tier: {resolvedQualityTier}. Runtime matches the authored Quality Tier.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Resolved Runtime Tier: {resolvedQualityTier}. Runtime differs from authored {qualityTier} because this feature instance forces Low at runtime.",
                MessageType.Info);
        }

        private static string GetQualitySummary(ToyDioramaQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case ToyDioramaQualityTier.Low:
                    return "Low: Color Grade / Pastel / Cream Highlight / Edge Tone only. Depth Haze, Bloom, Halation, and Grain stay off.";

                case ToyDioramaQualityTier.Medium:
                    return "Medium: Adds Depth Haze, Grain, and simple Bloom while keeping the lighter blur path.";

                case ToyDioramaQualityTier.High:
                    return "High: Uses the higher quality Bloom path and enables Halation.";

                case ToyDioramaQualityTier.Cinematic:
                    return "Cinematic: Keeps High features and uses the strongest Bloom blur chain for final-quality output.";

                default:
                    return string.Empty;
            }
        }

        private static void DrawPropertyGroup(string title, SerializedProperty parentProperty, params string[] relativePropertyNames)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            foreach (string relativePropertyName in relativePropertyNames)
            {
                SerializedProperty property = parentProperty.FindPropertyRelative(relativePropertyName);

                if (property != null)
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
        }
    }
}