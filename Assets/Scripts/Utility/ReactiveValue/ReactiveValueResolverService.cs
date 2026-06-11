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
                ReactiveFloatSourceKind.Literal => ReactiveResult<float>.Ok(value.Literal),
                ReactiveFloatSourceKind.EntityValueStore => ResolveEntityStoreValue<float>(context, value.EntityValue),
                ReactiveFloatSourceKind.KernelValueStore => ResolveKernelStoreValue<float>(context, value.LocalValue),
                ReactiveFloatSourceKind.Distance => ResolveDistance(context, value.DistanceSource),
                _ => FailUnsupportedSource<float>(nameof(ReactiveFloat), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<float> ResolveSnapshotFloat(in ReactiveEvalContext context, in ReactiveSnapshotFloat value)
        {
            return value.SourceKind switch
            {
                ReactiveSnapshotFloatSourceKind.Literal => ReactiveResult<float>.Ok(value.Literal),
                ReactiveSnapshotFloatSourceKind.EntityValueStore => ResolveEntityStoreValue<float>(context, value.EntityValue),
                ReactiveSnapshotFloatSourceKind.KernelValueStore => ResolveKernelStoreValue<float>(context, value.LocalValue),
                ReactiveSnapshotFloatSourceKind.Distance => ResolveDistance(context, value.DistanceSource),
                _ => FailUnsupportedSource<float>(nameof(ReactiveSnapshotFloat), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<float> ResolveWatchedFloat(in ReactiveEvalContext context, in ReactiveWatchedFloat value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedFloatSourceKind.EntityValueStore => ResolveEntityStoreValue<float>(context, value.EntityValue),
                ReactiveWatchedFloatSourceKind.KernelValueStore => ResolveKernelStoreValue<float>(context, value.LocalValue),
                _ => FailUnsupportedSource<float>(nameof(ReactiveWatchedFloat), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<int> ResolveInt(in ReactiveEvalContext context, in ReactiveInt value)
        {
            return value.SourceKind switch
            {
                ReactiveIntSourceKind.Literal => ReactiveResult<int>.Ok(value.Literal),
                ReactiveIntSourceKind.EntityValueStore => ResolveEntityStoreValue<int>(context, value.EntityValue),
                ReactiveIntSourceKind.KernelValueStore => ResolveKernelStoreValue<int>(context, value.LocalValue),
                _ => FailUnsupportedSource<int>(nameof(ReactiveInt), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<int> ResolveSnapshotInt(in ReactiveEvalContext context, in ReactiveSnapshotInt value)
        {
            return value.SourceKind switch
            {
                ReactiveSnapshotIntSourceKind.Literal => ReactiveResult<int>.Ok(value.Literal),
                ReactiveSnapshotIntSourceKind.EntityValueStore => ResolveEntityStoreValue<int>(context, value.EntityValue),
                ReactiveSnapshotIntSourceKind.KernelValueStore => ResolveKernelStoreValue<int>(context, value.LocalValue),
                _ => FailUnsupportedSource<int>(nameof(ReactiveSnapshotInt), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<int> ResolveWatchedInt(in ReactiveEvalContext context, in ReactiveWatchedInt value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedIntSourceKind.EntityValueStore => ResolveEntityStoreValue<int>(context, value.EntityValue),
                ReactiveWatchedIntSourceKind.KernelValueStore => ResolveKernelStoreValue<int>(context, value.LocalValue),
                _ => FailUnsupportedSource<int>(nameof(ReactiveWatchedInt), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<bool> ResolveBool(in ReactiveEvalContext context, in ReactiveBool value)
        {
            return value.SourceKind switch
            {
                ReactiveBoolSourceKind.Literal => ReactiveResult<bool>.Ok(value.Literal),
                ReactiveBoolSourceKind.EntityValueStore => ResolveEntityStoreValue<bool>(context, value.EntityValue),
                ReactiveBoolSourceKind.KernelValueStore => ResolveKernelStoreValue<bool>(context, value.LocalValue),
                ReactiveBoolSourceKind.EntityAlive => ResolveEntityAlive(context, value.EntityAliveSource),
                ReactiveBoolSourceKind.CompareNumber => ResolveCompareNumber(context, value.CompareNumberSource),
                _ => FailUnsupportedSource<bool>(nameof(ReactiveBool), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<bool> ResolveSnapshotBool(in ReactiveEvalContext context, in ReactiveSnapshotBool value)
        {
            return value.SourceKind switch
            {
                ReactiveSnapshotBoolSourceKind.Literal => ReactiveResult<bool>.Ok(value.Literal),
                ReactiveSnapshotBoolSourceKind.EntityValueStore => ResolveEntityStoreValue<bool>(context, value.EntityValue),
                ReactiveSnapshotBoolSourceKind.KernelValueStore => ResolveKernelStoreValue<bool>(context, value.LocalValue),
                ReactiveSnapshotBoolSourceKind.EntityAlive => ResolveEntityAlive(context, value.EntityAliveSource),
                ReactiveSnapshotBoolSourceKind.CompareNumber => ResolveCompareNumber(context, value.CompareNumberSource),
                _ => FailUnsupportedSource<bool>(nameof(ReactiveSnapshotBool), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<bool> ResolveWatchedBool(in ReactiveEvalContext context, in ReactiveWatchedBool value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedBoolSourceKind.EntityValueStore => ResolveEntityStoreValue<bool>(context, value.EntityValue),
                ReactiveWatchedBoolSourceKind.KernelValueStore => ResolveKernelStoreValue<bool>(context, value.LocalValue),
                _ => FailUnsupportedSource<bool>(nameof(ReactiveWatchedBool), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<Vector3> ResolveVector3(in ReactiveEvalContext context, in ReactiveVector3 value)
        {
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
                    return context.ActorEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.ActorEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.InvalidEntity, "Self entity is invalid.", context);

                case ReactiveEntitySourceKind.TriggerEntity:
                    return context.TriggerEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.TriggerEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.TargetNotFound, "Trigger entity is not available.", context);

                case ReactiveEntitySourceKind.EntityValueStore:
                    return ResolveEntityStoreValue<EntityRef>(context, value.EntityValue);

                case ReactiveEntitySourceKind.KernelValueStore:
                    return ResolveKernelStoreValue<EntityRef>(context, value.LocalValue);

                case ReactiveEntitySourceKind.TargetReference:
                    return ResolveTargetReferenceSingle(context, value.TargetReferenceValue);

                default:
                    return FailUnsupportedSource<EntityRef>(nameof(ReactiveEntityRef), value.SourceKind.ToString(), context);
            }
        }

        public ReactiveResult<EntityRef> ResolveSnapshotEntityRef(in ReactiveEvalContext context, in ReactiveSnapshotEntityRef value)
        {
            switch (value.SourceKind)
            {
                case ReactiveSnapshotEntityRefSourceKind.Self:
                    return context.ActorEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.ActorEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.InvalidEntity, "Self entity is invalid.", context);

                case ReactiveSnapshotEntityRefSourceKind.TriggerEntity:
                    return context.TriggerEntity.IsValid
                        ? ReactiveResult<EntityRef>.Ok(context.TriggerEntity)
                        : ReactiveErrorUtility.Fail<EntityRef>(ReactiveErrorCode.TargetNotFound, "Trigger entity is not available.", context);

                case ReactiveSnapshotEntityRefSourceKind.EntityValueStore:
                    return ResolveEntityStoreValue<EntityRef>(context, value.EntityValue);

                case ReactiveSnapshotEntityRefSourceKind.KernelValueStore:
                    return ResolveKernelStoreValue<EntityRef>(context, value.LocalValue);

                case ReactiveSnapshotEntityRefSourceKind.TargetReference:
                    return ResolveTargetReferenceSingle(context, value.TargetReferenceValue);

                default:
                    return FailUnsupportedSource<EntityRef>(nameof(ReactiveSnapshotEntityRef), value.SourceKind.ToString(), context);
            }
        }

        public ReactiveResult<EntityRef> ResolveWatchedEntityRef(in ReactiveEvalContext context, in ReactiveWatchedEntityRef value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedEntityRefSourceKind.EntityValueStore => ResolveEntityStoreValue<EntityRef>(context, value.EntityValue),
                ReactiveWatchedEntityRefSourceKind.KernelValueStore => ResolveKernelStoreValue<EntityRef>(context, value.LocalValue),
                _ => FailUnsupportedSource<EntityRef>(nameof(ReactiveWatchedEntityRef), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<string> ResolveString(in ReactiveEvalContext context, in ReactiveString value)
        {
            return value.SourceKind switch
            {
                ReactiveStringSourceKind.Literal => ReactiveResult<string>.Ok(value.Literal),
                ReactiveStringSourceKind.EntityValueStore => ResolveEntityStoreValue<string>(context, value.EntityValue),
                ReactiveStringSourceKind.KernelValueStore => ResolveKernelStoreValue<string>(context, value.LocalValue),
                _ => FailUnsupportedSource<string>(nameof(ReactiveString), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<string> ResolveSnapshotString(in ReactiveEvalContext context, in ReactiveSnapshotString value)
        {
            return value.SourceKind switch
            {
                ReactiveSnapshotStringSourceKind.Literal => ReactiveResult<string>.Ok(value.Literal),
                ReactiveSnapshotStringSourceKind.EntityValueStore => ResolveEntityStoreValue<string>(context, value.EntityValue),
                ReactiveSnapshotStringSourceKind.KernelValueStore => ResolveKernelStoreValue<string>(context, value.LocalValue),
                _ => FailUnsupportedSource<string>(nameof(ReactiveSnapshotString), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<string> ResolveWatchedString(in ReactiveEvalContext context, in ReactiveWatchedString value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedStringSourceKind.EntityValueStore => ResolveEntityStoreValue<string>(context, value.EntityValue),
                ReactiveWatchedStringSourceKind.KernelValueStore => ResolveKernelStoreValue<string>(context, value.LocalValue),
                _ => FailUnsupportedSource<string>(nameof(ReactiveWatchedString), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<FaceExpressionId> ResolveFaceExpressionId(in ReactiveEvalContext context, in ReactiveFaceExpressionId value)
        {
            return value.SourceKind switch
            {
                ReactiveFaceExpressionIdSourceKind.Literal => ReactiveResult<FaceExpressionId>.Ok(value.Literal),
                ReactiveFaceExpressionIdSourceKind.EntityValueStore => ResolveEntityStoreValue<FaceExpressionId>(context, value.EntityValue),
                ReactiveFaceExpressionIdSourceKind.KernelValueStore => ResolveKernelStoreValue<FaceExpressionId>(context, value.LocalValue),
                _ => FailUnsupportedSource<FaceExpressionId>(nameof(ReactiveFaceExpressionId), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<ShapeExpressionId> ResolveShapeExpressionId(in ReactiveEvalContext context, in ReactiveShapeExpressionId value)
        {
            return value.SourceKind switch
            {
                ReactiveShapeExpressionIdSourceKind.Literal => ReactiveResult<ShapeExpressionId>.Ok(value.Literal),
                ReactiveShapeExpressionIdSourceKind.EntityValueStore => ResolveEntityStoreValue<ShapeExpressionId>(context, value.EntityValue),
                ReactiveShapeExpressionIdSourceKind.KernelValueStore => ResolveKernelStoreValue<ShapeExpressionId>(context, value.LocalValue),
                _ => FailUnsupportedSource<ShapeExpressionId>(nameof(ReactiveShapeExpressionId), value.SourceKind.ToString(), context),
            };
        }

        public ReactiveResult<EntityMoveState> ResolveEntityMoveState(in ReactiveEvalContext context, in ReactiveEntityMoveState value)
        {
            return value.SourceKind switch
            {
                ReactiveEntityMoveStateSourceKind.Literal => ReactiveResult<EntityMoveState>.Ok(value.Literal),
                ReactiveEntityMoveStateSourceKind.EntityValueStore => ResolveEntityStoreValue<EntityMoveState>(context, value.EntityValue),
                ReactiveEntityMoveStateSourceKind.KernelValueStore => ResolveKernelStoreValue<EntityMoveState>(context, value.LocalValue),
                _ => FailUnsupportedSource<EntityMoveState>(nameof(ReactiveEntityMoveState), value.SourceKind.ToString(), context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<float>> ResolveFloatWatch(in ReactiveEvalContext context, in ReactiveFloat value)
        {
            return value.SourceKind switch
            {
                ReactiveFloatSourceKind.EntityValueStore => ResolveEntityStoreHandle<float>(context, value.EntityValue),
                ReactiveFloatSourceKind.KernelValueStore => ResolveKernelStoreHandle<float>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<float>>(nameof(ReactiveFloat), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<float>> ResolveWatchedFloatHandle(in ReactiveEvalContext context, in ReactiveWatchedFloat value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedFloatSourceKind.EntityValueStore => ResolveEntityStoreHandle<float>(context, value.EntityValue),
                ReactiveWatchedFloatSourceKind.KernelValueStore => ResolveKernelStoreHandle<float>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<float>>(nameof(ReactiveWatchedFloat), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<int>> ResolveIntWatch(in ReactiveEvalContext context, in ReactiveInt value)
        {
            return value.SourceKind switch
            {
                ReactiveIntSourceKind.EntityValueStore => ResolveEntityStoreHandle<int>(context, value.EntityValue),
                ReactiveIntSourceKind.KernelValueStore => ResolveKernelStoreHandle<int>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<int>>(nameof(ReactiveInt), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<int>> ResolveWatchedIntHandle(in ReactiveEvalContext context, in ReactiveWatchedInt value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedIntSourceKind.EntityValueStore => ResolveEntityStoreHandle<int>(context, value.EntityValue),
                ReactiveWatchedIntSourceKind.KernelValueStore => ResolveKernelStoreHandle<int>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<int>>(nameof(ReactiveWatchedInt), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<bool>> ResolveBoolWatch(in ReactiveEvalContext context, in ReactiveBool value)
        {
            return value.SourceKind switch
            {
                ReactiveBoolSourceKind.EntityValueStore => ResolveEntityStoreHandle<bool>(context, value.EntityValue),
                ReactiveBoolSourceKind.KernelValueStore => ResolveKernelStoreHandle<bool>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<bool>>(nameof(ReactiveBool), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<bool>> ResolveWatchedBoolHandle(in ReactiveEvalContext context, in ReactiveWatchedBool value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedBoolSourceKind.EntityValueStore => ResolveEntityStoreHandle<bool>(context, value.EntityValue),
                ReactiveWatchedBoolSourceKind.KernelValueStore => ResolveKernelStoreHandle<bool>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<bool>>(nameof(ReactiveWatchedBool), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<EntityRef>> ResolveEntityWatch(in ReactiveEvalContext context, in ReactiveEntityRef value)
        {
            return value.SourceKind switch
            {
                ReactiveEntitySourceKind.EntityValueStore => ResolveEntityStoreHandle<EntityRef>(context, value.EntityValue),
                ReactiveEntitySourceKind.KernelValueStore => ResolveKernelStoreHandle<EntityRef>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<EntityRef>>(nameof(ReactiveEntityRef), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<EntityRef>> ResolveWatchedEntityRefHandle(in ReactiveEvalContext context, in ReactiveWatchedEntityRef value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedEntityRefSourceKind.EntityValueStore => ResolveEntityStoreHandle<EntityRef>(context, value.EntityValue),
                ReactiveWatchedEntityRefSourceKind.KernelValueStore => ResolveKernelStoreHandle<EntityRef>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<EntityRef>>(nameof(ReactiveWatchedEntityRef), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<string>> ResolveStringWatch(in ReactiveEvalContext context, in ReactiveString value)
        {
            return value.SourceKind switch
            {
                ReactiveStringSourceKind.EntityValueStore => ResolveEntityStoreHandle<string>(context, value.EntityValue),
                ReactiveStringSourceKind.KernelValueStore => ResolveKernelStoreHandle<string>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<string>>(nameof(ReactiveString), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<string>> ResolveWatchedStringHandle(in ReactiveEvalContext context, in ReactiveWatchedString value)
        {
            return value.SourceKind switch
            {
                ReactiveWatchedStringSourceKind.EntityValueStore => ResolveEntityStoreHandle<string>(context, value.EntityValue),
                ReactiveWatchedStringSourceKind.KernelValueStore => ResolveKernelStoreHandle<string>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<string>>(nameof(ReactiveWatchedString), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<FaceExpressionId>> ResolveFaceExpressionIdWatch(in ReactiveEvalContext context, in ReactiveFaceExpressionId value)
        {
            return value.SourceKind switch
            {
                ReactiveFaceExpressionIdSourceKind.EntityValueStore => ResolveEntityStoreHandle<FaceExpressionId>(context, value.EntityValue),
                ReactiveFaceExpressionIdSourceKind.KernelValueStore => ResolveKernelStoreHandle<FaceExpressionId>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<FaceExpressionId>>(nameof(ReactiveFaceExpressionId), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<ShapeExpressionId>> ResolveShapeExpressionIdWatch(in ReactiveEvalContext context, in ReactiveShapeExpressionId value)
        {
            return value.SourceKind switch
            {
                ReactiveShapeExpressionIdSourceKind.EntityValueStore => ResolveEntityStoreHandle<ShapeExpressionId>(context, value.EntityValue),
                ReactiveShapeExpressionIdSourceKind.KernelValueStore => ResolveKernelStoreHandle<ShapeExpressionId>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<ShapeExpressionId>>(nameof(ReactiveShapeExpressionId), ReactiveEvaluationMode.Watched, context),
            };
        }

        internal ReactiveResult<ValueWatchHandle<EntityMoveState>> ResolveEntityMoveStateWatch(in ReactiveEvalContext context, in ReactiveEntityMoveState value)
        {
            return value.SourceKind switch
            {
                ReactiveEntityMoveStateSourceKind.EntityValueStore => ResolveEntityStoreHandle<EntityMoveState>(context, value.EntityValue),
                ReactiveEntityMoveStateSourceKind.KernelValueStore => ResolveKernelStoreHandle<EntityMoveState>(context, value.LocalValue),
                _ => FailUnsupportedEvaluationMode<ValueWatchHandle<EntityMoveState>>(nameof(ReactiveEntityMoveState), ReactiveEvaluationMode.Watched, context),
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

        private ReactiveResult<bool> ResolveCompareNumber(
            in ReactiveEvalContext context,
            in ReactiveNumberCompareSource source)
        {
            ReactiveResult<double> left = ResolveNumberOperand(context, source.LeftValueKind, source.LeftFloat, source.LeftInt);

            if (!left.Success)
                return ReactiveResult<bool>.Fail(left.Error);

            ReactiveResult<double> right = ResolveNumberOperand(context, source.RightValueKind, source.RightFloat, source.RightInt);

            if (!right.Success)
                return ReactiveResult<bool>.Fail(right.Error);

            return ReactiveResult<bool>.Ok(EvaluateComparison(left.Value, right.Value, source.Comparison, source.Epsilon));
        }

        private ReactiveResult<double> ResolveNumberOperand(
            in ReactiveEvalContext context,
            ReactiveNumberValueKind valueKind,
            in ReactiveFloat floatValue,
            in ReactiveInt intValue)
        {
            switch (valueKind)
            {
                case ReactiveNumberValueKind.Float:
                    ReactiveResult<float> floatResult = ResolveFloat(context, floatValue);
                    return floatResult.Success
                        ? ReactiveResult<double>.Ok(floatResult.Value)
                        : ReactiveResult<double>.Fail(floatResult.Error);

                case ReactiveNumberValueKind.Int:
                    ReactiveResult<int> intResult = ResolveInt(context, intValue);
                    return intResult.Success
                        ? ReactiveResult<double>.Ok(intResult.Value)
                        : ReactiveResult<double>.Fail(intResult.Error);

                default:
                    return ReactiveErrorUtility.Fail<double>(
                        ReactiveErrorCode.UnsupportedSource,
                        $"ReactiveNumberValueKind '{valueKind}' is not supported.",
                        context);
            }
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

        private ReactiveResult<T> ResolveKernelStoreValue<T>(
            in ReactiveEvalContext context,
            in ReactiveKernelValueSource source)
        {
            // KernelValueStore は StoreScope で lifetime を選ぶ。
            // SceneKernel scope を EntityValueStore にフォールバックさせると、scene-wide value の未配線を
            // entity target 解決の問題として隠してしまうため、ここでは kernel store だけを見る。
            IKernelValueStoreService kernelValueStore = context.GetKernelValueStore(source.StoreScope);

            if (kernelValueStore == null)
            {
                return ReactiveErrorUtility.Fail<T>(
                    ReactiveErrorCode.MissingValueStore,
                    $"{source.StoreScope} KernelValueStore is not available in the current ActionExecution.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<T>.Fail(keyError);

            try
            {
                T resolvedValue = kernelValueStore.Get(key);
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

        private ReactiveResult<ValueWatchHandle<T>> ResolveKernelStoreHandle<T>(
            in ReactiveEvalContext context,
            in ReactiveKernelValueSource source)
        {
            // Watched binding でも snapshot read と同じ store 解決を使う。
            // 読み取りと購読で別 store を見ないことが、ReactiveValue の一貫性の前提になる。
            IKernelValueStoreService kernelValueStore = context.GetKernelValueStore(source.StoreScope);

            if (kernelValueStore == null)
            {
                return ReactiveErrorUtility.Fail<ValueWatchHandle<T>>(
                    ReactiveErrorCode.MissingValueStore,
                    $"{source.StoreScope} KernelValueStore is not available in the current ActionExecution.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<ValueWatchHandle<T>>.Fail(keyError);

            try
            {
                ValueWatchHandle<T> handle = kernelValueStore.GetHandle(key);
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

        private ReactiveResult<T> ResolveSceneKernelEntityStoreValue<T>(
            in ReactiveEvalContext context,
            in ReactiveKernelValueSource source)
        {
            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<T>.Fail(kernelError);

            if (resolvedSceneKernel.EntityValueStore == null)
            {
                return ReactiveErrorUtility.Fail<T>(
                    ReactiveErrorCode.MissingValueStore,
                    "EntityValueStore is not available on the current SceneKernel.",
                    context);
            }

            if (!TryResolveSceneKernelEntity(resolvedSceneKernel, out EntityRef sceneKernelEntity))
            {
                return ReactiveErrorUtility.Fail<T>(
                    ReactiveErrorCode.TargetNotFound,
                    "SceneKernel entity was not found in SceneKernel.EntitiesRegistry.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<T>.Fail(keyError);

            try
            {
                T resolvedValue = resolvedSceneKernel.EntityValueStore.Get(sceneKernelEntity, key);
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

        private ReactiveResult<ValueWatchHandle<T>> ResolveSceneKernelEntityStoreHandle<T>(
            in ReactiveEvalContext context,
            in ReactiveKernelValueSource source)
        {
            if (!TryResolveSceneKernel(context, out SceneKernel resolvedSceneKernel, out ReactiveError kernelError))
                return ReactiveResult<ValueWatchHandle<T>>.Fail(kernelError);

            if (resolvedSceneKernel.EntityValueStore == null)
            {
                return ReactiveErrorUtility.Fail<ValueWatchHandle<T>>(
                    ReactiveErrorCode.MissingValueStore,
                    "EntityValueStore is not available on the current SceneKernel.",
                    context);
            }

            if (!TryResolveSceneKernelEntity(resolvedSceneKernel, out EntityRef sceneKernelEntity))
            {
                return ReactiveErrorUtility.Fail<ValueWatchHandle<T>>(
                    ReactiveErrorCode.TargetNotFound,
                    "SceneKernel entity was not found in SceneKernel.EntitiesRegistry.",
                    context);
            }

            if (!TryResolveValueKey(source.Key, context, out ValueKey<T> key, out ReactiveError keyError))
                return ReactiveResult<ValueWatchHandle<T>>.Fail(keyError);

            try
            {
                ValueWatchHandle<T> handle = resolvedSceneKernel.EntityValueStore.GetHandle(sceneKernelEntity, key);
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

        private static bool TryResolveSceneKernelEntity(SceneKernel sceneKernel, out EntityRef sceneKernelEntity)
        {
            sceneKernelEntity = default;

            if (sceneKernel?.EntitiesRegistry == null)
                return false;

            var entities = sceneKernel.EntitiesRegistry.GetEntitiesByTag(EntityTags.System.SceneKernel.Id);
            for (int index = 0; index < entities.Count; index++)
            {
                if (!entities[index].IsValid)
                    continue;

                sceneKernelEntity = entities[index];
                return true;
            }

            return false;
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

            EntityResolveContext resolveContext = new(resolvedSceneKernel, context.ActorEntity, context.TriggerEntity);
            var results = new List<EntityRef>(2);
            int count = ScopedEntityResolveUtility.ResolveTargets(resolveContext, EntityResolveScope.Entity, target, results);

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

            if (count > 1 || target.Selection == EntityTargetSelection.All)
            {
                return ReactiveErrorUtility.Fail<EntityRef>(
                    ReactiveErrorCode.MultipleTargetsNotAllowed,
                    "ReactiveEntityRef TargetReference requires a single target selection.",
                    context);
            }

            return ReactiveResult<EntityRef>.Ok(results[0]);
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
            double left,
            double right,
            ReactiveNumberComparisonKind comparison,
            float epsilon)
        {
            double safeEpsilon = Math.Max(0d, epsilon);

            return comparison switch
            {
                ReactiveNumberComparisonKind.Equal => Math.Abs(left - right) <= safeEpsilon,
                ReactiveNumberComparisonKind.NotEqual => Math.Abs(left - right) > safeEpsilon,
                ReactiveNumberComparisonKind.Greater => left > right + safeEpsilon,
                ReactiveNumberComparisonKind.GreaterOrEqual => left >= right - safeEpsilon,
                ReactiveNumberComparisonKind.Less => left < right - safeEpsilon,
                ReactiveNumberComparisonKind.LessOrEqual => left <= right + safeEpsilon,
                _ => false,
            };
        }

    }
}
