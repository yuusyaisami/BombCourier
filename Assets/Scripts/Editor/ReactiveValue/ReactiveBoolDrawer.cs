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
                (int)ReactiveBoolSourceKind.KernelValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveBoolSourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveBoolSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveBoolSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveBoolSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveBoolSourceKind.KernelValueStore => GetReactiveKernelValueSourceHeight(),
                (int)ReactiveBoolSourceKind.EntityAlive => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("entityAlive").FindPropertyRelative("entity"), true)),
                (int)ReactiveBoolSourceKind.CompareNumber => GetReactiveNumberCompareSourceHeight(property.FindPropertyRelative("compareNumber")),
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
                case ReactiveBoolSourceKind.KernelValueStore:
                    DrawReactiveKernelValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(bool));
                    break;
                case ReactiveBoolSourceKind.EntityAlive:
                    DrawPropertyField(ref position, property.FindPropertyRelative("entityAlive").FindPropertyRelative("entity"), "Entity");
                    break;
                case ReactiveBoolSourceKind.CompareNumber:
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
    }
}