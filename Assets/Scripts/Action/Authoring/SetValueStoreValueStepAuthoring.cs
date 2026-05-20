using System;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    public enum ValueStoreWriteStoreScope
    {
        Entity = 0,
        Kernel = 1,
        Local = 2,
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
    }

    [Serializable]
    public sealed class ValueStoreWriteAuthoring
    {
        [SerializeField] private ValueStoreWriteStoreScope storeScope;
        [SerializeField] private EntityTargetReference target = EntityTargetReference.Self();
        [SerializeField] private ValueStoreWriteValueKind valueKind;
        [SerializeField] private ValueKeyReference key;
        [SerializeField] private ReactiveBool boolValue = ReactiveBool.LiteralValue(false);
        [SerializeField] private ReactiveInt intValue = ReactiveInt.LiteralValue(0);
        [SerializeField] private ReactiveFloat floatValue = ReactiveFloat.LiteralValue(0f);
        [SerializeField] private ReactiveString stringValue = ReactiveString.LiteralValue(string.Empty);
        [SerializeField] private ReactiveEntityRef entityValue = ReactiveEntityRef.Self();
        [SerializeField] private ReactiveFaceExpressionId faceExpressionValue = ReactiveFaceExpressionId.LiteralValue(FaceExpressionId.Neutral);
        [SerializeField] private ReactiveEntityMoveState entityMoveStateValue = ReactiveEntityMoveState.LiteralValue(EntityMoveState.Idle);

        public ValueStoreWriteStoreScope StoreScope => storeScope;
        public EntityTargetReference Target => target;
        public ValueStoreWriteValueKind ValueKind => valueKind;
        public ValueKeyReference Key => key;
        public ReactiveBool BoolValue => boolValue;
        public ReactiveInt IntValue => intValue;
        public ReactiveFloat FloatValue => floatValue;
        public ReactiveString StringValue => stringValue;
        public ReactiveEntityRef EntityValue => entityValue;
        public ReactiveFaceExpressionId FaceExpressionValue => faceExpressionValue;
        public ReactiveEntityMoveState EntityMoveStateValue => entityMoveStateValue;
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

        public static bool IsKernelPath(string path)
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

            if (write.StoreScope == ValueStoreWriteStoreScope.Entity)
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

            bool isKernelKey = ValueStoreWriteValueTypeUtility.IsKernelDescriptor(descriptor);
            bool isLocalKey = ValueStoreWriteValueTypeUtility.IsLocalDescriptor(descriptor);

            if (write.StoreScope == ValueStoreWriteStoreScope.Kernel && !isKernelKey)
            {
                context.AddError("Kernel scope requires a Kernel.* ValueKey.");
                return;
            }

            if (write.StoreScope == ValueStoreWriteStoreScope.Local && !isLocalKey)
            {
                context.AddError("Local scope requires a Local.* ValueKey.");
                return;
            }

            if (write.StoreScope == ValueStoreWriteStoreScope.Entity && (isKernelKey || isLocalKey))
            {
                context.AddError("Entity scope cannot write to a Kernel.* or Local.* ValueKey.");
                return;
            }

            if (write.StoreScope == ValueStoreWriteStoreScope.Kernel && isLocalKey)
            {
                context.AddError("Kernel scope cannot write to a Local.* ValueKey.");
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
                write.Key,
                write.BoolValue,
                write.IntValue,
                write.FloatValue,
                write.StringValue,
                write.EntityValue,
                write.FaceExpressionValue,
                write.EntityMoveStateValue);
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