using System;
using System.Collections.Generic;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.ActionSystem
{
    public enum ValueStoreWriteStoreScope
    {
        Entity = 0,
        SceneKernel = 1,
        Local = 2,
        ApplicationKernel = 3,
    }

    public enum ValueStoreWriteValueKind
    {
        Auto = 0,
        Bool = 1,
        Int = 2,
        Float = 3,
        String = 4,
        EntityRef = 5,
        FaceExpressionId = 6,
        EntityMoveState = 7,
        ShapeExpressionId = 8,
    }

    public enum ValueStoreNumericOperation
    {
        Set = 0,
        Add = 1,
        Subtract = 2,
        Multiply = 3,
        Divide = 4,
    }

    [Serializable]
    public sealed class ValueStoreWriteAuthoring
    {
        [SerializeField] private ValueStoreWriteStoreScope storeScope;
        [ShowIf(nameof(UsesEntityTarget))]
        [LabelText("$TargetLabel")]
        [SerializeField] private EntityTargetReference target = EntityTargetReference.Self();
        [SerializeField] private ValueStoreWriteValueKind valueKind;
        [SerializeField] private ValueStoreNumericOperation numericOperation;
        [SerializeField] private ValueKeyReference key;
        [SerializeField] private ReactiveSnapshotBool boolValue = ReactiveSnapshotBool.LiteralValue(false);
        [SerializeField] private ReactiveSnapshotInt intValue = ReactiveSnapshotInt.LiteralValue(0);
        [SerializeField] private ReactiveSnapshotFloat floatValue = ReactiveSnapshotFloat.LiteralValue(0f);
        [SerializeField] private ReactiveSnapshotString stringValue = ReactiveSnapshotString.LiteralValue(string.Empty);
        [SerializeField] private ReactiveSnapshotEntityRef entityValue = ReactiveSnapshotEntityRef.Self();
        [SerializeField] private ReactiveFaceExpressionId faceExpressionValue = ReactiveFaceExpressionId.LiteralValue(FaceExpressionId.Neutral);
        [SerializeField] private ReactiveEntityMoveState entityMoveStateValue = ReactiveEntityMoveState.LiteralValue(EntityMoveState.Idle);
        [SerializeField] private ReactiveShapeExpressionId shapeExpressionValue = ReactiveShapeExpressionId.LiteralValue(ShapeExpressionId.Neutral);

        private bool UsesEntityTarget => ValueStoreWriteScopeUtility.UsesEntityTarget(storeScope);
        private string TargetLabel => $"EntityTarget [{target.ToSummaryString()}]";

        public ValueStoreWriteStoreScope StoreScope => storeScope;
        public EntityTargetReference Target => target;
        public ValueStoreWriteValueKind ValueKind => valueKind;
        public ValueStoreNumericOperation NumericOperation => numericOperation;
        public ValueKeyReference Key => key;
        public ReactiveSnapshotBool BoolValue => boolValue;
        public ReactiveSnapshotInt IntValue => intValue;
        public ReactiveSnapshotFloat FloatValue => floatValue;
        public ReactiveSnapshotString StringValue => stringValue;
        public ReactiveSnapshotEntityRef EntityValue => entityValue;
        public ReactiveFaceExpressionId FaceExpressionValue => faceExpressionValue;
        public ReactiveEntityMoveState EntityMoveStateValue => entityMoveStateValue;
        public ReactiveShapeExpressionId ShapeExpressionValue => shapeExpressionValue;
    }

    public static class ValueStoreWriteScopeUtility
    {
        public static bool UsesEntityTarget(ValueStoreWriteStoreScope scope)
        {
            return scope == ValueStoreWriteStoreScope.Entity;
        }

        public static bool IsKernelEntityScope(ValueStoreWriteStoreScope scope)
        {
            return scope == ValueStoreWriteStoreScope.SceneKernel ||
                   scope == ValueStoreWriteStoreScope.ApplicationKernel;
        }

        public static bool IsKeyCompatible(ValueStoreWriteStoreScope scope, ValueKeyDescriptor descriptor)
        {
            bool isKernel = ValueStoreWriteValueTypeUtility.IsKernelDescriptor(descriptor);
            bool isLocal = ValueStoreWriteValueTypeUtility.IsLocalDescriptor(descriptor);

            if (scope == ValueStoreWriteStoreScope.Local)
                return isLocal;

            if (IsKernelEntityScope(scope))
                return isKernel && !isLocal;

            // Entity スコープ: Local 専用キーと、アプリ永続の Kernel.* 以外は Entity 単位で保持できる。
            // GameLogic.* を共有(SceneKernel)だけでなく、NPC ごとの会話回数などエンティティ単位にも書き込めるようにする。
            bool isApplicationKernel = ValueStoreWriteValueTypeUtility.IsApplicationKernelDescriptor(descriptor);
            return !isLocal && !isApplicationKernel;
        }

        public static int ResolveTargets(
            in WiringActionContext context,
            ValueStoreWriteStoreScope scope,
            EntityTargetReference target,
            List<EntityRef> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            EntityResolveScope resolveScope = scope switch
            {
                ValueStoreWriteStoreScope.Entity => EntityResolveScope.Entity,
                ValueStoreWriteStoreScope.SceneKernel => EntityResolveScope.SceneKernel,
                ValueStoreWriteStoreScope.ApplicationKernel => EntityResolveScope.ApplicationKernel,
                _ => EntityResolveScope.Entity,
            };

            return scope == ValueStoreWriteStoreScope.Local
                ? 0
                : ScopedEntityResolveUtility.ResolveTargets(context, resolveScope, target, results);
        }

        public static int ResolveTargets(
            in ActionExecutionContext context,
            ValueStoreWriteStoreScope scope,
            EntityTargetReference target,
            List<EntityRef> results)
        {
            WiringActionContext wiringContext = new(
                context.SceneKernel,
                null,
                null,
                context.SelfEntity,
                default,
                null,
                null,
                context.TriggerEntity,
                default);

            return ResolveTargets(wiringContext, scope, target, results);
        }

    }

    public static class ValueStoreWriteValueTypeUtility
    {
        // Auto follows the selected ValueKey type so authoring does not duplicate the same information.
        public static bool TryResolveEffectiveKind(
            ValueStoreWriteValueKind requestedKind,
            ValueKeyReference keyReference,
            out ValueStoreWriteValueKind effectiveKind)
        {
            if (requestedKind != ValueStoreWriteValueKind.Auto)
            {
                effectiveKind = requestedKind;
                return GetValueType(requestedKind) != null;
            }

            if (!TryResolveDescriptor(keyReference, out ValueKeyDescriptor descriptor) ||
                !TryGetKind(descriptor.ValueType, out effectiveKind))
            {
                effectiveKind = ValueStoreWriteValueKind.Auto;
                return false;
            }

            return true;
        }

        public static bool TryResolveDescriptor(ValueKeyReference keyReference, out ValueKeyDescriptor descriptor)
        {
            if (keyReference.IsAssigned && keyReference.TryResolve(out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        public static bool TryGetKind(Type valueType, out ValueStoreWriteValueKind kind)
        {
            if (valueType == typeof(bool))
            {
                kind = ValueStoreWriteValueKind.Bool;
                return true;
            }

            if (valueType == typeof(int))
            {
                kind = ValueStoreWriteValueKind.Int;
                return true;
            }

            if (valueType == typeof(float))
            {
                kind = ValueStoreWriteValueKind.Float;
                return true;
            }

            if (valueType == typeof(string))
            {
                kind = ValueStoreWriteValueKind.String;
                return true;
            }

            if (valueType == typeof(EntityRef))
            {
                kind = ValueStoreWriteValueKind.EntityRef;
                return true;
            }

            if (valueType == typeof(FaceExpressionId))
            {
                kind = ValueStoreWriteValueKind.FaceExpressionId;
                return true;
            }

            if (valueType == typeof(EntityMoveState))
            {
                kind = ValueStoreWriteValueKind.EntityMoveState;
                return true;
            }

            if (valueType == typeof(ShapeExpressionId))
            {
                kind = ValueStoreWriteValueKind.ShapeExpressionId;
                return true;
            }

            kind = ValueStoreWriteValueKind.Auto;
            return false;
        }

        public static Type GetValueType(ValueStoreWriteValueKind kind)
        {
            return kind switch
            {
                ValueStoreWriteValueKind.Bool => typeof(bool),
                ValueStoreWriteValueKind.Int => typeof(int),
                ValueStoreWriteValueKind.Float => typeof(float),
                ValueStoreWriteValueKind.String => typeof(string),
                ValueStoreWriteValueKind.EntityRef => typeof(EntityRef),
                ValueStoreWriteValueKind.FaceExpressionId => typeof(FaceExpressionId),
                ValueStoreWriteValueKind.EntityMoveState => typeof(EntityMoveState),
                ValueStoreWriteValueKind.ShapeExpressionId => typeof(ShapeExpressionId),
                _ => null,
            };
        }

        public static bool IsSupportedDescriptor(ValueKeyDescriptor descriptor)
        {
            return TryGetKind(descriptor.ValueType, out _);
        }

        public static bool IsKernelDescriptor(ValueKeyDescriptor descriptor)
        {
            return IsKernelPath(descriptor.Path);
        }

        public static bool IsLocalDescriptor(ValueKeyDescriptor descriptor)
        {
            return IsLocalPath(descriptor.Path);
        }

        // アプリ永続のカーネルキー (Kernel.*) かどうか。GameLogic.* は含めない。
        public static bool IsApplicationKernelDescriptor(ValueKeyDescriptor descriptor)
        {
            return IsApplicationKernelPath(descriptor.Path);
        }

        public static bool IsKernelPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return path.StartsWith("Kernel.", StringComparison.Ordinal) ||
                   path.StartsWith("GameLogic.", StringComparison.Ordinal);
        }

        public static bool IsApplicationKernelPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && path.StartsWith("Kernel.", StringComparison.Ordinal);
        }

        public static bool IsLocalPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && path.StartsWith("Local.", StringComparison.Ordinal);
        }
    }

    public static class ValueStoreWriteAuthoringUtility
    {
        public static void Validate(ValueStoreWriteAuthoring write, ActionValidationContext context)
        {
            if (write == null)
            {
                context.AddError("ValueStore write payload is missing.");
                return;
            }

            if (ValueStoreWriteScopeUtility.UsesEntityTarget(write.StoreScope))
                context.ValidateEntityTarget(write.Target);

            if (!write.Key.IsAssigned)
            {
                context.AddError("ValueStore key is not assigned.");
                return;
            }

            if (!write.Key.TryResolve(out ValueKeyDescriptor descriptor))
            {
                context.AddError("ValueStore key could not be resolved.");
                return;
            }

            if (!ValueStoreWriteValueTypeUtility.IsSupportedDescriptor(descriptor))
            {
                context.AddError($"ValueStore key type '{descriptor.TypeName}' is not supported by this step.");
                return;
            }

            if (!ValueStoreWriteScopeUtility.IsKeyCompatible(write.StoreScope, descriptor))
            {
                context.AddError($"Store scope '{write.StoreScope}' does not match key '{descriptor.Path}'.");
                return;
            }

            if (!ValueStoreWriteValueTypeUtility.TryResolveEffectiveKind(write.ValueKind, write.Key, out ValueStoreWriteValueKind effectiveKind))
            {
                context.AddError("Value type could not be resolved from the selected ValueKey.");
                return;
            }

            Type effectiveType = ValueStoreWriteValueTypeUtility.GetValueType(effectiveKind);

            if (effectiveType == null || descriptor.ValueType != effectiveType)
            {
                context.AddError($"Value type mismatch. Key expects '{descriptor.TypeName}', but step is configured for '{effectiveKind}'.");
            }
        }

        public static SetValueStoreValueStepRuntime CreateRuntime(ValueStoreWriteAuthoring write)
        {
            return new SetValueStoreValueStepRuntime(
                write.StoreScope,
                write.Target,
                write.ValueKind,
                write.NumericOperation,
                write.Key,
                write.BoolValue,
                write.IntValue,
                write.FloatValue,
                write.StringValue,
                write.EntityValue,
                write.FaceExpressionValue,
                write.EntityMoveStateValue,
                write.ShapeExpressionValue);
        }
    }

    [Serializable]
    public sealed class SetValueStoreValueStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private ValueStoreWriteAuthoring write = new();

        public override void Validate(ActionValidationContext context)
        {
            ValueStoreWriteAuthoringUtility.Validate(write, context);
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(ValueStoreWriteAuthoringUtility.CreateRuntime(write));
        }
    }
}