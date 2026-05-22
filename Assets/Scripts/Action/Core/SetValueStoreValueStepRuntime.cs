using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetValueStoreValueStepRuntime : IActionNodeDefinition
    {
        private readonly ValueStoreWriteStoreScope storeScope;
        private readonly EntityTargetReference target;
        private readonly ValueStoreWriteValueKind valueKind;
        private readonly ValueKeyReference key;
        private readonly ReactiveBool boolValue;
        private readonly ReactiveInt intValue;
        private readonly ReactiveFloat floatValue;
        private readonly ReactiveString stringValue;
        private readonly ReactiveEntityRef entityValue;
        private readonly ReactiveFaceExpressionId faceExpressionValue;
        private readonly ReactiveEntityMoveState entityMoveStateValue;
        private readonly ReactiveShapeExpressionId shapeExpressionValue;

        public SetValueStoreValueStepRuntime(
            ValueStoreWriteStoreScope storeScope,
            EntityTargetReference target,
            ValueStoreWriteValueKind valueKind,
            ValueKeyReference key,
            ReactiveBool boolValue,
            ReactiveInt intValue,
            ReactiveFloat floatValue,
            ReactiveString stringValue,
            ReactiveEntityRef entityValue,
            ReactiveFaceExpressionId faceExpressionValue,
            ReactiveEntityMoveState entityMoveStateValue,
            ReactiveShapeExpressionId shapeExpressionValue)
        {
            this.storeScope = storeScope;
            this.target = target;
            this.valueKind = valueKind;
            this.key = key;
            this.boolValue = boolValue;
            this.intValue = intValue;
            this.floatValue = floatValue;
            this.stringValue = stringValue;
            this.entityValue = entityValue;
            this.faceExpressionValue = faceExpressionValue;
            this.entityMoveStateValue = entityMoveStateValue;
            this.shapeExpressionValue = shapeExpressionValue;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(
                storeScope,
                target,
                valueKind,
                key,
                boolValue,
                intValue,
                floatValue,
                stringValue,
                entityValue,
                faceExpressionValue,
                entityMoveStateValue,
                shapeExpressionValue);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly ValueStoreWriteStoreScope storeScope;
            private readonly EntityTargetReference target;
            private readonly ValueStoreWriteValueKind valueKind;
            private readonly ValueKeyReference key;
            private readonly ReactiveBool boolValue;
            private readonly ReactiveInt intValue;
            private readonly ReactiveFloat floatValue;
            private readonly ReactiveString stringValue;
            private readonly ReactiveEntityRef entityValue;
            private readonly ReactiveFaceExpressionId faceExpressionValue;
            private readonly ReactiveEntityMoveState entityMoveStateValue;
            private readonly ReactiveShapeExpressionId shapeExpressionValue;

            private List<EntityRef> resolvedTargets;

            public Runtime(
                ValueStoreWriteStoreScope storeScope,
                EntityTargetReference target,
                ValueStoreWriteValueKind valueKind,
                ValueKeyReference key,
                ReactiveBool boolValue,
                ReactiveInt intValue,
                ReactiveFloat floatValue,
                ReactiveString stringValue,
                ReactiveEntityRef entityValue,
                ReactiveFaceExpressionId faceExpressionValue,
                ReactiveEntityMoveState entityMoveStateValue,
                ReactiveShapeExpressionId shapeExpressionValue)
            {
                this.storeScope = storeScope;
                this.target = target;
                this.valueKind = valueKind;
                this.key = key;
                this.boolValue = boolValue;
                this.intValue = intValue;
                this.floatValue = floatValue;
                this.stringValue = stringValue;
                this.entityValue = entityValue;
                this.faceExpressionValue = faceExpressionValue;
                this.entityMoveStateValue = entityMoveStateValue;
                this.shapeExpressionValue = shapeExpressionValue;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel == null || context.SceneKernel.ReactiveValues == null)
                    return ActionNodeStatus.Failed;

                if (!TryResolveWriteMetadata(out ValueKeyDescriptor descriptor, out ValueStoreWriteValueKind effectiveKind))
                    return ActionNodeStatus.Failed;

                return effectiveKind switch
                {
                    ValueStoreWriteValueKind.Bool => WriteBool(context, descriptor),
                    ValueStoreWriteValueKind.Int => WriteInt(context, descriptor),
                    ValueStoreWriteValueKind.Float => WriteFloat(context, descriptor),
                    ValueStoreWriteValueKind.String => WriteString(context, descriptor),
                    ValueStoreWriteValueKind.EntityRef => WriteEntityRef(context, descriptor),
                    ValueStoreWriteValueKind.FaceExpressionId => WriteFaceExpressionId(context, descriptor),
                    ValueStoreWriteValueKind.EntityMoveState => WriteEntityMoveState(context, descriptor),
                    ValueStoreWriteValueKind.ShapeExpressionId => WriteShapeExpressionId(context, descriptor),
                    _ => ActionNodeStatus.Failed,
                };
            }

            private bool TryResolveWriteMetadata(
                out ValueKeyDescriptor descriptor,
                out ValueStoreWriteValueKind effectiveKind)
            {
                descriptor = default;
                effectiveKind = ValueStoreWriteValueKind.Auto;

                if (!ValueStoreWriteValueTypeUtility.TryResolveDescriptor(key, out descriptor) ||
                    !ValueStoreWriteValueTypeUtility.IsSupportedDescriptor(descriptor) ||
                    !ValueStoreWriteValueTypeUtility.TryResolveEffectiveKind(valueKind, key, out effectiveKind))
                {
                    return false;
                }

                bool isKernelKey = ValueStoreWriteValueTypeUtility.IsKernelDescriptor(descriptor);
                bool isLocalKey = ValueStoreWriteValueTypeUtility.IsLocalDescriptor(descriptor);

                if ((storeScope == ValueStoreWriteStoreScope.Kernel && (!isKernelKey || isLocalKey)) ||
                    (storeScope == ValueStoreWriteStoreScope.Local && !isLocalKey) ||
                    (storeScope == ValueStoreWriteStoreScope.Entity && (isKernelKey || isLocalKey)))
                {
                    return false;
                }

                Type effectiveType = ValueStoreWriteValueTypeUtility.GetValueType(effectiveKind);
                return effectiveType != null && descriptor.ValueType == effectiveType;
            }

            private ActionNodeStatus WriteBool(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveBoolBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), boolValue);
                ReactiveResult<bool> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<bool>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteInt(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveIntBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), intValue);
                ReactiveResult<int> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<int>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteFloat(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveFloatBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), floatValue);
                ReactiveResult<float> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<float>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteString(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveStringBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), stringValue);
                ReactiveResult<string> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<string>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteEntityRef(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveEntityRefBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), entityValue);
                ReactiveResult<EntityRef> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<EntityRef>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteFaceExpressionId(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveFaceExpressionIdBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), faceExpressionValue);
                ReactiveResult<FaceExpressionId> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<FaceExpressionId>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteEntityMoveState(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveEntityMoveStateBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), entityMoveStateValue);
                ReactiveResult<EntityMoveState> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<EntityMoveState>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteShapeExpressionId(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveShapeExpressionIdBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), shapeExpressionValue);
                ReactiveResult<ShapeExpressionId> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<ShapeExpressionId>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus ApplyValue<T>(in ActionExecutionContext context, ValueKey<T> resolvedKey, T value)
            {
                try
                {
                    if (storeScope == ValueStoreWriteStoreScope.Local)
                    {
                        if (context.LocalValueStore == null)
                            return ActionNodeStatus.Failed;

                        context.LocalValueStore.Set(resolvedKey, value);
                        return ActionNodeStatus.Continue;
                    }

                    if (storeScope == ValueStoreWriteStoreScope.Kernel)
                    {
                        if (context.KernelValueStore == null)
                            return ActionNodeStatus.Failed;

                        context.KernelValueStore.Set(resolvedKey, value);
                        return ActionNodeStatus.Continue;
                    }

                    if (context.EntityValueStore == null)
                        return ActionNodeStatus.Failed;

                    resolvedTargets ??= new List<EntityRef>(4);
                    int count = ActionTargetResolver.Resolve(context, target, resolvedTargets);

                    if (count == 0)
                        return ActionNodeStatus.Failed;

                    for (int index = 0; index < count; index++)
                    {
                        context.EntityValueStore.Set(resolvedTargets[index], resolvedKey, value);
                    }

                    return ActionNodeStatus.Continue;
                }
                catch (InvalidOperationException exception)
                {
                    Debug.LogWarning($"{nameof(SetValueStoreValueStepRuntime)} failed to write '{resolvedKey.Path}'. {exception.Message}");
                    return ActionNodeStatus.Failed;
                }
            }
        }
    }
}