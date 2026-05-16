using System;
using System.Collections.Generic;
using BC.ActionSystem;
using UnityEngine;

namespace BC.Base
{
    public sealed class ReactiveValueResolverService
    {
        private readonly SceneKernel sceneKernel;

        public ReactiveValueResolverService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel;
        }

        public ReactiveActionScope CreateActionScope(
            ActionExecutionHandle handle,
            EntityRef actor,
            EntityRef trigger)
        {
            return new ReactiveActionScope(this, sceneKernel, handle, actor, trigger);
        }

        public ReactiveResult<float> ResolveFloat(in ReactiveEvalContext context, in ReactiveFloat value)
        {
            return value.SourceKind switch
            {
                ReactiveFloatSourceKind.Literal => value.EvaluationMode == ReactiveEvaluationMode.Watched
                    ? FailUnsupportedEvaluationMode<float>(nameof(ReactiveFloat), value.EvaluationMode, context)
                    : ReactiveResult<float>.Ok(value.Literal),
                ReactiveFloatSourceKind.EntityValueStore => value.EvaluationMode == ReactiveEvaluationMode.Continuous
                    ? FailUnsupportedEvaluationMode<float>(nameof(ReactiveFloat), value.EvaluationMode, context)
                    : ResolveEntityStoreValue<float>(context, value.EntityValue),
                ReactiveFloatSourceKind.KernelValueStore => value.EvaluationMode == ReactiveEvaluationMode.Continuous
                    ? FailUnsupportedEvaluationMode<float>(nameof(ReactiveFloat), value.EvaluationMode, context)
                    : ResolveKernelStoreValue<float>(context, value.KernelValue),
                ReactiveFloatSourceKind.Distance => value.EvaluationMode == ReactiveEvaluationMode.Watched
                    ? FailUnsupportedEvaluationMode<float>(nameof(ReactiveFloat), value.EvaluationMode, context)
                    : ResolveDistance(context, value.DistanceSource),
                _ => FailUnsupportedSource<float>(nameof(ReactiveFloat), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<int> ResolveInt(in ReactiveEvalContext context, in ReactiveInt value)
        {
            return value.SourceKind switch
            {
                ReactiveIntSourceKind.Literal => value.EvaluationMode == ReactiveEvaluationMode.Watched
                    ? FailUnsupportedEvaluationMode<int>(nameof(ReactiveInt), value.EvaluationMode, context)
                    : ReactiveResult<int>.Ok(value.Literal),
                ReactiveIntSourceKind.EntityValueStore => value.EvaluationMode == ReactiveEvaluationMode.Continuous
                    ? FailUnsupportedEvaluationMode<int>(nameof(ReactiveInt), value.EvaluationMode, context)
                    : ResolveEntityStoreValue<int>(context, value.EntityValue),
                ReactiveIntSourceKind.KernelValueStore => value.EvaluationMode == ReactiveEvaluationMode.Continuous
                    ? FailUnsupportedEvaluationMode<int>(nameof(ReactiveInt), value.EvaluationMode, context)
                    : ResolveKernelStoreValue<int>(context, value.KernelValue),
                _ => FailUnsupportedSource<int>(nameof(ReactiveInt), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<bool> ResolveBool(in ReactiveEvalContext context, in ReactiveBool value)
        {
            return value.SourceKind switch
            {
                ReactiveBoolSourceKind.Literal => value.EvaluationMode == ReactiveEvaluationMode.Watched
                    ? FailUnsupportedEvaluationMode<bool>(nameof(ReactiveBool), value.EvaluationMode, context)
                    : ReactiveResult<bool>.Ok(value.Literal),
                ReactiveBoolSourceKind.EntityValueStore => value.EvaluationMode == ReactiveEvaluationMode.Continuous
                    ? FailUnsupportedEvaluationMode<bool>(nameof(ReactiveBool), value.EvaluationMode, context)
                    : ResolveEntityStoreValue<bool>(context, value.EntityValue),
                ReactiveBoolSourceKind.KernelValueStore => value.EvaluationMode == ReactiveEvaluationMode.Continuous
                    ? FailUnsupportedEvaluationMode<bool>(nameof(ReactiveBool), value.EvaluationMode, context)
                    : ResolveKernelStoreValue<bool>(context, value.KernelValue),
                ReactiveBoolSourceKind.EntityAlive => value.EvaluationMode == ReactiveEvaluationMode.Watched
                    ? FailUnsupportedEvaluationMode<bool>(nameof(ReactiveBool), value.EvaluationMode, context)
                    : ResolveEntityAlive(context, value.EntityAliveSource),
                ReactiveBoolSourceKind.CompareFloat => value.EvaluationMode == ReactiveEvaluationMode.Watched
                    ? FailUnsupportedEvaluationMode<bool>(nameof(ReactiveBool), value.EvaluationMode, context)
                    : ResolveCompareFloat(context, value.CompareFloatSource),
                _ => FailUnsupportedSource<bool>(nameof(ReactiveBool), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<Vector3> ResolveVector3(in ReactiveEvalContext context, in ReactiveVector3 value)
        {
            if (value.EvaluationMode == ReactiveEvaluationMode.Watched)
                return FailUnsupportedEvaluationMode<Vector3>(nameof(ReactiveVector3), value.EvaluationMode, context);

            return value.SourceKind switch
            {
                ReactiveVector3SourceKind.Literal => ReactiveResult<Vector3>.Ok(value.Literal),
                ReactiveVector3SourceKind.EntityTransformPosition => ResolveTransformVector(context, value.TransformValue, ReactiveTransformSourceKind.Position),
                ReactiveVector3SourceKind.EntityTransformForward => ResolveTransformVector(context, value.TransformValue, ReactiveTransformSourceKind.Forward),
                ReactiveVector3SourceKind.AddPosition => ResolveTransformAdd(context, value.AddSource, ReactiveTransformSourceKind.Position),
                ReactiveVector3SourceKind.AddForward => ResolveTransformAdd(context, value.AddSource, ReactiveTransformSourceKind.Forward),
                ReactiveVector3SourceKind.Direction => ResolveDirection(context, value.DirectionSource),
                _ => FailUnsupportedSource<Vector3>(nameof(ReactiveVector3), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<EntityRef> ResolveEntity(in ReactiveEvalContext context, in ReactiveEntityRef value)
        {
            switch (value.SourceKind)
            {
                case ReactiveEntitySourceKind.Self:
                    if (value.EvaluationMode == ReactiveEvaluationMode.Watched)
                        return FailUnsupportedEvaluationMode<EntityRef>(nameof(ReactiveEntityRef), value.EvaluationMode, context);

                    return context.ActorEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.ActorEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.InvalidEntity, "Self entity is invalid.", context);

                case ReactiveEntitySourceKind.TriggerEntity:
                    if (value.EvaluationMode == ReactiveEvaluationMode.Watched)
                        return FailUnsupportedEvaluationMode<EntityRef>(nameof(ReactiveEntityRef), value.EvaluationMode, context);

                    return context.TriggerEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.TriggerEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.TargetNotFound, "Trigger entity is not available.", context);

                case ReactiveEntitySourceKind.EntityValueStore:
                    if (value.EvaluationMode == ReactiveEvaluationMode.Continuous)
                        return FailUnsupportedEvaluationMode<EntityRef>(nameof(ReactiveEntityRef), value.EvaluationMode, context);

                    return ResolveEntityStoreValue<EntityRef>(context, value.EntityValue);

                case ReactiveEntitySourceKind.KernelValueStore:
                    if (value.EvaluationMode == ReactiveEvaluationMode.Continuous)
                        return FailUnsupportedEvaluationMode<EntityRef>(nameof(ReactiveEntityRef), value.EvaluationMode, context);

                    return ResolveKernelStoreValue<EntityRef>(context, value.KernelValue);

                case ReactiveEntitySourceKind.TargetReference:
                    if (value.EvaluationMode == ReactiveEvaluationMode.Watched)
                        return FailUnsupportedEvaluationMode<EntityRef>(nameof(ReactiveEntityRef), value.EvaluationMode, context);

                    return ResolveTargetReferenceSingle(context, value.TargetReferenceValue);

                default:
                    return FailUnsupportedSource<EntityRef>(nameof(ReactiveEntityRef), value.SourceKind.ToString(), context);
            }
        }

        internal ReactiveResult<ValueWatchHandle<float>> ResolveFloatWatch(in ReactiveEvalContext context, in ReactiveFloat value)
        {
            return value.SourceKind switch
            {
                ReactiveFloatSourceKind.EntityValueStore => ResolveEntityStoreHandle<float>(context, value.EntityValue),
                ReactiveFloatSourceKind.KernelValueStore => ResolveKernelStoreHandle<float>(context, value.KernelValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<float>>(nameof(ReactiveFloat), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<int>> ResolveIntWatch(in ReactiveEvalContext context, in ReactiveInt value)
        {
            return value.SourceKind switch
            {
                ReactiveIntSourceKind.EntityValueStore => ResolveEntityStoreHandle<int>(context, value.EntityValue),
                ReactiveIntSourceKind.KernelValueStore => ResolveKernelStoreHandle<int>(context, value.KernelValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<int>>(nameof(ReactiveInt), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<bool>> ResolveBoolWatch(in ReactiveEvalContext context, in ReactiveBool value)
        {
            return value.SourceKind switch
            {
                ReactiveBoolSourceKind.EntityValueStore => ResolveEntityStoreHandle<bool>(context, value.EntityValue),
                ReactiveBoolSourceKind.KernelValueStore => ResolveKernelStoreHandle<bool>(context, value.KernelValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<bool>>(nameof(ReactiveBool), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<EntityRef>> ResolveEntityWatch(in ReactiveEvalContext context, in ReactiveEntityRef value)
        {
            return value.SourceKind switch
            {
                ReactiveEntitySourceKind.EntityValueStore => ResolveEntityStoreHandle<EntityRef>(context, value.EntityValue),
                ReactiveEntitySourceKind.KernelValueStore => ResolveKernelStoreHandle<EntityRef>(context, value.KernelValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<EntityRef>>(nameof(ReactiveEntityRef), ReactiveEvaluationMode.Watched, context),
            };
        }

        public void Clear()
        {
        }

        private ReactiveResult<float> ResolveDistance(
            in ReactiveEvalContext context,
            in ReactiveFloatDistanceSource source)
        {
            ReactiveResult<Vector3> fromPosition = ResolveEntityPosition(context, source.FromEntity);

            if (!fromPosition.Success)
                return ReactiveResult<float>.Fail(fromPosition.Error);

            ReactiveResult<Vector3> toPosition = ResolveEntityPosition(context, source.ToEntity);

            if (!toPosition.Success)
                return ReactiveResult<float>.Fail(toPosition.Error);

            return ReactiveResult<float>.Ok(Vector3.Distance(fromPosition.Value, toPosition.Value));
        }

        private ReactiveResult<bool> ResolveEntityAlive(
            in ReactiveEvalContext context,
            in ReactiveBoolEntityAliveSource source)
        {
            ReactiveResult<EntityRef> entityResult = ResolveEntity(context, source.Entity);

            if (!entityResult.Success)
                return ReactiveResult<bool>.Fail(entityResult.Error);

            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<bool>.Fail(kernelError);

            bool isAlive = resolvedSceneKernel.EntitiesRegistry != null &&
                           resolvedSceneKernel.EntitiesRegistry.IsAlive(entityResult.Value);

            if (!isAlive && resolvedSceneKernel.EntityComponents != null)
                isAlive = resolvedSceneKernel.EntityComponents.TryGetGameObject(entityResult.Value, out _);

            return ReactiveResult<bool>.Ok(isAlive);
        }

        private ReactiveResult<bool> ResolveCompareFloat(
            in ReactiveEvalContext context,
            in ReactiveFloatCompareSource source)
        {
            ReactiveResult<float> left = ResolveFloat(context, source.Left);

            if (!left.Success)
                return ReactiveResult<bool>.Fail(left.Error);

            ReactiveResult<float> right = ResolveFloat(context, source.Right);

            if (!right.Success)
                return ReactiveResult<bool>.Fail(right.Error);

            return ReactiveResult<bool>.Ok(EvaluateComparison(left.Value, right.Value, source.Comparison, source.Epsilon));
        }

        private ReactiveResult<Vector3> ResolveTransformVector(
            in ReactiveEvalContext context,
            in ReactiveTransformVectorSource source,
            ReactiveTransformSourceKind expectedSourceKind)
        {
            if (source.SourceKind != expectedSourceKind)
            {
                return ReactiveErrorUtility.Fail<Vector3>(
                    ReactiveErrorCode.UnsupportedSource,
                    $"ReactiveTransformVectorSource expected '{expectedSourceKind}', but found '{source.SourceKind}'.",
                    context);
            }

            ReactiveResult<Transform> transformResult = ResolveTransform(context, source.Entity);

            if (!transformResult.Success)
                return ReactiveResult<Vector3>.Fail(transformResult.Error);

            return expectedSourceKind switch
            {
                ReactiveTransformSourceKind.Position => ReactiveResult<Vector3>.Ok(transformResult.Value.position),
                ReactiveTransformSourceKind.Forward => ReactiveResult<Vector3>.Ok(transformResult.Value.forward),
                _ => ReactiveErrorUtility.Fail<Vector3>(
                    ReactiveErrorCode.UnsupportedSource,
                    $"ReactiveTransformVectorSource '{expectedSourceKind}' is not supported in the current milestone.",
                    context),
            };
        }

        private ReactiveResult<Vector3> ResolveTransformAdd(
            in ReactiveEvalContext context,
            in ReactiveVector3AddSource source,
            ReactiveTransformSourceKind expectedBaseSourceKind)
        {
            ReactiveResult<Vector3> baseValue = ResolveTransformVector(context, source.BaseValue, expectedBaseSourceKind);

            if (!baseValue.Success)
                return baseValue;

            return ReactiveResult<Vector3>.Ok(baseValue.Value + source.Addend);
        }

        private ReactiveResult<Vector3> ResolveDirection(
            in ReactiveEvalContext context,
            in ReactiveVector3DirectionSource source)
        {
            ReactiveResult<Vector3> fromPosition = ResolveEntityPosition(context, source.FromEntity);

            if (!fromPosition.Success)
                return ReactiveResult<Vector3>.Fail(fromPosition.Error);

            ReactiveResult<Vector3> toPosition = ResolveEntityPosition(context, source.ToEntity);

            if (!toPosition.Success)
                return ReactiveResult<Vector3>.Fail(toPosition.Error);

            Vector3 delta = toPosition.Value - fromPosition.Value;
            return ReactiveResult<Vector3>.Ok(delta.sqrMagnitude > 0f ? delta.normalized : Vector3.zero);
        }

        private static ReactiveResult<T> FailUnsupportedSource<T>(
            string valueType,
            string sourceKind,
            in ReactiveEvalContext context)
        {
            return ReactiveErrorUtility.Fail<T>(
                ReactiveErrorCode.UnsupportedSource,
                $"{valueType} source '{sourceKind}' is not supported in the current milestone.",
                context);
        }

        private static ReactiveResult<T> FailUnsupportedEvaluationMode<T>(
            string valueType,
            ReactiveEvaluationMode mode,
            in ReactiveEvalContext context)
        {
            return ReactiveErrorUtility.Fail<T>(
                ReactiveErrorCode.UnsupportedEvaluationMode,
                $"{valueType} does not support evaluation mode '{mode}' in the current milestone.",
                context);
        }

        private ReactiveResult<T> ResolveEntityStoreValue<T>(
            in ReactiveEvalContext context,
            in ReactiveEntityValueSource source)
        {
            ReactiveResult<EntityRef> entityResult = ResolveEntityStoreTarget(context, source.EntitySourceKind);

            if (!entityResult.Success)
                return ReactiveResult<T>.Fail(entityResult.Error);

            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<T>.Fail(kernelError);

            if (resolvedSceneKernel.EntityValueStore == null)
            {
                return ReactiveErrorUtility.Fail<T>(
                    ReactiveErrorCode.MissingValueStore,
                    "EntityValueStore is not available on the current SceneKernel.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<T>.Fail(keyError);

            try
            {
                T resolvedValue = resolvedSceneKernel.EntityValueStore.Get(entityResult.Value, key);
                return ReactiveResult<T>.Ok(resolvedValue);
            }
            catch (InvalidOperationException exception)
            {
                return ReactiveErrorUtility.Fail<T>(
                    ReactiveErrorCode.ValueStoreReadFailed,
                    exception.Message,
                    context);
            }
        }

        private ReactiveResult<T> ResolveKernelStoreValue<T>(
            in ReactiveEvalContext context,
            in ReactiveKernelValueSource source)
        {
            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<T>.Fail(kernelError);

            if (resolvedSceneKernel.KernelValueStore == null)
            {
                return ReactiveErrorUtility.Fail<T>(
                    ReactiveErrorCode.MissingValueStore,
                    "KernelValueStore is not available on the current SceneKernel.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<T>.Fail(keyError);

            try
            {
                T resolvedValue = resolvedSceneKernel.KernelValueStore.Get(key);
                return ReactiveResult<T>.Ok(resolvedValue);
            }
            catch (InvalidOperationException exception)
            {
                return ReactiveErrorUtility.Fail<T>(
                    ReactiveErrorCode.ValueStoreReadFailed,
                    exception.Message,
                    context);
            }
        }

        private ReactiveResult<ValueWatchHandle<T>> ResolveEntityStoreHandle<T>(
            in ReactiveEvalContext context,
            in ReactiveEntityValueSource source)
        {
            ReactiveResult<EntityRef> entityResult = ResolveEntityStoreTarget(context, source.EntitySourceKind);

            if (!entityResult.Success)
                return ReactiveResult<ValueWatchHandle<T>>.Fail(entityResult.Error);

            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<ValueWatchHandle<T>>.Fail(kernelError);

            if (resolvedSceneKernel.EntityValueStore == null)
            {
                return ReactiveErrorUtility.Fail<ValueWatchHandle<T>>(
                    ReactiveErrorCode.MissingValueStore,
                    "EntityValueStore is not available on the current SceneKernel.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<ValueWatchHandle<T>>.Fail(keyError);

            try
            {
                ValueWatchHandle<T> handle = resolvedSceneKernel.EntityValueStore.GetHandle(entityResult.Value, key);
                return ReactiveResult<ValueWatchHandle<T>>.Ok(handle, handle.Version);
            }
            catch (InvalidOperationException exception)
            {
                return ReactiveErrorUtility.Fail<ValueWatchHandle<T>>(
                    ReactiveErrorCode.ValueStoreReadFailed,
                    exception.Message,
                    context);
            }
        }

        private ReactiveResult<ValueWatchHandle<T>> ResolveKernelStoreHandle<T>(
            in ReactiveEvalContext context,
            in ReactiveKernelValueSource source)
        {
            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<ValueWatchHandle<T>>.Fail(kernelError);

            if (resolvedSceneKernel.KernelValueStore == null)
            {
                return ReactiveErrorUtility.Fail<ValueWatchHandle<T>>(
                    ReactiveErrorCode.MissingValueStore,
                    "KernelValueStore is not available on the current SceneKernel.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<ValueWatchHandle<T>>.Fail(keyError);

            try
            {
                ValueWatchHandle<T> handle = resolvedSceneKernel.KernelValueStore.GetHandle(key);
                return ReactiveResult<ValueWatchHandle<T>>.Ok(handle, handle.Version);
            }
            catch (InvalidOperationException exception)
            {
                return ReactiveErrorUtility.Fail<ValueWatchHandle<T>>(
                    ReactiveErrorCode.ValueStoreReadFailed,
                    exception.Message,
                    context);
            }
        }

        private bool TryResolveSceneKernel(
            in ReactiveEvalContext context,
            out SceneKernel resolvedSceneKernel,
            out ReactiveError error)
        {
            resolvedSceneKernel = context.SceneKernel ?? sceneKernel;

            if (resolvedSceneKernel != null)
            {
                error = default;
                return true;
            }

            error = ReactiveErrorUtility.Create(
                ReactiveErrorCode.MissingSceneKernel,
                "SceneKernel is required to resolve scene-backed ReactiveValues.",
                context);
            return false;
        }

        private static bool TryResolveValueKey<T>(
            ValueKeyReference keyReference,
            in ReactiveEvalContext context,
            out ValueKey<T> key,
            out ReactiveError error)
        {
            if (!keyReference.IsAssigned)
            {
                key = default;
                error = ReactiveErrorUtility.Create(
                    ReactiveErrorCode.ValueKeyNotAssigned,
                    $"A ValueKeyReference for {typeof(T).Name} is required.",
                    context);
                return false;
            }

            if (!keyReference.TryResolve(out ValueKeyDescriptor descriptor))
            {
                key = default;
                error = ReactiveErrorUtility.Create(
                    ReactiveErrorCode.ValueKeyTypeMismatch,
                    $"ValueKey could not be resolved for expected type '{typeof(T).Name}'.",
                    context);
                return false;
            }

            if (descriptor.ValueType != typeof(T))
            {
                key = default;
                error = ReactiveErrorUtility.Create(
                    ReactiveErrorCode.ValueKeyTypeMismatch,
                    $"ValueKey type mismatch. Expected '{typeof(T).Name}', actual '{descriptor.TypeName}'.",
                    context);
                return false;
            }

            key = descriptor.GetKey<T>();
            error = default;
            return true;
        }

        private static ReactiveResult<EntityRef> ResolveEntityStoreTarget(
            in ReactiveEvalContext context,
            ReactiveScopedEntitySourceKind sourceKind)
        {
            switch (sourceKind)
            {
                case ReactiveScopedEntitySourceKind.Self:
                    return context.ActorEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.ActorEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.InvalidEntity, "Self entity is invalid.", context);

                case ReactiveScopedEntitySourceKind.TriggerEntity:
                    return context.TriggerEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.TriggerEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.TargetNotFound, "Trigger entity is not available.", context);

                default:
                    return ReactiveErrorUtility.Fail<EntityRef>(
                        ReactiveErrorCode.UnsupportedSource,
                        $"EntityValueStore target source '{sourceKind}' is not supported in the current milestone.",
                        context);
            }
        }

        private ReactiveResult<EntityRef> ResolveTargetReferenceSingle(
            in ReactiveEvalContext context,
            EntityTargetReference target)
        {
            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<EntityRef>.Fail(kernelError);

            ActionExecutionContext actionContext = new(
                resolvedSceneKernel,
                resolvedSceneKernel.Actions,
                context.ActorEntity,
                context.TriggerEntity);

            List<EntityRef> results = new(2);
            int count = ActionTargetResolver.Resolve(actionContext, target, results);

            if (count == 0)
            {
                ReactiveErrorCode errorCode = target.Mode == EntityTargetResolveMode.Self
                    ? ReactiveErrorCode.InvalidEntity
                    : ReactiveErrorCode.TargetNotFound;

                string message = target.Mode switch
                {
                    EntityTargetResolveMode.Self => "Self entity is invalid.",
                    EntityTargetResolveMode.TriggerEntity => "Trigger entity is not available.",
                    EntityTargetResolveMode.TagSearch => "TargetReference could not resolve a live entity.",
                    _ => $"TargetReference mode '{target.Mode}' could not resolve any entities.",
                };

                return ReactiveErrorUtility.Fail<EntityRef>(errorCode, message, context);
            }

            if (target.Selection == EntityTargetSelection.All)
            {
                return ReactiveErrorUtility.Fail<EntityRef>(
                    ReactiveErrorCode.MultipleTargetsNotAllowed,
                    "ReactiveEntityRef TargetReference requires a single target selection.",
                    context);
            }

            if (target.Mode == EntityTargetResolveMode.TagSearch && CountTagTargets(resolvedSceneKernel, target.Tag.Id, 2) > 1)
            {
                return ReactiveErrorUtility.Fail<EntityRef>(
                    ReactiveErrorCode.MultipleTargetsNotAllowed,
                    "ReactiveEntityRef TargetReference requires a single target selection.",
                    context);
            }

            return ReactiveResult<EntityRef>.Ok(results[0]);
        }

        private static int CountTagTargets(
            SceneKernel sceneKernel,
            EntityTagId tag,
            int maxCount)
        {
            if (!tag.IsValid)
                return 0;

            int count = 0;

            if (sceneKernel?.EntitiesRegistry != null)
                count += CountTargetsInRegistry(sceneKernel.EntitiesRegistry, tag, maxCount - count);

            if (count >= maxCount)
                return count;

            ApplicationKernel applicationKernel = ApplicationKernelMB.Instance != null
                ? ApplicationKernelMB.Instance.Kernel
                : null;

            if (applicationKernel?.ApplicationEntityRegistry != null)
                count += CountTargetsInRegistry(applicationKernel.ApplicationEntityRegistry, tag, maxCount - count);

            return count;
        }

        private static int CountTargetsInRegistry(
            ScopedEntityRegistry registry,
            EntityTagId tag,
            int remainingBudget)
        {
            if (registry == null || remainingBudget <= 0)
                return 0;

            int count = 0;
            IReadOnlyList<EntityRef> entities = registry.GetEntitiesByTag(tag);

            for (int index = 0; index < entities.Count && count < remainingBudget; index++)
            {
                if (entities[index].IsValid)
                    count++;
            }

            return count;
        }

        private ReactiveResult<Vector3> ResolveEntityPosition(
            in ReactiveEvalContext context,
            in ReactiveEntityRef entity)
        {
            ReactiveResult<Transform> transform = ResolveTransform(context, entity);

            if (!transform.Success)
                return ReactiveResult<Vector3>.Fail(transform.Error);

            return ReactiveResult<Vector3>.Ok(transform.Value.position);
        }

        private ReactiveResult<Transform> ResolveTransform(
            in ReactiveEvalContext context,
            in ReactiveEntityRef entity)
        {
            ReactiveResult<EntityRef> entityResult = ResolveEntity(context, entity);

            if (!entityResult.Success)
                return ReactiveResult<Transform>.Fail(entityResult.Error);

            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<Transform>.Fail(kernelError);

            if (resolvedSceneKernel.EntityComponents == null)
            {
                return ReactiveErrorUtility.Fail<Transform>(
                    ReactiveErrorCode.MissingEntityComponentResolver,
                    "EntityComponentResolverService is not available on the current SceneKernel.",
                    context);
            }

            if (!resolvedSceneKernel.EntityComponents.TryGetTransform(entityResult.Value, out Transform transform) || transform == null)
            {
                return ReactiveErrorUtility.Fail<Transform>(
                    ReactiveErrorCode.TransformNotFound,
                    $"Transform could not be resolved for entity '{entityResult.Value}'.",
                    context);
            }

            return ReactiveResult<Transform>.Ok(transform);
        }

        private static bool EvaluateComparison(
            float left,
            float right,
            ReactiveFloatComparisonKind comparison,
            float epsilon)
        {
            float safeEpsilon = Mathf.Max(0f, epsilon);

            return comparison switch
            {
                ReactiveFloatComparisonKind.Equal => Mathf.Abs(left - right) <= safeEpsilon,
                ReactiveFloatComparisonKind.NotEqual => Mathf.Abs(left - right) > safeEpsilon,
                ReactiveFloatComparisonKind.Greater => left > right + safeEpsilon,
                ReactiveFloatComparisonKind.GreaterOrEqual => left >= right - safeEpsilon,
                ReactiveFloatComparisonKind.Less => left < right - safeEpsilon,
                ReactiveFloatComparisonKind.LessOrEqual => left <= right + safeEpsilon,
                _ => false,
            };
        }

    }
}