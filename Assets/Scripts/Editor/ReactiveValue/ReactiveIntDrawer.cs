using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveInt))]
    public sealed class ReactiveIntDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveIntSourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                (int)ReactiveIntSourceKind.LocalValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveIntSourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveIntSourceKind.LocalValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveIntSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveIntSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveIntSourceKind.LocalValueStore => GetReactiveLocalValueSourceHeight(),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveIntSourceKind)sourceKind)
            {
                case ReactiveIntSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveIntSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(int));
                    break;
                case ReactiveIntSourceKind.LocalValueStore:
                    DrawReactiveLocalValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(int));
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