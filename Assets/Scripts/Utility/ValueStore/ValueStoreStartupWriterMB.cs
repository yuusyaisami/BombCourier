using System;
using System.Collections;
using System.Collections.Generic;
using BC.ActionSystem;
using UnityEngine;

namespace BC.Base
{
    [DisallowMultipleComponent]
    public sealed class ValueStoreStartupWriterMB : MonoBehaviour
    {
        [SerializeField] private bool applyOnStart = true;
        [SerializeField, Min(0)] private int resolveRetryFrames = 2;
        [SerializeField] private ValueStoreWriteAuthoring[] writes = Array.Empty<ValueStoreWriteAuthoring>();

        private readonly List<EntityRef> resolvedTargets = new(8);
        private SceneKernel sceneKernel;
        private EntityMB entityMB;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (!applyOnStart)
                return;

            StartCoroutine(ApplyWhenReady());
        }

        public bool ApplyNow()
        {
            if (!TryBuildContext(out WiringActionContext context))
                return false;

            return ApplyWrites(in context);
        }

        private IEnumerator ApplyWhenReady()
        {
            int attempts = 0;
            WiringActionContext context = default;

            while (!TryBuildContext(out context))
            {
                if (attempts >= resolveRetryFrames)
                {
                    Debug.LogWarning($"{nameof(ValueStoreStartupWriterMB)}: Failed to resolve startup context. Writes were skipped.", this);
                    yield break;
                }

                attempts++;
                yield return null;
            }

            ApplyWrites(in context);
        }

        private bool TryBuildContext(out WiringActionContext context)
        {
            ResolveReferences();

            context = default;

            if (sceneKernel == null || sceneKernel.ReactiveValues == null)
                return false;

            if (sceneKernel.EntityValueStore == null)
                return false;

            if (entityMB != null && !entityMB.HasEntity)
                return false;

            EntityRef selfEntity = entityMB != null && entityMB.HasEntity ? entityMB.Entity : default;
            EntityTagId selfTag = entityMB != null ? entityMB.Tag : default;

            context = new WiringActionContext(
                sceneKernel,
                gameObject,
                transform,
                selfEntity,
                selfTag,
                null,
                null,
                default,
                default);

            return true;
        }

        private bool ApplyWrites(in WiringActionContext context)
        {
            if (writes == null || writes.Length == 0)
                return true;

            bool allSucceeded = true;

            for (int index = 0; index < writes.Length; index++)
            {
                ValueStoreWriteAuthoring write = writes[index];

                if (write == null)
                    continue;

                if (!TryApplyWrite(in context, write))
                    allSucceeded = false;
            }

            return allSucceeded;
        }

        private bool TryApplyWrite(in WiringActionContext context, ValueStoreWriteAuthoring write)
        {
            if (!TryResolveWriteMetadata(write, out ValueKeyDescriptor descriptor, out ValueStoreWriteValueKind effectiveKind))
                return false;

            ReactiveEvalContext evalContext = new ReactiveEvalContext(context.SceneKernel, context.SelfEntity, context.TriggerEntity);

            return effectiveKind switch
            {
                ValueStoreWriteValueKind.Bool => EvaluateAndApplyBool(in context, evalContext, write, descriptor),
                ValueStoreWriteValueKind.Int => EvaluateAndApplyInt(in context, evalContext, write, descriptor),
                ValueStoreWriteValueKind.Float => EvaluateAndApplyFloat(in context, evalContext, write, descriptor),
                ValueStoreWriteValueKind.String => EvaluateAndApplyString(in context, evalContext, write, descriptor),
                ValueStoreWriteValueKind.EntityRef => EvaluateAndApplyEntityRef(in context, evalContext, write, descriptor),
                ValueStoreWriteValueKind.FaceExpressionId => EvaluateAndApplyFaceExpressionId(in context, evalContext, write, descriptor),
                ValueStoreWriteValueKind.EntityMoveState => EvaluateAndApplyEntityMoveState(in context, evalContext, write, descriptor),
                ValueStoreWriteValueKind.ShapeExpressionId => EvaluateAndApplyShapeExpressionId(in context, evalContext, write, descriptor),
                _ => false,
            };
        }

        private bool TryResolveWriteMetadata(
            ValueStoreWriteAuthoring write,
            out ValueKeyDescriptor descriptor,
            out ValueStoreWriteValueKind effectiveKind)
        {
            descriptor = default;
            effectiveKind = ValueStoreWriteValueKind.Auto;

            if (!ValueStoreWriteValueTypeUtility.TryResolveDescriptor(write.Key, out descriptor))
            {
                Debug.LogWarning($"{nameof(ValueStoreStartupWriterMB)}: ValueKey is not assigned.", this);
                return false;
            }

            if (!ValueStoreWriteValueTypeUtility.IsSupportedDescriptor(descriptor) ||
                !ValueStoreWriteValueTypeUtility.TryResolveEffectiveKind(write.ValueKind, write.Key, out effectiveKind))
            {
                Debug.LogWarning($"{nameof(ValueStoreStartupWriterMB)}: Unsupported ValueKey type '{descriptor.TypeName}' at '{descriptor.Path}'.", this);
                return false;
            }

            if (write.StoreScope == ValueStoreWriteStoreScope.Local)
            {
                Debug.LogWarning($"{nameof(ValueStoreStartupWriterMB)}: Local store scope is not supported in startup writer.", this);
                return false;
            }

            ValueStoreWriteStoreScope effectiveScope = ResolveEffectiveStoreScope(write.StoreScope, descriptor);

            if (!ValueStoreWriteScopeUtility.IsKeyCompatible(effectiveScope, descriptor))
            {
                Debug.LogWarning(
                    $"{nameof(ValueStoreStartupWriterMB)}: Store scope '{write.StoreScope}' does not match key '{descriptor.Path}'.",
                    this);
                return false;
            }

            Type effectiveType = ValueStoreWriteValueTypeUtility.GetValueType(effectiveKind);
            if (effectiveType == null || descriptor.ValueType != effectiveType)
            {
                Debug.LogWarning(
                    $"{nameof(ValueStoreStartupWriterMB)}: Value type mismatch. Key='{descriptor.Path}', Descriptor='{descriptor.TypeName}', Requested='{effectiveKind}'.",
                    this);
                return false;
            }

            return true;
        }

