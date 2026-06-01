using System;
using BC.Base;
using UnityEngine;

namespace BC.Tutorial
{
    [Serializable]
    public struct TutorialStepId : IEquatable<TutorialStepId>
    {
        [SerializeField] private string value;

        public TutorialStepId(string value)
        {
            this.value = value ?? string.Empty;
        }

        public string Value => value ?? string.Empty;
        public bool IsAssigned => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(TutorialStepId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TutorialStepId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return IsAssigned ? Value : "(None)";
        }
    }

    public enum TutorialPlayerControlPolicy
    {
        None = 0,
        LockDuringEnterActions = 10,
        LockUntilStepComplete = 20,
    }

    public readonly struct TutorialConditionContext
    {
        public TutorialConditionContext(
            SceneKernel sceneKernel,
            TutorialStageAuthoringMB stageAuthoring,
            EntityRef playerEntity,
            EntityRef actorEntity)
        {
            SceneKernel = sceneKernel;
            StageAuthoring = stageAuthoring;
            PlayerEntity = playerEntity;
            ActorEntity = actorEntity;
        }

        public SceneKernel SceneKernel { get; }
        public TutorialStageAuthoringMB StageAuthoring { get; }
        public EntityRef PlayerEntity { get; }
        public EntityRef ActorEntity { get; }
        public ValueStoreService ValueStore => SceneKernel?.ValueStore;
    }

    public interface ITutorialConditionRuntime : IDisposable
    {
        event Action Completed;
        bool IsCompleted { get; }
        void Start(in TutorialConditionContext context, object restoredState);
        void Tick(float deltaTime);
        object CaptureState();
    }

    [Serializable]
    public abstract class TutorialConditionAuthoring
    {
        public abstract ITutorialConditionRuntime CreateRuntime();

        public virtual void Validate(TutorialValidationContext context, string ownerPath)
        {
        }
    }

    public sealed class TutorialValidationContext
    {
        private readonly System.Collections.Generic.List<string> errors = new();

        public bool IsValid => errors.Count == 0;
        public System.Collections.Generic.IReadOnlyList<string> Errors => errors;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }
    }
}
