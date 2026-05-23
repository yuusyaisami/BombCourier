using BC.Editor.Foundation.IMGUI;
using BC.Managers;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(TalkRequestData))]
    public sealed class TalkRequestDataDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            bool showStartAction = IsToggleEnabled(property, "useOnStartTalkAction");
            bool showCompleteAction = IsToggleEnabled(property, "useOnCompleteTalkAction");

            float height = 0f;
            height += GetRowHeight(property.FindPropertyRelative("talkStateId"));
            height += GetRowHeight(property.FindPropertyRelative("speakerCharacter"));
            height += GetRowHeight(property.FindPropertyRelative("speakerName"));
            height += GetRowHeight(property.FindPropertyRelative("dialogueText"));
            height += GetRowHeight(property.FindPropertyRelative("textEffectData"));
            height += GetRowHeight(property.FindPropertyRelative("isWaitingActionCompleted"));
            height += GetRowHeight(property.FindPropertyRelative("useOnStartTalkAction"));

            if (showStartAction)
                height += GetRowHeight(property.FindPropertyRelative("onStartTalkAction"));

            height += GetRowHeight(property.FindPropertyRelative("useOnCompleteTalkAction"));

            if (showCompleteAction)
                height += GetRowHeight(property.FindPropertyRelative("onCompleteTalkAction"));

            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureLegacyFlagsInitialized(property);

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            DrawChildProperty(ref rowRect, property.FindPropertyRelative("talkStateId"), new GUIContent("Talk State"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("speakerCharacter"), new GUIContent("Speaker Character"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("speakerName"), new GUIContent("Legacy Speaker Label"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("dialogueText"), new GUIContent("Dialogue"), true);
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("textEffectData"), new GUIContent("Text Effect"), true);
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("isWaitingActionCompleted"), new GUIContent("Wait Action Completed"));

            SerializedProperty useStartProperty = property.FindPropertyRelative("useOnStartTalkAction");
            DrawChildProperty(ref rowRect, useStartProperty, new GUIContent("Use Start Talk Action"));

            if (useStartProperty?.boolValue == true)
                DrawChildProperty(ref rowRect, property.FindPropertyRelative("onStartTalkAction"), new GUIContent("On Start Talk Action"), true);

            SerializedProperty useCompleteProperty = property.FindPropertyRelative("useOnCompleteTalkAction");
            DrawChildProperty(ref rowRect, useCompleteProperty, new GUIContent("Use Complete Talk Action"));

            if (useCompleteProperty?.boolValue == true)
                DrawChildProperty(ref rowRect, property.FindPropertyRelative("onCompleteTalkAction"), new GUIContent("On Complete Talk Action"), true);
        }

        private static bool IsToggleEnabled(SerializedProperty talkRequestDataProperty, string togglePropertyName)
        {
            if (talkRequestDataProperty == null)
                return false;

            SerializedProperty versionProperty = talkRequestDataProperty.FindPropertyRelative("actionToggleVersion");
            SerializedProperty toggleProperty = talkRequestDataProperty.FindPropertyRelative(togglePropertyName);

            if (toggleProperty == null)
                return false;

            // New assets or not-yet-initialized assets should default to enabled to preserve existing behavior.
            if (versionProperty != null && versionProperty.intValue <= 0)
                return true;

            return toggleProperty.boolValue;
        }

        private static void EnsureLegacyFlagsInitialized(SerializedProperty talkRequestDataProperty)
        {
            if (talkRequestDataProperty == null)
                return;

            SerializedProperty versionProperty = talkRequestDataProperty.FindPropertyRelative("actionToggleVersion");
            if (versionProperty == null || versionProperty.intValue > 0)
                return;

            SerializedProperty useStartProperty = talkRequestDataProperty.FindPropertyRelative("useOnStartTalkAction");
            SerializedProperty useCompleteProperty = talkRequestDataProperty.FindPropertyRelative("useOnCompleteTalkAction");

            if (useStartProperty != null)
                useStartProperty.boolValue = true;

            if (useCompleteProperty != null)
                useCompleteProperty.boolValue = true;

            versionProperty.intValue = 1;
        }

        private static float GetRowHeight(SerializedProperty property)
        {
            if (property == null)
                return LineHeight + Spacing;

            return EditorGUI.GetPropertyHeight(property, true) + Spacing;
        }

        private static void DrawChildProperty(ref Rect rowRect, SerializedProperty property, GUIContent label, bool includeChildren = false)
        {
            if (property == null)
                return;

            float height = EditorGUI.GetPropertyHeight(property, label, includeChildren);
            Rect fieldRect = new(rowRect.x, rowRect.y, rowRect.width, height);
            EditorGUI.PropertyField(fieldRect, property, label, includeChildren);
            rowRect.y += height + Spacing;
        }
    }
}
