using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.IMGUI
{
    public abstract class PropertyDrawerBase : PropertyDrawer
    {
        protected static float LineHeight => EditorThemeTokens.LineHeight;
        protected static float Spacing => EditorThemeTokens.StandardSpacing;

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return LineHeight;

            return Mathf.Max(LineHeight, GetPropertyHeightCore(property, label ?? GUIContent.none));
        }

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label ??= GUIContent.none;

            if (property == null)
            {
                DrawMissingField(position, label, "SerializedProperty is missing.");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            DrawProperty(position, property, label);
            EditorGUI.EndProperty();
        }

        protected virtual float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        protected abstract void DrawProperty(Rect position, SerializedProperty property, GUIContent label);

        protected static void DrawMissingField(Rect position, GUIContent label, string message)
        {
            string prefix = label == null || string.IsNullOrWhiteSpace(label.text) ? "Field" : label.text;
            EditorGUI.HelpBox(position, $"{prefix}: {message}", MessageType.Error);
        }

        protected static float GetChildHeight(SerializedProperty property, bool includeChildren = true)
        {
            return property == null ? 0f : EditorGUI.GetPropertyHeight(property, includeChildren);
        }

        protected static void DrawChild(ref Rect rect, SerializedProperty property, GUIContent label = null, bool includeChildren = true)
        {
            if (property == null)
                return;

            float height = EditorGUI.GetPropertyHeight(property, label ?? GUIContent.none, includeChildren);
            Rect rowRect = RectLayoutUtility.TakeHeight(ref rect, height);
            EditorGUI.PropertyField(rowRect, property, label ?? GUIContent.none, includeChildren);
        }
    }
}
