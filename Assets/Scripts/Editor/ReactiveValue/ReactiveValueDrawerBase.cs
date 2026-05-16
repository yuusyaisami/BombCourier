using System;
using System.Collections.Generic;
using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    public abstract class ReactiveValueDrawerBase : PropertyDrawer
    {
        protected static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        protected static readonly float Spacing = EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourceKindProperty = property.FindPropertyRelative("sourceKind");
            SerializedProperty failurePolicyProperty = property.FindPropertyRelative("failurePolicy");

            if (sourceKindProperty == null || failurePolicyProperty == null)
                return LineHeight;

            int sourceKind = sourceKindProperty.enumValueIndex;
            float height = 0f;
            height += GetControlDelta(LineHeight);
            height += GetControlDelta(LineHeight);
            height += GetControlDelta(LineHeight);
            height += GetSourcePayloadHeight(property, sourceKind);

            if (failurePolicyProperty.enumValueIndex == (int)ReactiveFailurePolicy.UseFallback)
                height += GetFallbackHeight(property);

            return Mathf.Max(LineHeight, height - Spacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourceKindProperty = property.FindPropertyRelative("sourceKind");
            SerializedProperty evaluationModeProperty = property.FindPropertyRelative("evaluationMode");
            SerializedProperty failurePolicyProperty = property.FindPropertyRelative("failurePolicy");

            if (sourceKindProperty == null || evaluationModeProperty == null || failurePolicyProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "ReactiveValue fields are missing.");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            EditorGUI.PropertyField(rowRect, sourceKindProperty, new GUIContent(label.text));
            NormalizeEvaluationMode(evaluationModeProperty, sourceKindProperty.enumValueIndex);

            rowRect.y += LineHeight + Spacing;
            DrawEvaluationModePopup(rowRect, evaluationModeProperty, sourceKindProperty.enumValueIndex);

            rowRect.y += LineHeight + Spacing;
            EditorGUI.PropertyField(rowRect, failurePolicyProperty, new GUIContent("Failure"));

            rowRect.y += LineHeight + Spacing;
            EditorGUI.indentLevel++;
            DrawSourcePayload(ref rowRect, property, sourceKindProperty.enumValueIndex);

            if (failurePolicyProperty.enumValueIndex == (int)ReactiveFailurePolicy.UseFallback)
                DrawFallback(ref rowRect, property);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        protected abstract ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind);

        protected abstract ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind);

        protected abstract float GetSourcePayloadHeight(SerializedProperty property, int sourceKind);

        protected abstract void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind);

        protected abstract float GetFallbackHeight(SerializedProperty property);

        protected abstract void DrawFallback(ref Rect position, SerializedProperty property);

        protected static float GetControlDelta(float controlHeight)
        {
            return controlHeight <= 0f ? 0f : controlHeight + Spacing;
        }

        protected static float GetPropertyHeightWithChildren(SerializedProperty property, GUIContent label = null)
        {
            return property == null ? 0f : EditorGUI.GetPropertyHeight(property, label, true);
        }

        protected static float GetPropertyHeightWithChildren(SerializedProperty property, bool includeChildren)
        {
            return property == null ? 0f : EditorGUI.GetPropertyHeight(property, includeChildren);
        }

        protected static void DrawPropertyField(ref Rect position, SerializedProperty property, string label = null)
        {
            if (property == null)
                return;

            float height = EditorGUI.GetPropertyHeight(property, true);
            Rect rowRect = new(position.x, position.y, position.width, height);
            EditorGUI.PropertyField(rowRect, property, label != null ? new GUIContent(label) : GUIContent.none, true);
            position.y += height + Spacing;
        }

        protected static float GetValueKeyHeight()
        {
            return ValueKeyReferenceDrawer.GetFilteredPropertyHeight();
        }

        protected static void DrawFilteredValueKey(ref Rect position, SerializedProperty property, string label, Type valueType)
        {
            if (property == null)
                return;

            Rect rowRect = new(position.x, position.y, position.width, ValueKeyReferenceDrawer.GetFilteredPropertyHeight());
            ValueKeyReferenceDrawer.DrawFilteredDropdown(rowRect, property, new GUIContent(label), valueType);
            position.y += rowRect.height + Spacing;
        }

        protected static float GetReactiveEntityValueSourceHeight()
        {
            return GetControlDelta(LineHeight) + GetControlDelta(GetValueKeyHeight());
        }

        protected static void DrawReactiveEntityValueSource(ref Rect position, SerializedProperty property, Type valueType)
        {
            DrawPropertyField(ref position, property.FindPropertyRelative("entitySourceKind"), "Entity Source");
            DrawFilteredValueKey(ref position, property.FindPropertyRelative("key"), "Key", valueType);
        }

        protected static float GetReactiveKernelValueSourceHeight()
        {
            return GetControlDelta(GetValueKeyHeight());
        }

        protected static void DrawReactiveKernelValueSource(ref Rect position, SerializedProperty property, Type valueType)
        {
            DrawFilteredValueKey(ref position, property.FindPropertyRelative("key"), "Key", valueType);
        }

        protected static float GetEntityTargetReferenceHeight(SerializedProperty property)
        {
            if (property == null)
                return 0f;

            float height = GetControlDelta(LineHeight);
            SerializedProperty modeProperty = property.FindPropertyRelative("mode");

            if (modeProperty != null && modeProperty.enumValueIndex == (int)EntityTargetResolveMode.TagSearch)
            {
                height += GetControlDelta(LineHeight);
                height += GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("tag")));
            }

            return height;
        }

        protected static void DrawEntityTargetReference(ref Rect position, SerializedProperty property)
        {
            if (property == null)
                return;

            SerializedProperty modeProperty = property.FindPropertyRelative("mode");
            SerializedProperty selectionProperty = property.FindPropertyRelative("selection");
            SerializedProperty tagProperty = property.FindPropertyRelative("tag");

            DrawPropertyField(ref position, modeProperty, "Target Mode");

            if (modeProperty != null && modeProperty.enumValueIndex == (int)EntityTargetResolveMode.TagSearch)
            {
                DrawPropertyField(ref position, selectionProperty, "Selection");
                DrawPropertyField(ref position, tagProperty, "Tag");
            }
        }

        protected static void NormalizeTransformSourceKind(SerializedProperty property, ReactiveTransformSourceKind expectedKind)
        {
            SerializedProperty sourceKindProperty = property?.FindPropertyRelative("sourceKind");

            if (sourceKindProperty != null && sourceKindProperty.enumValueIndex != (int)expectedKind)
                sourceKindProperty.enumValueIndex = (int)expectedKind;
        }

        protected static float GetReactiveTransformEntityHeight(SerializedProperty property)
        {
            return property == null ? 0f : GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("entity"), true));
        }

        protected static void DrawReactiveTransformEntity(ref Rect position, SerializedProperty property, ReactiveTransformSourceKind expectedKind)
        {
            NormalizeTransformSourceKind(property, expectedKind);
            DrawPropertyField(ref position, property.FindPropertyRelative("entity"), "Entity");
        }

        protected static IEnumerable<string> ToDisplayNames(IReadOnlyList<ReactiveEvaluationMode> modes)
        {
            for (int index = 0; index < modes.Count; index++)
                yield return ObjectNames.NicifyVariableName(modes[index].ToString());
        }

        private void NormalizeEvaluationMode(SerializedProperty evaluationModeProperty, int sourceKind)
        {
            ReactiveEvaluationMode currentMode = (ReactiveEvaluationMode)evaluationModeProperty.enumValueIndex;
            ReactiveEvaluationMode[] allowedModes = GetAllowedEvaluationModes(sourceKind);

            for (int index = 0; index < allowedModes.Length; index++)
            {
                if (allowedModes[index] == currentMode)
                    return;
            }

            // Unsupported combinations are normalized in the inspector so serialized data stays runtime-valid.
            evaluationModeProperty.enumValueIndex = (int)GetDefaultEvaluationMode(sourceKind);
        }

        private void DrawEvaluationModePopup(Rect position, SerializedProperty evaluationModeProperty, int sourceKind)
        {
            ReactiveEvaluationMode[] allowedModes = GetAllowedEvaluationModes(sourceKind);
            ReactiveEvaluationMode currentMode = (ReactiveEvaluationMode)evaluationModeProperty.enumValueIndex;
            int selectedIndex = 0;

            for (int index = 0; index < allowedModes.Length; index++)
            {
                if (allowedModes[index] == currentMode)
                {
                    selectedIndex = index;
                    break;
                }
            }

            string[] displayNames = new List<string>(ToDisplayNames(allowedModes)).ToArray();
            int nextIndex = EditorGUI.Popup(position, "Evaluation", selectedIndex, displayNames);
            evaluationModeProperty.enumValueIndex = (int)allowedModes[Mathf.Clamp(nextIndex, 0, allowedModes.Length - 1)];
        }
    }
}