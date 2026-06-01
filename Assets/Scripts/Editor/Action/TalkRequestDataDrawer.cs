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

    [CustomPropertyDrawer(typeof(DialogueRequestData))]
    public sealed class DialogueRequestDataDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            bool showStartAction = IsToggleEnabled(property, "useOnStartDialogueAction");
            bool showCompleteAction = IsToggleEnabled(property, "useOnCompleteDialogueAction");

            float height = 0f;
            height += GetRowHeight(property.FindPropertyRelative("speakerCharacter"));
            height += GetRowHeight(property.FindPropertyRelative("speakerName"));
            height += GetRowHeight(property.FindPropertyRelative("dialogueText"));
            height += GetRowHeight(property.FindPropertyRelative("textEffectData"));
            height += GetRowHeight(property.FindPropertyRelative("hideDuration"));
            height += GetRowHeight(property.FindPropertyRelative("isWaitingActionCompleted"));
            height += GetRowHeight(property.FindPropertyRelative("useOnStartDialogueAction"));

            if (showStartAction)
                height += GetRowHeight(property.FindPropertyRelative("onStartDialogueAction"));

            height += GetRowHeight(property.FindPropertyRelative("useOnCompleteDialogueAction"));

            if (showCompleteAction)
                height += GetRowHeight(property.FindPropertyRelative("onCompleteDialogueAction"));

            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureLegacyFlagsInitialized(property);

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            DrawChildProperty(ref rowRect, property.FindPropertyRelative("speakerCharacter"), new GUIContent("Speaker Character"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("speakerName"), new GUIContent("Speaker Name"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("dialogueText"), new GUIContent("Dialogue"), true);
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("textEffectData"), new GUIContent("Text Effect"), true);
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("hideDuration"), new GUIContent("Hide Duration"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("isWaitingActionCompleted"), new GUIContent("Wait Action Completed"));

            SerializedProperty useStartProperty = property.FindPropertyRelative("useOnStartDialogueAction");
            DrawChildProperty(ref rowRect, useStartProperty, new GUIContent("Use Start Dialogue Action"));

            if (useStartProperty?.boolValue == true)
                DrawChildProperty(ref rowRect, property.FindPropertyRelative("onStartDialogueAction"), new GUIContent("On Start Dialogue Action"), true);

            SerializedProperty useCompleteProperty = property.FindPropertyRelative("useOnCompleteDialogueAction");
            DrawChildProperty(ref rowRect, useCompleteProperty, new GUIContent("Use Complete Dialogue Action"));

            if (useCompleteProperty?.boolValue == true)
                DrawChildProperty(ref rowRect, property.FindPropertyRelative("onCompleteDialogueAction"), new GUIContent("On Complete Dialogue Action"), true);
        }

        private static bool IsToggleEnabled(SerializedProperty dialogueRequestDataProperty, string togglePropertyName)
        {
            if (dialogueRequestDataProperty == null)
                return false;

            SerializedProperty versionProperty = dialogueRequestDataProperty.FindPropertyRelative("actionToggleVersion");
            SerializedProperty toggleProperty = dialogueRequestDataProperty.FindPropertyRelative(togglePropertyName);

            if (toggleProperty == null)
                return false;

            if (versionProperty != null && versionProperty.intValue <= 0)
                return true;

            return toggleProperty.boolValue;
        }

        private static void EnsureLegacyFlagsInitialized(SerializedProperty dialogueRequestDataProperty)
        {
            if (dialogueRequestDataProperty == null)
                return;

            SerializedProperty versionProperty = dialogueRequestDataProperty.FindPropertyRelative("actionToggleVersion");
            if (versionProperty == null || versionProperty.intValue > 0)
                return;

            SerializedProperty useStartProperty = dialogueRequestDataProperty.FindPropertyRelative("useOnStartDialogueAction");
            SerializedProperty useCompleteProperty = dialogueRequestDataProperty.FindPropertyRelative("useOnCompleteDialogueAction");

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

    [CustomPropertyDrawer(typeof(ScreenOverlayDisplayId))]
    public sealed class ScreenOverlayDisplayIdDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("value");
            EditorGUI.PropertyField(position, valueProperty, label ?? new GUIContent("Display Id"));
        }
    }

    [CustomPropertyDrawer(typeof(ScreenOverlayShowRequestData))]
    public sealed class ScreenOverlayShowRequestDataDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            EnsureDefaultsInitialized(property);

            float height = 0f;
            height += GetRowHeight(property.FindPropertyRelative("displayId"));
            height += GetRowHeight(property.FindPropertyRelative("contentKind"));
            height += GetRowHeight(property.FindPropertyRelative("anchoredPosition"));
            height += GetRowHeight(property.FindPropertyRelative("sortOrder"));

            ScreenOverlayContentKind contentKind = GetContentKind(property);
            switch (contentKind)
            {
                case ScreenOverlayContentKind.Image:
                    height += GetRowHeight(property.FindPropertyRelative("sprite"));
                    height += GetRowHeight(property.FindPropertyRelative("size"));
                    height += GetRowHeight(property.FindPropertyRelative("imageColor"));
                    break;

                case ScreenOverlayContentKind.Text:
                    height += GetRowHeight(property.FindPropertyRelative("text"));
                    height += GetRowHeight(property.FindPropertyRelative("fontSize"));
                    height += GetRowHeight(property.FindPropertyRelative("textColor"));
                    break;

                case ScreenOverlayContentKind.Prefab:
                    height += GetRowHeight(property.FindPropertyRelative("prefab"));
                    break;
            }

            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureDefaultsInitialized(property);

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            DrawChildProperty(ref rowRect, property.FindPropertyRelative("displayId"), new GUIContent("Display Id"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("contentKind"), new GUIContent("Content Kind"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("anchoredPosition"), new GUIContent("Anchored Position"));
            DrawChildProperty(ref rowRect, property.FindPropertyRelative("sortOrder"), new GUIContent("Sort Order"));

            ScreenOverlayContentKind contentKind = GetContentKind(property);
            switch (contentKind)
            {
                case ScreenOverlayContentKind.Image:
                    DrawChildProperty(ref rowRect, property.FindPropertyRelative("sprite"), new GUIContent("Sprite"));
                    DrawChildProperty(ref rowRect, property.FindPropertyRelative("size"), new GUIContent("Size"));
                    DrawChildProperty(ref rowRect, property.FindPropertyRelative("imageColor"), new GUIContent("Color"));
                    break;

                case ScreenOverlayContentKind.Text:
                    DrawChildProperty(ref rowRect, property.FindPropertyRelative("text"), new GUIContent("Text"), true);
                    DrawChildProperty(ref rowRect, property.FindPropertyRelative("fontSize"), new GUIContent("Font Size"));
                    DrawChildProperty(ref rowRect, property.FindPropertyRelative("textColor"), new GUIContent("Color"));
                    break;

                case ScreenOverlayContentKind.Prefab:
                    DrawChildProperty(ref rowRect, property.FindPropertyRelative("prefab"), new GUIContent("Prefab"));
                    break;
            }
        }

        private static ScreenOverlayContentKind GetContentKind(SerializedProperty property)
        {
            SerializedProperty contentKindProperty = property.FindPropertyRelative("contentKind");
            return contentKindProperty != null
                ? (ScreenOverlayContentKind)contentKindProperty.enumValueIndex
                : ScreenOverlayContentKind.Image;
        }

        private static void EnsureDefaultsInitialized(SerializedProperty property)
        {
            if (property == null)
                return;

            SerializedProperty versionProperty = property.FindPropertyRelative("defaultsVersion");
            if (versionProperty == null || versionProperty.intValue > 0)
                return;

            SerializedProperty imageColorProperty = property.FindPropertyRelative("imageColor");
            if (imageColorProperty != null && imageColorProperty.colorValue == default)
                imageColorProperty.colorValue = Color.white;

            SerializedProperty textColorProperty = property.FindPropertyRelative("textColor");
            if (textColorProperty != null && textColorProperty.colorValue == default)
                textColorProperty.colorValue = Color.white;

            SerializedProperty fontSizeProperty = property.FindPropertyRelative("fontSize");
            if (fontSizeProperty != null && fontSizeProperty.floatValue <= 0f)
                fontSizeProperty.floatValue = 36f;

            SerializedProperty sizeProperty = property.FindPropertyRelative("size");
            if (sizeProperty != null && sizeProperty.vector2Value == Vector2.zero)
                sizeProperty.vector2Value = new Vector2(128f, 128f);

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

    [CustomPropertyDrawer(typeof(ScreenOverlayHideRequestData))]
    public sealed class ScreenOverlayHideRequestDataDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("displayId"), true);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty displayIdProperty = property.FindPropertyRelative("displayId");
            EditorGUI.PropertyField(position, displayIdProperty, new GUIContent("Display Id"), true);
        }
    }
}