        private bool EvaluateAndApplyBool(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveSnapshotBoolBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.BoolValue);
            ReactiveResult<bool> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<bool>(), result.Value);
        }

        private bool EvaluateAndApplyInt(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveSnapshotIntBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.IntValue);
            ReactiveResult<int> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<int>(), result.Value);
        }

        private bool EvaluateAndApplyFloat(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveSnapshotFloatBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.FloatValue);
            ReactiveResult<float> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<float>(), result.Value);
        }

        private bool EvaluateAndApplyString(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveSnapshotStringBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.StringValue);
            ReactiveResult<string> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<string>(), result.Value);
        }

        private bool EvaluateAndApplyEntityRef(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveSnapshotEntityRefBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.EntityValue);
            ReactiveResult<EntityRef> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<EntityRef>(), result.Value);
        }

        private bool EvaluateAndApplyFaceExpressionId(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveFaceExpressionIdBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.FaceExpressionValue);
            ReactiveResult<FaceExpressionId> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<FaceExpressionId>(), result.Value);
        }

        private bool EvaluateAndApplyEntityMoveState(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveEntityMoveStateBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.EntityMoveStateValue);
            ReactiveResult<EntityMoveState> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<EntityMoveState>(), result.Value);
        }

        private bool EvaluateAndApplyShapeExpressionId(
            in WiringActionContext context,
            in ReactiveEvalContext evalContext,
            ValueStoreWriteAuthoring write,
            ValueKeyDescriptor descriptor)
        {
            using ReactiveShapeExpressionIdBinding binding = new(context.SceneKernel.ReactiveValues, evalContext, write.ShapeExpressionValue);
            ReactiveResult<ShapeExpressionId> result = binding.Read();
            return result.Success && ApplyValue(in context, write, descriptor.GetKey<ShapeExpressionId>(), result.Value);
        }

        private bool ApplyValue<T>(
            in WiringActionContext context,
            ValueStoreWriteAuthoring write,
            ValueKey<T> key,
            T value)
        {
            try
            {
                if (context.SceneKernel?.EntityValueStore == null)
                    return false;

                ValueStoreWriteStoreScope effectiveScope = ResolveEffectiveStoreScope(write.StoreScope, key);
                if (effectiveScope == ValueStoreWriteStoreScope.Local)
                    return false;

                int count = ValueStoreWriteScopeUtility.ResolveTargets(context, effectiveScope, write.Target, resolvedTargets);

                if (count == 0)
                {
                    Debug.LogWarning($"{nameof(ValueStoreStartupWriterMB)}: No target entity resolved for key '{key.Path}'.", this);
                    return false;
                }

                bool changedAny = false;

                for (int index = 0; index < count; index++)
                    changedAny |= context.SceneKernel.EntityValueStore.Set(resolvedTargets[index], key, value);

                return changedAny;
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogWarning($"{nameof(ValueStoreStartupWriterMB)} failed to write '{key.Path}'. {exception.Message}", this);
                return false;
            }
        }

        private static ValueStoreWriteStoreScope ResolveEffectiveStoreScope(ValueStoreWriteStoreScope authoredScope, in ValueKeyDescriptor descriptor)
        {
            if (authoredScope != ValueStoreWriteStoreScope.Entity)
                return authoredScope;

            if (string.IsNullOrWhiteSpace(descriptor.Path))
                return authoredScope;

            // Legacy scenes may author GameLogic.* as Entity scope even though it is shared scene state.
            return descriptor.Path.StartsWith("GameLogic.", StringComparison.Ordinal)
                ? ValueStoreWriteStoreScope.SceneKernel
                : authoredScope;
        }

        private static ValueStoreWriteStoreScope ResolveEffectiveStoreScope<T>(ValueStoreWriteStoreScope authoredScope, in ValueKey<T> key)
        {
            if (authoredScope != ValueStoreWriteStoreScope.Entity)
                return authoredScope;

            if (string.IsNullOrWhiteSpace(key.Path))
                return authoredScope;

            return key.Path.StartsWith("GameLogic.", StringComparison.Ordinal)
                ? ValueStoreWriteStoreScope.SceneKernel
                : authoredScope;
        }

        private void ResolveReferences()
        {
            if (sceneKernel == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();
                if (kernelMB != null)
                    sceneKernel = kernelMB.Kernel;
            }

            if (entityMB == null)
                entityMB = GetComponentInParent<EntityMB>();
        }
    }
}
