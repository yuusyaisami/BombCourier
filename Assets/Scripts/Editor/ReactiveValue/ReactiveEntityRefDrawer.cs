using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveEntityRef))]
    public sealed class ReactiveEntityRefDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveEntitySourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                (int)ReactiveEntitySourceKind.LocalValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveEntitySourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveEntitySourceKind.LocalValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveEntitySourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveEntitySourceKind.LocalValueStore => GetReactiveLocalValueSourceHeight(),
                (int)ReactiveEntitySourceKind.TargetReference => GetEntityTargetReferenceHeight(property.FindPropertyRelative("targetReference")),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveEntitySourceKind)sourceKind)
            {
                case ReactiveEntitySourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(EntityRef));
                    break;
                case ReactiveEntitySourceKind.LocalValueStore:
                    DrawReactiveLocalValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(EntityRef));
                    break;
                case ReactiveEntitySourceKind.TargetReference:
                    DrawEntityTargetReference(ref position, property.FindPropertyRelative("targetReference"));
                    break;
            }
        }

        protected override float GetFallbackHeight(SerializedProperty property)
        {
            return GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("fallbackKind")));
        }

        protected override void DrawFallback(ref Rect position, SerializedProperty property)
        {
            DrawPropertyField(ref position, property.FindPropertyRelative("fallbackKind"), "Fallback");
        }
    }
}