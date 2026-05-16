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
        public readonly EntityRef ActorEntity;
        public readonly EntityRef TriggerEntity;

        public ReactiveEvalContext(SceneKernel sceneKernel, EntityRef actorEntity, EntityRef triggerEntity)
        {
            SceneKernel = sceneKernel;
            ActorEntity = actorEntity;
            TriggerEntity = triggerEntity;
        }

        public ReactiveEvalContext(in ActionExecutionContext actionContext)
            : this(actionContext.SceneKernel, actionContext.ActorEntity, actionContext.TriggerEntity)
        {
        }

        public EntityRef SelfEntity => ActorEntity;
    }
}