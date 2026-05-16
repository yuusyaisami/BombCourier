using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveBool))]
    public sealed class ReactiveBoolDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveBoolSourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveBoolSourceKind.EntityValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveBoolSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveBoolSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveBoolSourceKind.EntityAlive => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("entityAlive").FindPropertyRelative("entity"), true)),
                (int)ReactiveBoolSourceKind.CompareFloat =>
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("compareFloat").FindPropertyRelative("left"), true)) +
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("compareFloat").FindPropertyRelative("right"), true)) +
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("compareFloat").FindPropertyRelative("comparison"))) +
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("compareFloat").FindPropertyRelative("epsilon"))),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveBoolSourceKind)sourceKind)
            {
                case ReactiveBoolSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveBoolSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(bool));
                    break;
                case ReactiveBoolSourceKind.EntityAlive:
                    DrawPropertyField(ref position, property.FindPropertyRelative("entityAlive").FindPropertyRelative("entity"), "Entity");
                    break;
                case ReactiveBoolSourceKind.CompareFloat:
                    SerializedProperty compareProperty = property.FindPropertyRelative("compareFloat");
                    DrawPropertyField(ref position, compareProperty.FindPropertyRelative("left"), "Left");
                    DrawPropertyField(ref position, compareProperty.FindPropertyRelative("right"), "Right");
                    DrawPropertyField(ref position, compareProperty.FindPropertyRelative("comparison"), "Comparison");
                    DrawPropertyField(ref position, compareProperty.FindPropertyRelative("epsilon"), "Epsilon");
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
    }
}