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
        private readonly ValueStoreNumericOperation numericOperation;
        private readonly ValueKeyReference key;
        private readonly ReactiveSnapshotBool boolValue;
        private readonly ReactiveSnapshotInt intValue;
        private readonly ReactiveSnapshotFloat floatValue;
        private readonly ReactiveSnapshotString stringValue;
        private readonly ReactiveSnapshotEntityRef entityValue;
        private readonly ReactiveFaceExpressionId faceExpressionValue;
        private readonly ReactiveEntityMoveState entityMoveStateValue;
        private readonly ReactiveShapeExpressionId shapeExpressionValue;

        public SetValueStoreValueStepRuntime(
            ValueStoreWriteStoreScope storeScope,
            EntityTargetReference target,
            ValueStoreWriteValueKind valueKind,
            ValueStoreNumericOperation numericOperation,
            ValueKeyReference key,
            ReactiveSnapshotBool boolValue,
            ReactiveSnapshotInt intValue,
            ReactiveSnapshotFloat floatValue,
            ReactiveSnapshotString stringValue,
            ReactiveSnapshotEntityRef entityValue,
            ReactiveFaceExpressionId faceExpressionValue,
            ReactiveEntityMoveState entityMoveStateValue,
            ReactiveShapeExpressionId shapeExpressionValue)
        {
            this.storeScope = storeScope;
            this.target = target;
            this.valueKind = valueKind;
            this.numericOperation = numericOperation;
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
                numericOperation,
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
            private readonly ValueStoreNumericOperation numericOperation;
            private readonly ValueKeyReference key;
            private readonly ReactiveSnapshotBool boolValue;
            private readonly ReactiveSnapshotInt intValue;
            private readonly ReactiveSnapshotFloat floatValue;
            private readonly ReactiveSnapshotString stringValue;
            private readonly ReactiveSnapshotEntityRef entityValue;
            private readonly ReactiveFaceExpressionId faceExpressionValue;
            private readonly ReactiveEntityMoveState entityMoveStateValue;
            private readonly ReactiveShapeExpressionId shapeExpressionValue;

            private List<EntityRef> resolvedTargets;

            public Runtime(
                ValueStoreWriteStoreScope storeScope,
                EntityTargetReference target,
                ValueStoreWriteValueKind valueKind,
                ValueStoreNumericOperation numericOperation,
                ValueKeyReference key,
                ReactiveSnapshotBool boolValue,
                ReactiveSnapshotInt intValue,
                ReactiveSnapshotFloat floatValue,
                ReactiveSnapshotString stringValue,
                ReactiveSnapshotEntityRef entityValue,
                ReactiveFaceExpressionId faceExpressionValue,
                ReactiveEntityMoveState entityMoveStateValue,
                ReactiveShapeExpressionId shapeExpressionValue)
            {
                this.storeScope = storeScope;
                this.target = target;
                this.valueKind = valueKind;
                this.numericOperation = numericOperation;
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

                if (!ValueStoreWriteScopeUtility.IsKeyCompatible(storeScope, descriptor))
                {
                    return false;
                }

                Type effectiveType = ValueStoreWriteValueTypeUtility.GetValueType(effectiveKind);
                return effectiveType != null && descriptor.ValueType == effectiveType;
            }

            private ActionNodeStatus WriteBool(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveSnapshotBoolBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), boolValue);
                ReactiveResult<bool> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<bool>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteInt(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveSnapshotIntBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), intValue);
                ReactiveResult<int> result = binding.Read();
                return result.Success ? ApplyIntValue(context, descriptor.GetKey<int>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteFloat(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveSnapshotFloatBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), floatValue);
                ReactiveResult<float> result = binding.Read();
                return result.Success ? ApplyFloatValue(context, descriptor.GetKey<float>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus ApplyIntValue(in ActionExecutionContext context, ValueKey<int> resolvedKey, int operand)
            {
                try
                {
                    if (storeScope == ValueStoreWriteStoreScope.Local)
                    {
                        if (context.LocalValueStore == null)
                            return ActionNodeStatus.Failed;

                        int current = context.LocalValueStore.Get(resolvedKey);
                        if (!TryApplyNumericInt(current, operand, out int next))
                            return ActionNodeStatus.Failed;

                        context.LocalValueStore.Set(resolvedKey, next);
                        return ActionNodeStatus.Continue;
                    }

                    if (TryResolveKernelValueStore(context, out KernelValueStoreService kernelValueStore))
                    {
                        int current = kernelValueStore.Get(resolvedKey);
                        if (!TryApplyNumericInt(current, operand, out int next))
                            return ActionNodeStatus.Failed;

                        kernelValueStore.Set(resolvedKey, next);
                        return ActionNodeStatus.Continue;
                    }

                    if (context.EntityValueStore == null)
                        return ActionNodeStatus.Failed;

                    resolvedTargets ??= new List<EntityRef>(4);
                    int count = ValueStoreWriteScopeUtility.ResolveTargets(context, storeScope, target, resolvedTargets);

                    if (count == 0)
                        return ActionNodeStatus.Failed;

                    for (int index = 0; index < count; index++)
                    {
                        EntityRef targetEntity = resolvedTargets[index];
                        int current = context.EntityValueStore.Get(targetEntity, resolvedKey);

                        if (!TryApplyNumericInt(current, operand, out int next))
                            return ActionNodeStatus.Failed;

                        context.EntityValueStore.Set(targetEntity, resolvedKey, next);
                    }

                    return ActionNodeStatus.Continue;
                }
                catch (InvalidOperationException exception)
                {
                    Debug.LogWarning($"{nameof(SetValueStoreValueStepRuntime)} failed to write '{resolvedKey.Path}'. {exception.Message}");
                    return ActionNodeStatus.Failed;
                }
            }

            private ActionNodeStatus ApplyFloatValue(in ActionExecutionContext context, ValueKey<float> resolvedKey, float operand)
            {
                try
                {
                    if (storeScope == ValueStoreWriteStoreScope.Local)
                    {
                        if (context.LocalValueStore == null)
                            return ActionNodeStatus.Failed;

                        float current = context.LocalValueStore.Get(resolvedKey);
                        if (!TryApplyNumericFloat(current, operand, out float next))
                            return ActionNodeStatus.Failed;

                        context.LocalValueStore.Set(resolvedKey, next);
                        return ActionNodeStatus.Continue;
                    }

                    if (TryResolveKernelValueStore(context, out KernelValueStoreService kernelValueStore))
                    {
                        float current = kernelValueStore.Get(resolvedKey);
                        if (!TryApplyNumericFloat(current, operand, out float next))
                            return ActionNodeStatus.Failed;

                        kernelValueStore.Set(resolvedKey, next);
                        return ActionNodeStatus.Continue;
                    }

                    if (context.EntityValueStore == null)
                        return ActionNodeStatus.Failed;

                    resolvedTargets ??= new List<EntityRef>(4);
                    int count = ValueStoreWriteScopeUtility.ResolveTargets(context, storeScope, target, resolvedTargets);

                    if (count == 0)
                        return ActionNodeStatus.Failed;

                    for (int index = 0; index < count; index++)
                    {
                        EntityRef targetEntity = resolvedTargets[index];
                        float current = context.EntityValueStore.Get(targetEntity, resolvedKey);

                        if (!TryApplyNumericFloat(current, operand, out float next))
                            return ActionNodeStatus.Failed;

                        context.EntityValueStore.Set(targetEntity, resolvedKey, next);
                    }

                    return ActionNodeStatus.Continue;
                }
                catch (InvalidOperationException exception)
                {
                    Debug.LogWarning($"{nameof(SetValueStoreValueStepRuntime)} failed to write '{resolvedKey.Path}'. {exception.Message}");
                    return ActionNodeStatus.Failed;
                }
            }

            private bool TryApplyNumericInt(int current, int operand, out int result)
            {
                switch (numericOperation)
                {
                    case ValueStoreNumericOperation.Set:
                        result = operand;
                        return true;

                    case ValueStoreNumericOperation.Add:
                        result = current + operand;
                        return true;

                    case ValueStoreNumericOperation.Subtract:
                        result = current - operand;
                        return true;

                    case ValueStoreNumericOperation.Multiply:
                        result = current * operand;
                        return true;

                    case ValueStoreNumericOperation.Divide:
                        if (operand == 0)
                        {
                            Debug.LogWarning($"{nameof(SetValueStoreValueStepRuntime)}: divide by zero is not allowed for int key '{key.Path}'.");
                            result = current;
                            return false;
                        }

                        result = current / operand;
                        return true;

                    default:
                        result = operand;
                        return true;
                }
            }

            private bool TryApplyNumericFloat(float current, float operand, out float result)
            {
                switch (numericOperation)
                {
                    case ValueStoreNumericOperation.Set:
                        result = operand;
                        return true;

                    case ValueStoreNumericOperation.Add:
                        result = current + operand;
                        return true;

                    case ValueStoreNumericOperation.Subtract:
                        result = current - operand;
                        return true;

                    case ValueStoreNumericOperation.Multiply:
                        result = current * operand;
                        return true;

                    case ValueStoreNumericOperation.Divide:
                        if (Mathf.Approximately(operand, 0f))
                        {
                            Debug.LogWarning($"{nameof(SetValueStoreValueStepRuntime)}: divide by zero is not allowed for float key '{key.Path}'.");
                            result = current;
                            return false;
                        }

                        result = current / operand;
                        return true;

                    default:
                        result = operand;
                        return true;
                }
            }

            private ActionNodeStatus WriteString(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveSnapshotStringBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), stringValue);
                ReactiveResult<string> result = binding.Read();
                return result.Success ? ApplyValue(context, descriptor.GetKey<string>(), result.Value) : ActionNodeStatus.Failed;
            }

            private ActionNodeStatus WriteEntityRef(in ActionExecutionContext context, ValueKeyDescriptor descriptor)
            {
                using ReactiveSnapshotEntityRefBinding binding = new(context.SceneKernel.ReactiveValues, new ReactiveEvalContext(context), entityValue);
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

                    if (TryResolveKernelValueStore(context, out KernelValueStoreService kernelValueStore))
                    {
                        kernelValueStore.Set(resolvedKey, value);
                        return ActionNodeStatus.Continue;
                    }

                    if (context.EntityValueStore == null)
                        return ActionNodeStatus.Failed;

                    resolvedTargets ??= new List<EntityRef>(4);
                    int count = ValueStoreWriteScopeUtility.ResolveTargets(context, storeScope, target, resolvedTargets);

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

            private bool TryResolveKernelValueStore(
                in ActionExecutionContext context,
                out KernelValueStoreService kernelValueStore)
            {
                kernelValueStore = storeScope switch
                {
                    ValueStoreWriteStoreScope.SceneKernel => context.SceneKernel?.KernelValueStore,
                    ValueStoreWriteStoreScope.ApplicationKernel => context.KernelValueStore,
                    _ => null,
                };

                return kernelValueStore != null;
            }
        }
    }
}
