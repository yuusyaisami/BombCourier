using BC.ActionSystem;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(TalkChoiceOptionAuthoring))]
    public sealed class TalkChoiceOptionAuthoringDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty displayTextProperty = property.FindPropertyRelative("displayText");
            SerializedProperty outcomeKindProperty = property.FindPropertyRelative("outcomeKind");

            if (displayTextProperty == null || outcomeKindProperty == null)
                return LineHeight * 2f;

            float height = 0f;
            height += GetRowHeight(displayTextProperty);
            height += GetRowHeight(outcomeKindProperty);

            if (!TryGetOutcomePayloadProperty(property, outcomeKindProperty, out SerializedProperty payloadProperty, out string helpMessage))
            {
                height += GetHelpHeight(helpMessage);
                return Mathf.Max(LineHeight, height - Spacing);
            }

            height += GetRowHeight(payloadProperty);
            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty displayTextProperty = property.FindPropertyRelative("displayText");
            SerializedProperty outcomeKindProperty = property.FindPropertyRelative("outcomeKind");

            if (displayTextProperty == null || outcomeKindProperty == null)
            {
                DrawMissingField(position, label, "Talk choice option fields are missing.");
                return;
            }

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            EditorGUI.PropertyField(rowRect, displayTextProperty, new GUIContent("Display Text"));
            rowRect.y += LineHeight + Spacing;

            EditorGUI.PropertyField(rowRect, outcomeKindProperty, new GUIContent("Outcome Kind"));
            rowRect.y += LineHeight + Spacing;

            if (!TryGetOutcomePayloadProperty(property, outcomeKindProperty, out SerializedProperty payloadProperty, out string helpMessage))
            {
                Rect helpRect = new(contentRect.x, rowRect.y, contentRect.width, GetHelpHeight(helpMessage));
                EditorGUI.HelpBox(helpRect, helpMessage, MessageType.Info);
                return;
            }

            float payloadHeight = EditorGUI.GetPropertyHeight(payloadProperty, true);
            Rect payloadRect = new(contentRect.x, rowRect.y, contentRect.width, payloadHeight);
            EditorGUI.PropertyField(payloadRect, payloadProperty, GetPayloadLabel(payloadProperty, outcomeKindProperty), true);
        }

        private static bool TryGetOutcomePayloadProperty(
            SerializedProperty optionProperty,
            SerializedProperty outcomeKindProperty,
            out SerializedProperty payloadProperty,
            out string helpMessage)
        {
            payloadProperty = null;

            TalkChoiceOptionOutcomeKind outcomeKind = (TalkChoiceOptionOutcomeKind)outcomeKindProperty.enumValueIndex;

            switch (outcomeKind)
            {
                case TalkChoiceOptionOutcomeKind.InlineAction:
                    payloadProperty = optionProperty.FindPropertyRelative("inlineAction");
                    helpMessage = payloadProperty == null
                        ? "InlineAction field was not found."
                        : null;
                    return payloadProperty != null;

                case TalkChoiceOptionOutcomeKind.ValueStoreWrite:
                    payloadProperty = optionProperty.FindPropertyRelative("valueStoreWrite");
                    helpMessage = payloadProperty == null
                        ? "ValueStoreWrite field was not found."
                        : null;
                    return payloadProperty != null;

                case TalkChoiceOptionOutcomeKind.None:
                    helpMessage = "No outcome is configured for this option.";
                    return false;

                default:
                    helpMessage = $"Unsupported outcome kind: {outcomeKind}.";
                    return false;
            }
        }

        private static GUIContent GetPayloadLabel(
            SerializedProperty payloadProperty,
            SerializedProperty outcomeKindProperty)
        {
            TalkChoiceOptionOutcomeKind outcomeKind = (TalkChoiceOptionOutcomeKind)outcomeKindProperty.enumValueIndex;

            return outcomeKind switch
            {
                TalkChoiceOptionOutcomeKind.InlineAction => new GUIContent("Inline Action"),
                TalkChoiceOptionOutcomeKind.ValueStoreWrite => new GUIContent("Value Store Write"),
                _ => new GUIContent(ObjectNames.NicifyVariableName(payloadProperty.name)),
            };
        }

        private static float GetRowHeight(SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(property, true) + Spacing;
        }

        private static float GetHelpHeight(string message)
        {
            string text = string.IsNullOrWhiteSpace(message) ? "No details." : message;
            return Mathf.Max(LineHeight * 2f, EditorStyles.helpBox.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth)) + Spacing;
        }
    }
}
