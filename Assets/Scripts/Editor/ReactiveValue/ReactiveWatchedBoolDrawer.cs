using System;
using BC.Base;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveWatchedBool))]
    public sealed class ReactiveWatchedBoolDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourceKindProperty = property.FindPropertyRelative("sourceKind");
            SerializedProperty failurePolicyProperty = property.FindPropertyRelative("failurePolicy");

            if (sourceKindProperty == null)
                return LineHeight;

            float height = GetControlDelta(LineHeight);
            height += GetControlDelta(LineHeight);
            height += GetSourcePayloadHeight(property, sourceKindProperty.enumValueIndex);

            if (failurePolicyProperty != null &&
                failurePolicyProperty.enumValueIndex == (int)ReactiveFailurePolicy.UseFallback)
            {
                height += GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("fallbackValue")));
            }

            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourceKindProperty = property.FindPropertyRelative("sourceKind");
            SerializedProperty failurePolicyProperty = property.FindPropertyRelative("failurePolicy");

            if (sourceKindProperty == null)
            {
                DrawMissingField(position, label, "ReactiveWatchedBool fields are missing.");
                return;
            }

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            EditorGUI.PropertyField(rowRect, sourceKindProperty, new GUIContent(label.text));

            rowRect.y += LineHeight + Spacing;
            if (failurePolicyProperty != null)
                EditorGUI.PropertyField(rowRect, failurePolicyProperty, new GUIContent("Failure"));

            rowRect.y += LineHeight + Spacing;
            EditorGUI.indentLevel++;
            DrawSourcePayload(ref rowRect, property, sourceKindProperty.enumValueIndex);

            if (failurePolicyProperty != null &&
                failurePolicyProperty.enumValueIndex == (int)ReactiveFailurePolicy.UseFallback)
            {
                DrawPropertyField(ref rowRect, property.FindPropertyRelative("fallbackValue"), "Fallback");
            }

            EditorGUI.indentLevel--;
        }

        private static float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveWatchedBoolSourceKind.EntityValueStore => GetEntityValueSourceHeight(),
                (int)ReactiveWatchedBoolSourceKind.KernelValueStore => GetKernelValueSourceHeight(),
                _ => 0f,
            };
        }

        private static void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveWatchedBoolSourceKind)sourceKind)
            {
                case ReactiveWatchedBoolSourceKind.EntityValueStore:
                    DrawPropertyField(ref position, property.FindPropertyRelative("entityValue").FindPropertyRelative("entitySourceKind"), "Entity Source");
                    DrawFilteredValueKey(ref position, property.FindPropertyRelative("entityValue").FindPropertyRelative("key"), "Key", typeof(bool));
                    break;
                case ReactiveWatchedBoolSourceKind.KernelValueStore:
                    DrawPropertyField(ref position, property.FindPropertyRelative("localValue").FindPropertyRelative("storeScope"), "Store Scope");
                    DrawFilteredValueKey(ref position, property.FindPropertyRelative("localValue").FindPropertyRelative("key"), "Key", typeof(bool));
                    break;
            }
        }

        private static float GetEntityValueSourceHeight()
        {
            return GetControlDelta(LineHeight) + GetControlDelta(ValueKeyReferenceDrawer.GetFilteredPropertyHeight());
        }

        private static float GetKernelValueSourceHeight()
        {
            return GetControlDelta(LineHeight) + GetControlDelta(ValueKeyReferenceDrawer.GetFilteredPropertyHeight());
        }

        private static float GetControlDelta(float controlHeight)
        {
            return controlHeight <= 0f ? 0f : controlHeight + Spacing;
        }

        private static float GetPropertyHeightWithChildren(SerializedProperty property)
        {
            return property == null ? 0f : EditorGUI.GetPropertyHeight(property, true);
        }

        private static void DrawPropertyField(ref Rect position, SerializedProperty property, string label)
        {
            if (property == null)
                return;

            float height = EditorGUI.GetPropertyHeight(property, true);
            Rect rowRect = new(position.x, position.y, position.width, height);
            EditorGUI.PropertyField(rowRect, property, new GUIContent(label), true);
            position.y += height + Spacing;
        }

        private static void DrawFilteredValueKey(ref Rect position, SerializedProperty property, string label, Type valueType, string pathPrefix = null)
        {
            if (property == null)
                return;

            float height = ValueKeyReferenceDrawer.GetFilteredPropertyHeight();
            Rect rowRect = new(position.x, position.y, position.width, height);
            ValueKeyReferenceDrawer.DrawFilteredDropdown(rowRect, property, new GUIContent(label), valueType, pathPrefix);
            position.y += height + Spacing;
        }
    }
}