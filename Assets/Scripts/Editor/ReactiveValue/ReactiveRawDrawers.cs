using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ReactiveString))]
    public sealed class ReactiveStringDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveStringSourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                (int)ReactiveStringSourceKind.KernelValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveStringSourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveStringSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveStringSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveStringSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveStringSourceKind.KernelValueStore => GetReactiveKernelValueSourceHeight(),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveStringSourceKind)sourceKind)
            {
                case ReactiveStringSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveStringSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(string));
                    break;
                case ReactiveStringSourceKind.KernelValueStore:
                    DrawReactiveKernelValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(string));
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

    [CustomPropertyDrawer(typeof(ReactiveFaceExpressionId))]
    public sealed class ReactiveFaceExpressionIdDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveFaceExpressionIdSourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                (int)ReactiveFaceExpressionIdSourceKind.KernelValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveFaceExpressionIdSourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveFaceExpressionIdSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveFaceExpressionIdSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveFaceExpressionIdSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveFaceExpressionIdSourceKind.KernelValueStore => GetReactiveKernelValueSourceHeight(),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveFaceExpressionIdSourceKind)sourceKind)
            {
                case ReactiveFaceExpressionIdSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveFaceExpressionIdSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(FaceExpressionId));
                    break;
                case ReactiveFaceExpressionIdSourceKind.KernelValueStore:
                    DrawReactiveKernelValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(FaceExpressionId));
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

    [CustomPropertyDrawer(typeof(ReactiveEntityMoveState))]
    public sealed class ReactiveEntityMoveStateDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveEntityMoveStateSourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                (int)ReactiveEntityMoveStateSourceKind.KernelValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveEntityMoveStateSourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveEntityMoveStateSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveEntityMoveStateSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveEntityMoveStateSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveEntityMoveStateSourceKind.KernelValueStore => GetReactiveKernelValueSourceHeight(),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveEntityMoveStateSourceKind)sourceKind)
            {
                case ReactiveEntityMoveStateSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveEntityMoveStateSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(EntityMoveState));
                    break;
                case ReactiveEntityMoveStateSourceKind.KernelValueStore:
                    DrawReactiveKernelValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(EntityMoveState));
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

    [CustomPropertyDrawer(typeof(ReactiveShapeExpressionId))]
    public sealed class ReactiveShapeExpressionIdDrawer : ReactiveValueDrawerBase
    {
        protected override ReactiveEvaluationMode[] GetAllowedEvaluationModes(int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveShapeExpressionIdSourceKind.EntityValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                (int)ReactiveShapeExpressionIdSourceKind.KernelValueStore => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Watched },
                _ => new[] { ReactiveEvaluationMode.Snapshot, ReactiveEvaluationMode.Continuous },
            };
        }

        protected override ReactiveEvaluationMode GetDefaultEvaluationMode(int sourceKind)
        {
            return sourceKind == (int)ReactiveShapeExpressionIdSourceKind.EntityValueStore ||
                   sourceKind == (int)ReactiveShapeExpressionIdSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        protected override float GetSourcePayloadHeight(SerializedProperty property, int sourceKind)
        {
            return sourceKind switch
            {
                (int)ReactiveShapeExpressionIdSourceKind.Literal => GetControlDelta(GetPropertyHeightWithChildren(property.FindPropertyRelative("literal"))),
                (int)ReactiveShapeExpressionIdSourceKind.EntityValueStore => GetReactiveEntityValueSourceHeight(),
                (int)ReactiveShapeExpressionIdSourceKind.KernelValueStore => GetReactiveKernelValueSourceHeight(),
                _ => 0f,
            };
        }

        protected override void DrawSourcePayload(ref Rect position, SerializedProperty property, int sourceKind)
        {
            switch ((ReactiveShapeExpressionIdSourceKind)sourceKind)
            {
                case ReactiveShapeExpressionIdSourceKind.Literal:
                    DrawPropertyField(ref position, property.FindPropertyRelative("literal"), "Value");
                    break;
                case ReactiveShapeExpressionIdSourceKind.EntityValueStore:
                    DrawReactiveEntityValueSource(ref position, property.FindPropertyRelative("entityValue"), typeof(ShapeExpressionId));
                    break;
                case ReactiveShapeExpressionIdSourceKind.KernelValueStore:
                    DrawReactiveKernelValueSource(ref position, property.FindPropertyRelative("localValue"), typeof(ShapeExpressionId));
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