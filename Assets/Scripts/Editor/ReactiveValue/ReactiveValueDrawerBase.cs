using System;
using System.Collections.Generic;
using BC.Base;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    public abstract class ReactiveValueDrawerBase : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourceKindProperty = property.FindPropertyRelative("sourceKind");

            if (sourceKindProperty == null)
                return LineHeight;

            int sourceKind = sourceKindProperty.enumValueIndex;
            float height = 0f;
            height += GetControlDelta(LineHeight);
            height += GetControlDelta(LineHeight);
            height += GetControlDelta(LineHeight);
            height += GetSourcePayloadHeight(property, sourceKind);

            SerializedProperty failurePolicyProperty = property.FindPropertyRelative("failurePolicy");

            if (failurePolicyProperty != null &&
                failurePolicyProperty.enumValueIndex == (int)ReactiveFailurePolicy.UseFallback)
            {
                height += GetFallbackHeight(property);
            }

            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourceKindProperty = property.FindPropertyRelative("sourceKind");
            SerializedProperty evaluationModeProperty = property.FindPropertyRelative("evaluationMode");
            SerializedProperty failurePolicyProperty = property.FindPropertyRelative("failurePolicy");

            if (sourceKindProperty == null)
            {
                DrawMissingField(position, label, "ReactiveValue fields are missing.");
                return;
            }

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            EditorGUI.PropertyField(rowRect, sourceKindProperty, new GUIContent(label.text));

            if (evaluationModeProperty != null)
            {
                NormalizeEvaluationMode(evaluationModeProperty, sourceKindProperty.enumValueIndex);
                rowRect.y += LineHeight + Spacing;
                DrawEvaluationModePopup(rowRect, evaluationModeProperty, sourceKindProperty.enumValueIndex);
            }

            if (failurePolicyProperty != null)
            {
                rowRect.y += LineHeight + Spacing;
                EditorGUI.PropertyField(rowRect, failurePolicyProperty, new GUIContent("Failure"));
            }

            rowRect.y += LineHeight + Spacing;
            EditorGUI.indentLevel++;
            DrawSourcePayload(ref rowRect, property, sourceKindProperty.enumValueIndex);

            if (failurePolicyProperty != null &&
                failurePolicyProperty.enumValueIndex == (int)ReactiveFailurePolicy.UseFallback)
            {
                DrawFallback(ref rowRect, property);
            }

            EditorGUI.indentLevel--;
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

        protected static void DrawFilteredValueKey(ref Rect position, SerializedProperty property, string label, Type valueType, string pathPrefix = null)
        {
            if (property == null)
                return;

            Rect rowRect = new(position.x, position.y, position.width, ValueKeyReferenceDrawer.GetFilteredPropertyHeight());
            ValueKeyReferenceDrawer.DrawFilteredDropdown(rowRect, property, new GUIContent(label), valueType, pathPrefix);
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
            return GetControlDelta(LineHeight) + GetControlDelta(GetValueKeyHeight());
        }

        protected static void DrawReactiveKernelValueSource(ref Rect position, SerializedProperty property, Type valueType)
        {
            DrawPropertyField(ref position, property.FindPropertyRelative("storeScope"), "Store");
            DrawFilteredValueKey(ref position, property.FindPropertyRelative("key"), "Key", valueType);
        }

        protected static float GetReactiveNumberCompareSourceHeight(SerializedProperty property)
        {
            if (property == null)
                return 0f;

            return GetReactiveNumberCompareOperandHeight(property, "leftValueKind", "leftFloat", "leftInt") +
                   GetReactiveNumberCompareOperandHeight(property, "rightValueKind", "rightFloat", "rightInt") +
                   GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("comparison"))) +
                   GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("epsilon")));
        }

        protected static void DrawReactiveNumberCompareSource(ref Rect position, SerializedProperty property)
        {
            if (property == null)
                return;

            DrawReactiveNumberCompareOperand(ref position, property, "leftValueKind", "leftFloat", "leftInt", "Left");
            DrawReactiveNumberCompareOperand(ref position, property, "rightValueKind", "rightFloat", "rightInt", "Right");
            DrawPropertyField(ref position, property.FindPropertyRelative("comparison"), "Comparison");
            DrawPropertyField(ref position, property.FindPropertyRelative("epsilon"), "Epsilon");
        }

        private static float GetReactiveNumberCompareOperandHeight(
            SerializedProperty property,
            string kindPropertyName,
            string floatPropertyName,
            string intPropertyName)
        {
            return GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative(kindPropertyName))) +
                   GetControlDelta(GetPropertyHeightWithChildren(GetReactiveNumberCompareOperandValueProperty(property, kindPropertyName, floatPropertyName, intPropertyName), true));
        }

        private static void DrawReactiveNumberCompareOperand(
            ref Rect position,
            SerializedProperty property,
            string kindPropertyName,
            string floatPropertyName,
            string intPropertyName,
            string label)
        {
            DrawPropertyField(ref position, property.FindPropertyRelative(kindPropertyName), $"{label} Type");
            DrawPropertyField(ref position, GetReactiveNumberCompareOperandValueProperty(property, kindPropertyName, floatPropertyName, intPropertyName), label);
        }

        private static SerializedProperty GetReactiveNumberCompareOperandValueProperty(
            SerializedProperty property,
            string kindPropertyName,
            string floatPropertyName,
            string intPropertyName)
        {
            SerializedProperty kindProperty = property.FindPropertyRelative(kindPropertyName);
            return kindProperty != null && kindProperty.enumValueIndex == (int)ReactiveNumberValueKind.Int
                ? property.FindPropertyRelative(intPropertyName)
                : property.FindPropertyRelative(floatPropertyName);
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
