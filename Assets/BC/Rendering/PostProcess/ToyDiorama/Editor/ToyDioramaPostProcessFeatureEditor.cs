using UnityEditor;
using UnityEngine;

namespace BC.Rendering.Editor
{
    [CustomEditor(typeof(ToyDioramaPostProcessFeature))]
    public sealed class ToyDioramaPostProcessFeatureEditor : UnityEditor.Editor
    {
        private SerializedProperty compositeShaderProperty;
        private SerializedProperty bloomShaderProperty;
        private SerializedProperty passEventProperty;
        private SerializedProperty sceneViewEnabledProperty;
        private SerializedProperty forceLowQualityTierProperty;
        private SerializedProperty selectedPresetProperty;
        private SerializedProperty settingsProperty;

        private void OnEnable()
        {
            compositeShaderProperty = serializedObject.FindProperty("compositeShader");
            bloomShaderProperty = serializedObject.FindProperty("bloomShader");
            passEventProperty = serializedObject.FindProperty("passEvent");
            sceneViewEnabledProperty = serializedObject.FindProperty("sceneViewEnabled");
            forceLowQualityTierProperty = serializedObject.FindProperty("forceLowQualityTier");
            selectedPresetProperty = serializedObject.FindProperty("selectedPreset");
            settingsProperty = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ToyDioramaPostProcessFeature feature = (ToyDioramaPostProcessFeature)target;

            DrawPresetSection();
            DrawInjectionPolicySection();
            ToyDioramaPostProcessInspectorUtility.DrawFeatureSettings(feature, settingsProperty);
            DrawRuntimeResources();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInjectionPolicySection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Injection Policy", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sceneViewEnabledProperty, new GUIContent("Scene View Enabled"));
            EditorGUILayout.PropertyField(forceLowQualityTierProperty, new GUIContent("Force Low Quality Tier"));

            ToyDioramaPostProcessFeature feature = (ToyDioramaPostProcessFeature)target;
            EditorGUILayout.HelpBox(feature.GetCameraPolicySummary(), MessageType.None);

            if (feature.ForceLowQualityTier)
            {
                EditorGUILayout.HelpBox(
                    "This feature instance resolves runtime Quality Tier to Low without mutating the authored settings asset values.",
                    MessageType.Info);
            }

            if (ToyDioramaPostProcessBuildValidator.TryGetRendererPolicyBuildError(feature, out string policyError))
            {
                EditorGUILayout.HelpBox(policyError, MessageType.Error);
            }

            EditorGUILayout.HelpBox(
                "World Space UI may receive ToyDiorama. Screen Space UI should stay outside the ToyDiorama camera path.",
                MessageType.Info);

            if (ToyDioramaPostProcessBuildValidator.TryGetRendererOwnershipWarning(feature, out string rendererWarning))
            {
                EditorGUILayout.HelpBox(rendererWarning, MessageType.Warning);
            }

            if (ToyDioramaPostProcessBuildValidator.TryGetDebugViewAuthoringWarning(feature, out string debugViewWarning))
            {
                EditorGUILayout.HelpBox(debugViewWarning, MessageType.Warning);
            }

            if (ToyDioramaPostProcessBuildValidator.TryGetMobileQualityPolicyWarning(feature, out string mobileWarning))
            {
                EditorGUILayout.HelpBox(mobileWarning, MessageType.Warning);
            }

            if (ToyDioramaPostProcessBuildValidator.TryGetMobileAuthoredQualityTierWarning(feature, out string mobileAuthoredTierWarning))
            {
                EditorGUILayout.HelpBox(mobileAuthoredTierWarning, MessageType.Warning);
            }

            if (ToyDioramaPostProcessBuildValidator.TryGetProjectPostProcessOverlapWarning(out string warningMessage))
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
    }
}