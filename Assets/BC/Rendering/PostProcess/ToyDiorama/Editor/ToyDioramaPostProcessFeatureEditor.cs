using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering.Editor
{
    [CustomEditor(typeof(ToyDioramaPostProcessFeature))]
    public sealed class ToyDioramaPostProcessFeatureEditor : UnityEditor.Editor
    {
        private static readonly string[] VolumeSearchFolders = { "Assets/Settings" };

        private SerializedProperty compositeShaderProperty;
        private SerializedProperty bloomShaderProperty;
        private SerializedProperty passEventProperty;
        private SerializedProperty sceneViewEnabledProperty;
        private SerializedProperty selectedPresetProperty;
        private SerializedProperty settingsProperty;

        private void OnEnable()
        {
            compositeShaderProperty = serializedObject.FindProperty("compositeShader");
            bloomShaderProperty = serializedObject.FindProperty("bloomShader");
            passEventProperty = serializedObject.FindProperty("passEvent");
            sceneViewEnabledProperty = serializedObject.FindProperty("sceneViewEnabled");
            selectedPresetProperty = serializedObject.FindProperty("selectedPreset");
            settingsProperty = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPresetSection();
            DrawInjectionPolicySection();
            ToyDioramaPostProcessInspectorUtility.DrawFeatureSettings(settingsProperty);
            DrawRuntimeResources();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInjectionPolicySection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Injection Policy", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sceneViewEnabledProperty, new GUIContent("Scene View Enabled"));

            ToyDioramaPostProcessFeature feature = (ToyDioramaPostProcessFeature)target;
            EditorGUILayout.HelpBox(feature.GetCameraPolicySummary(), MessageType.None);
            EditorGUILayout.HelpBox(
                "World Space UI may receive ToyDiorama. Screen Space UI should stay outside the ToyDiorama camera path.",
                MessageType.Info);

            if (TryBuildProjectPostProcessWarning(out string warningMessage))
            {
                EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
            }
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(selectedPresetProperty);
            EditorGUILayout.HelpBox(
                "Applying a preset copies visual values into the current settings. You can keep editing values manually after the apply step.",
                MessageType.Info);

            ToyDioramaPostProcessPreset selectedPreset = selectedPresetProperty.objectReferenceValue as ToyDioramaPostProcessPreset;

            if (selectedPreset != null && !string.IsNullOrWhiteSpace(selectedPreset.Description))
            {
                EditorGUILayout.HelpBox(selectedPreset.Description, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(selectedPreset == null))
            {
                if (GUILayout.Button("Apply Selected Preset"))
                {
                    serializedObject.ApplyModifiedProperties();

                    ToyDioramaPostProcessFeature feature = (ToyDioramaPostProcessFeature)target;
                    Undo.RecordObject(feature, "Apply Toy Diorama Preset");
                    feature.ApplySelectedPreset();
                    EditorUtility.SetDirty(feature);

                    serializedObject.Update();
                }
            }
        }

        private void DrawRuntimeResources()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Resources", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(compositeShaderProperty);
            EditorGUILayout.PropertyField(bloomShaderProperty);
            EditorGUILayout.PropertyField(passEventProperty);
        }

        private static bool TryBuildProjectPostProcessWarning(out string warningMessage)
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
    }
}