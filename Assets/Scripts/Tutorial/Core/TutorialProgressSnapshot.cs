using System;

namespace BC.Tutorial
{
    public readonly struct TutorialProgressSnapshot
    {
        public static readonly TutorialProgressSnapshot None = default;
        public static readonly TutorialProgressSnapshot Completed = new TutorialProgressSnapshot(true);

        public TutorialProgressSnapshot(
            int stepIndex,
            TutorialStepId stepId,
            bool completionConditionCompleted,
            object completionConditionState,
            TutorialToDoProgressSnapshot[] todoSnapshots)
        {
            StepIndex = stepIndex;
            StepId = stepId;
            CompletionConditionCompleted = completionConditionCompleted;
            CompletionConditionState = completionConditionState;
            TodoSnapshots = todoSnapshots ?? Array.Empty<TutorialToDoProgressSnapshot>();
            IsValid = true;
            IsCompleted = false;
        }

        private TutorialProgressSnapshot(bool completed)
        {
            StepIndex = -1;
            StepId = default;
            CompletionConditionCompleted = true;
            CompletionConditionState = null;
            TodoSnapshots = Array.Empty<TutorialToDoProgressSnapshot>();
            IsValid = true;
            IsCompleted = completed;
        }

        public bool IsValid { get; }
        public bool IsCompleted { get; }
        public int StepIndex { get; }
        public TutorialStepId StepId { get; }
        public bool CompletionConditionCompleted { get; }
        public object CompletionConditionState { get; }
        public TutorialToDoProgressSnapshot[] TodoSnapshots { get; }
    }

    public readonly struct TutorialToDoProgressSnapshot
    {
        public TutorialToDoProgressSnapshot(bool completed, object conditionState)
        {
            Completed = completed;
            ConditionState = conditionState;
        }

        public bool Completed { get; }
        public object ConditionState { get; }
    }
}
