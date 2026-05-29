using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveSnapshotBool))]
    public sealed class ReactiveSnapshotBoolDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return new[] { ReactiveEvaluationMode.Snapshot };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveSnapshotBoolSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveSnapshotBoolSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveSnapshotBoolSourceKind.KernelValueStore => GetReactiveKernelValueSourceHeight(),
                (int)ReactiveSnapshotBoolSourceKind.EntityAlive => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("entityAlive").FindPropertyRelative("entity"), true)),
                (int)ReactiveSnapshotBoolSourceKind.CompareNumber => GetReactiveNumberCompareSourceHeight(property.FindPropertyRelative("compareNumber")),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveSnapshotBoolSourceKind)sourceKind)
            {
                case ReactiveSnapshotBoolSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveSnapshotBoolSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(bool));
                    break;
                case ReactiveSnapshotBoolSourceKind.KernelValueStore:
                    DrawReactiveKernelValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(bool));
                    break;
                case ReactiveSnapshotBoolSourceKind.EntityAlive:
                    DrawPropertyField(ref position, property.FindPropertyRelative("entityAlive").FindPropertyRelative("entity"), "Entity");
                    break;
                case ReactiveSnapshotBoolSourceKind.CompareNumber:
                    DrawReactiveNumberCompareSource(ref position, property.FindPropertyRelative("compareNumber"));
                    break;
            }
        }

        protected override float GetFallbackHeight(SerializedProperty property)
        {
            return GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("fallbackValue")));
        }

        protected override void DrawFallback(ref Rect position, SerializedProperty property)
        {
            DrawPropertyField(ref position, property.FindPropertyRelative("fallbackValue"), "Fallback");
        }

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
                height += GetFallbackHeight(property);
            }

            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty sourceKindProperty = property.FindPropertyRelative("sourceKind");
            SerializedProperty failurePolicyProperty = property.FindPropertyRelative("failurePolicy");

            if (sourceKindProperty == null)
            {
                DrawMissingField(position, label, "ReactiveSnapshotBool fields are missing.");
                return;
            }

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            EditorGUI.PropertyField(rowRect, sourceKindProperty, new GUIContent(label.text));
            rowRect.y += LineHeight + Spacing;

            if (failurePolicyProperty != null)
            {
                EditorGUI.PropertyField(new Rect(contentRect.x, rowRect.y, contentRect.width, LineHeight), failurePolicyProperty, new GUIContent("Failure"));
                rowRect.y += LineHeight + Spacing;
            }

            EditorGUI.indentLevel++;
            DrawSourcePayload(ref rowRect, property, sourceKindProperty.enumValueIndex);

            if (failurePolicyProperty != null &&
                failurePolicyProperty.enumValueIndex == (int)ReactiveFailurePolicy.UseFallback)
            {
                DrawFallback(ref rowRect, property);
            }

            EditorGUI.indentLevel--;
        }
    }
}