using UnityEditor;

namespace BC.Rendering.Editor
{
    [CustomEditor(typeof(ToyDioramaPostProcessPreset))]
    public sealed class ToyDioramaPostProcessPresetEditor : UnityEditor.Editor
    {
        private SerializedProperty presetKindProperty;
        private SerializedProperty descriptionProperty;
        private SerializedProperty settingsProperty;

        private void OnEnable()
        {
            presetKindProperty = serializedObject.FindProperty("presetKind");
            descriptionProperty = serializedObject.FindProperty("description");
            settingsProperty = serializedObject.FindProperty("settings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(presetKindProperty);
            EditorGUILayout.PropertyField(descriptionProperty);
            ToyDioramaPostProcessInspectorUtility.DrawPresetSettings(settingsProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}