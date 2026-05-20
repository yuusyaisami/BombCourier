using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveFloat))]
    public sealed class ReactiveFloatDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveFloatSourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                (int)ReactiveFloatSourceKind.LocalValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveFloatSourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveFloatSourceKind.LocalValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveFloatSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveFloatSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveFloatSourceKind.LocalValueStore => GetReactiveLocalValueSourceHeight(),
                (int)ReactiveFloatSourceKind.Distance =>
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("distance").FindPropertyRelative("fromEntity"), true)) +
                    GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("distance").FindPropertyRelative("toEntity"), true)),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveFloatSourceKind)sourceKind)
            {
                case ReactiveFloatSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveFloatSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(float));
                    break;
                case ReactiveFloatSourceKind.LocalValueStore:
                    DrawReactiveLocalValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(float));
                    break;
                case ReactiveFloatSourceKind.Distance:
                    DrawPropertyField(ref position, property.FindPropertyRelative("distance").FindPropertyRelative("fromEntity"), "From");
                    DrawPropertyField(ref position, property.FindPropertyRelative("distance").FindPropertyRelative("toEntity"), "To");
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