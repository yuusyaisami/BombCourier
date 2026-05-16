using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveVector3))]
    public sealed class ReactiveVector3Drawer : ReactiveValueDrawerBase
    {
        private static readonly ReactiveEvaluationMode[] AllowedModes = { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous };

        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return AllowedModes;
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveVector3SourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveVector3SourceKind.EntityTransformPosition => GetReactiveTransformEntityHeight(property.FindPropertyRelative("transformValue")),
                (int)ReactiveVector3SourceKind.EntityTransformForward => GetReactiveTransformEntityHeight(property.FindPropertyRelative("transformValue")),
                (int)ReactiveVector3SourceKind.AddPosition =>
                    GetReactiveTransformEntityHeight(property.FindPropertyRelative("addSource").FindPropertyRelative("baseValue")) +
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("addSource").FindPropertyRelative("addend"))),
                (int)ReactiveVector3SourceKind.AddForward =>
                    GetReactiveTransformEntityHeight(property.FindPropertyRelative("addSource").FindPropertyRelative("baseValue")) +
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("addSource").FindPropertyRelative("addend"))),
                (int)ReactiveVector3SourceKind.Direction =>
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("directionSource").FindPropertyRelative("fromEntity"), true)) +
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("directionSource").FindPropertyRelative("toEntity"), true)),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveVector3SourceKind)sourceKind)
            {
                case ReactiveVector3SourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveVector3SourceKind.EntityTransformPosition:
                    DrawReactiveTransformEntity(ref position, property.FindPropertyRelative("transformValue"), ReactiveTransformSourceKind.Position);
                    break;
                case ReactiveVector3SourceKind.EntityTransformForward:
                    DrawReactiveTransformEntity(ref position, property.FindPropertyRelative("transformValue"), ReactiveTransformSourceKind.Forward);
                    break;
                case ReactiveVector3SourceKind.AddPosition:
                    SerializedProperty addPositionProperty = property.FindPropertyRelative("addSource");
                    DrawReactiveTransformEntity(ref position, addPositionProperty.FindPropertyRelative("baseValue"), ReactiveTransformSourceKind.Position);
                    DrawPropertyField(ref position, addPositionProperty.FindPropertyRelative("addend"), "Addend");
                    break;
                case ReactiveVector3SourceKind.AddForward:
                    SerializedProperty addForwardProperty = property.FindPropertyRelative("addSource");
                    DrawReactiveTransformEntity(ref position, addForwardProperty.FindPropertyRelative("baseValue"), ReactiveTransformSourceKind.Forward);
                    DrawPropertyField(ref position, addForwardProperty.FindPropertyRelative("addend"), "Addend");
                    break;
                case ReactiveVector3SourceKind.Direction:
                    SerializedProperty directionProperty = property.FindPropertyRelative("directionSource");
                    DrawPropertyField(ref position, directionProperty.FindPropertyRelative("fromEntity"), "From");
                    DrawPropertyField(ref position, directionProperty.FindPropertyRelative("toEntity"), "To");
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