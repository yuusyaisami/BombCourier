using System;
using BC.ActionSystem;

namespace BC.Base
{
    public enum ReactiveEvaluationMode
    {
        Snapshot = 0,
        Watched = 1,
        Continuous = 2,
    }

    public enum ReactiveFailurePolicy
    {
        FailAction = 0,
        UseFallback = 1,
    }

    public enum ReactiveErrorCode
    {
        None = 0,
        MissingSceneKernel = 1,
        MissingValueStore = 2,
        MissingEntityComponentResolver = 3,
        InvalidEntity = 4,
        EntityNotAlive = 5,
        TargetNotFound = 6,
        MultipleTargetsNotAllowed = 7,
        ValueKeyNotAssigned = 8,
        ValueKeyTypeMismatch = 9,
        ValueStoreReadFailed = 10,
        TransformNotFound = 11,
        UnsupportedSource = 12,
        UnsupportedEvaluationMode = 13,
    }

    // M1 は contract-only milestone とし、Action context 依存は薄い変換口だけに留める。
    public readonly struct ReactiveEvalContext
    {
        public readonly SceneKernel SceneKernel;
        public readonly ApplicationKernel ApplicationKernel;
        public readonly EntityRef ActorEntity;
        public readonly EntityRef TriggerEntity;
        public readonly IKernelValueStoreService KernelValueStore;
        public readonly IKernelValueStoreService ApplicationKernelValueStore;

        public ReactiveEvalContext(SceneKernel sceneKernel, EntityRef actorEntity, EntityRef triggerEntity)
            : this(sceneKernel, actorEntity, triggerEntity, null, ApplicationKernelMB.Instance?.Kernel, ApplicationKernelMB.Instance?.Kernel?.KernelValueStore)
        {
        }

        public ReactiveEvalContext(SceneKernel sceneKernel, EntityRef actorEntity, EntityRef triggerEntity, IKernelValueStoreService kernelValueStore)
            : this(sceneKernel, actorEntity, triggerEntity, kernelValueStore, ApplicationKernelMB.Instance?.Kernel, ApplicationKernelMB.Instance?.Kernel?.KernelValueStore)
        {
        }

        private ReactiveEvalContext(
            SceneKernel sceneKernel,
            EntityRef actorEntity,
            EntityRef triggerEntity,
            IKernelValueStoreService kernelValueStore,
            ApplicationKernel applicationKernel,
            IKernelValueStoreService applicationKernelValueStore)
        {
            SceneKernel = sceneKernel;
            ApplicationKernel = applicationKernel;
            ActorEntity = actorEntity;
            TriggerEntity = triggerEntity;
            KernelValueStore = kernelValueStore;
            ApplicationKernelValueStore = applicationKernelValueStore;
        }

        public ReactiveEvalContext(in ActionExecutionContext actionContext)
            : this(
                actionContext.SceneKernel,
                actionContext.ActorEntity,
                actionContext.TriggerEntity,
                actionContext.SceneKernel?.KernelValueStore,
                ApplicationKernelMB.Instance?.Kernel,
                ApplicationKernelMB.Instance?.Kernel?.KernelValueStore)
        {
        }

        public EntityRef SelfEntity => ActorEntity;

        public IKernelValueStoreService GetKernelValueStore(ReactiveKernelValueStoreScope scope)
        {
            return scope switch
            {
                ReactiveKernelValueStoreScope.SceneKernel => KernelValueStore,
                ReactiveKernelValueStoreScope.ApplicationKernel => ApplicationKernelValueStore,
                _ => null,
            };
        }
    }

    public static class ReactiveBindingPolicy
    {
        // Inspector does not author evaluation policy; runtime code selects it when binding.
        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveFloatSourceKind sourceKind)
        {
            return sourceKind == ReactiveFloatSourceKind.EntityValueStore ||
                   sourceKind == ReactiveFloatSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveIntSourceKind sourceKind)
        {
            return sourceKind == ReactiveIntSourceKind.EntityValueStore ||
                   sourceKind == ReactiveIntSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveBoolSourceKind sourceKind)
        {
            return sourceKind == ReactiveBoolSourceKind.EntityValueStore ||
                   sourceKind == ReactiveBoolSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveVector3SourceKind sourceKind)
        {
            return ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveEntitySourceKind sourceKind)
        {
            return sourceKind == ReactiveEntitySourceKind.EntityValueStore ||
                   sourceKind == ReactiveEntitySourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveStringSourceKind sourceKind)
        {
            return sourceKind == ReactiveStringSourceKind.EntityValueStore ||
                   sourceKind == ReactiveStringSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveFaceExpressionIdSourceKind sourceKind)
        {
            return sourceKind == ReactiveFaceExpressionIdSourceKind.EntityValueStore ||
                   sourceKind == ReactiveFaceExpressionIdSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveShapeExpressionIdSourceKind sourceKind)
        {
            return sourceKind == ReactiveShapeExpressionIdSourceKind.EntityValueStore ||
               sourceKind == ReactiveShapeExpressionIdSourceKind.KernelValueStore
            ? ReactiveEvaluationMode.Watched
            : ReactiveEvaluationMode.Snapshot;
        }

        public static ReactiveEvaluationMode GetDefaultBindingMode(ReactiveEntityMoveStateSourceKind sourceKind)
        {
            return sourceKind == ReactiveEntityMoveStateSourceKind.EntityValueStore ||
                   sourceKind == ReactiveEntityMoveStateSourceKind.KernelValueStore
                ? ReactiveEvaluationMode.Watched
                : ReactiveEvaluationMode.Snapshot;
        }
    }
}