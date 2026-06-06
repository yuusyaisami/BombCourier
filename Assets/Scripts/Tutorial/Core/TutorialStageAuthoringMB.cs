using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace BC.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialStageAuthoringMB : MonoBehaviour
    {
        [SerializeField] private EntityTargetReference actionActor = EntityTargetReference.Self();
        [SerializeField] private List<TutorialStepAuthoring> steps = new();

        public EntityTargetReference ActionActor => actionActor;
        public IReadOnlyList<TutorialStepAuthoring> Steps => steps;
        public bool HasSteps => steps != null && steps.Count > 0;

        public TutorialValidationContext ValidateDefinition()
        {
            TutorialValidationContext context = new();

            if (steps == null || steps.Count == 0)
            {
                context.AddError($"{nameof(TutorialStageAuthoringMB)} requires at least one step.");
                return context;
            }

            HashSet<string> explicitStepIds = new(StringComparer.Ordinal);
            for (int i = 0; i < steps.Count; i++)
            {
                TutorialStepAuthoring step = steps[i];
                if (step == null)
                {
                    context.AddError($"Tutorial step at index {i} is missing.");
                    continue;
                }

                if (step.StepId.IsAssigned && !explicitStepIds.Add(step.StepId.Value))
                    context.AddError($"Duplicate tutorial step id '{step.StepId.Value}'.");

                step.Validate(context, $"steps[{i}]");
            }

            for (int i = 0; i < steps.Count; i++)
            {
                TutorialStepAuthoring step = steps[i];
                if (step == null || !step.NextStepId.IsAssigned)
                    continue;

                if (!explicitStepIds.Contains(step.NextStepId.Value))
                    context.AddError($"Tutorial step '{ResolveStepLabel(step, i)}' points to unknown next step id '{step.NextStepId.Value}'.");
            }

            return context;
        }

        private static string ResolveStepLabel(TutorialStepAuthoring step, int index)
        {
            return step != null && step.StepId.IsAssigned ? step.StepId.Value : index.ToString();
        }
    }

    [Serializable]
    public sealed class TutorialStepAuthoring
    {
        [SerializeField] private TutorialStepId stepId;
        [SerializeField] private TutorialPlayerControlPolicy playerControlPolicy = TutorialPlayerControlPolicy.LockDuringEnterActions;
        [SerializeField] private InlineAction onEnter;
        [SerializeField] private InlineAction onComplete;
        [SerializeField] private TutorialStepId nextStepId;
        [SerializeReference] private TutorialConditionAuthoring completionCondition;
        [SerializeField] private List<TutorialToDoEntryAuthoring> todoEntries = new();

        public TutorialStepId StepId => stepId;
        public TutorialPlayerControlPolicy PlayerControlPolicy => playerControlPolicy;
        public InlineAction OnEnter => onEnter;
        public InlineAction OnComplete => onComplete;
        public TutorialStepId NextStepId => nextStepId;
        public TutorialConditionAuthoring CompletionCondition => completionCondition;
        public IReadOnlyList<TutorialToDoEntryAuthoring> ToDoEntries => todoEntries;

        public void Validate(TutorialValidationContext context, string ownerPath)
        {
            completionCondition?.Validate(context, $"{ownerPath}.completionCondition");

            if (todoEntries == null)
                return;

            for (int i = 0; i < todoEntries.Count; i++)
            {
                TutorialToDoEntryAuthoring entry = todoEntries[i];
                if (entry == null)
                {
                    context.AddError($"{ownerPath}.todoEntries[{i}] is missing.");
                    continue;
                }

                entry.Validate(context, $"{ownerPath}.todoEntries[{i}]");
            }
        }
    }

    [Serializable]
    public sealed class TutorialToDoEntryAuthoring
    {
        [Tooltip("ToDo に表示するフォールバックテキスト（Key が見つからない場合に表示）。")]
        [SerializeField] private string labelText;
        [Tooltip("ローカライズ用 String Table。Key で引けなければ labelText を表示する。")]
        [SerializeField] private LocalizedStringTable table;
        [Tooltip("ローカライズ用エントリ Key。")]
        [SerializeField] private string entry;
        [SerializeReference] private TutorialConditionAuthoring condition;

        public string LabelText => labelText ?? string.Empty;
        public LocalizedStringTable Table => table;
        public string Entry => entry;
        public TutorialConditionAuthoring Condition => condition;

        public void Validate(TutorialValidationContext context, string ownerPath)
        {
            if (string.IsNullOrWhiteSpace(labelText))
                context.AddError($"{ownerPath}.labelText is required.");

            if (condition == null)
            {
                context.AddError($"{ownerPath}.condition is required.");
                return;
            }

            condition.Validate(context, $"{ownerPath}.condition");
        }
    }
}
