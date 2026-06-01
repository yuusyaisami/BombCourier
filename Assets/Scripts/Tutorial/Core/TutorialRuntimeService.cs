using System;
using System.Collections.Generic;
using System.Threading;
using BC.ActionSystem;
using BC.Base;
using BC.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.Tutorial
{
    public sealed class TutorialRuntimeService : ITickable
    {
        private static readonly ValueModifierTagId InputLockTag = new ValueModifierTagId(12001);

        private readonly SceneKernel sceneKernel;
        private readonly Dictionary<string, int> explicitStepIndexById = new(StringComparer.Ordinal);
        private readonly List<EntityRef> actorResolveBuffer = new(2);

        private TutorialStageAuthoringMB activeAuthoring;
        private IReadOnlyList<TutorialStepAuthoring> activeSteps = Array.Empty<TutorialStepAuthoring>();
        private UITutorialToDoListMB todoListUI;
        private EntityRef playerEntity;
        private EntityRef actionActor;
        private CancellationTokenSource runCancellation;
        private int currentStepIndex = -1;
        private TutorialStepId currentStepId;
        private bool isRunning;
        private bool isAdvancing;
        private bool lastRunCompleted;
        private bool currentCompletionConditionCompleted;
        private bool inputLockActive;
        private bool shouldLockUntilStepComplete;
        private bool[] currentTodoCompleted = Array.Empty<bool>();
        private ITutorialConditionRuntime completionConditionRuntime;
        private ITutorialConditionRuntime[] todoConditionRuntimes = Array.Empty<ITutorialConditionRuntime>();

        public TutorialRuntimeService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
        }

        public bool IsRunning => isRunning;

        public bool Start(
            TutorialStageAuthoringMB authoring,
            EntityRef player,
            UITutorialToDoListMB todoList,
            TutorialProgressSnapshot restoreSnapshot = default)
        {
            Stop();

            if (authoring == null || !authoring.HasSteps)
                return false;

            TutorialValidationContext validation = authoring.ValidateDefinition();
            if (!validation.IsValid)
            {
                LogValidationErrors(authoring, validation);
                return false;
            }

            if (!TryResolveActionActor(authoring, player, out EntityRef resolvedActor))
            {
                Debug.LogError($"{nameof(TutorialRuntimeService)}: failed to resolve tutorial action actor.", authoring);
                return false;
            }

            if (restoreSnapshot.IsValid && restoreSnapshot.IsCompleted)
            {
                lastRunCompleted = true;
                return false;
            }

            activeAuthoring = authoring;
            activeSteps = authoring.Steps;
            todoListUI = todoList;
            playerEntity = player;
            actionActor = resolvedActor;
            runCancellation = new CancellationTokenSource();
            currentStepIndex = ResolveStartStepIndex(restoreSnapshot);
            explicitStepIndexById.Clear();
            BuildExplicitStepIndex();
            isRunning = true;
            lastRunCompleted = false;

            BeginStepAsync(currentStepIndex, restoreSnapshot).Forget();
            return true;
        }

        public void Stop()
        {
            if (runCancellation != null)
            {
                runCancellation.Cancel();
                runCancellation.Dispose();
                runCancellation = null;
            }

            ClearCurrentStepRuntime();
            ClearInputLock();
            todoListUI?.Hide();
            activeAuthoring = null;
            activeSteps = Array.Empty<TutorialStepAuthoring>();
            todoListUI = null;
            playerEntity = default;
            actionActor = default;
            currentStepIndex = -1;
            currentStepId = default;
            currentCompletionConditionCompleted = false;
            currentTodoCompleted = Array.Empty<bool>();
            isRunning = false;
            isAdvancing = false;
            lastRunCompleted = false;
            shouldLockUntilStepComplete = false;
            explicitStepIndexById.Clear();
        }

        public TutorialProgressSnapshot CaptureSnapshot()
        {
            if (!isRunning || currentStepIndex < 0 || currentStepIndex >= activeSteps.Count)
                return lastRunCompleted ? TutorialProgressSnapshot.Completed : TutorialProgressSnapshot.None;

            TutorialToDoProgressSnapshot[] todoSnapshots = new TutorialToDoProgressSnapshot[currentTodoCompleted.Length];
            for (int i = 0; i < todoSnapshots.Length; i++)
            {
                object conditionState = null;
                if (i < todoConditionRuntimes.Length && todoConditionRuntimes[i] != null)
                {
                    conditionState = CaptureConditionState(todoConditionRuntimes[i]);
                }

                todoSnapshots[i] = new TutorialToDoProgressSnapshot(currentTodoCompleted[i], conditionState);
            }

            object completionState = completionConditionRuntime != null
                ? CaptureConditionState(completionConditionRuntime)
                : null;

            return new TutorialProgressSnapshot(
                currentStepIndex,
                currentStepId,
                currentCompletionConditionCompleted,
                completionState,
                todoSnapshots);
        }

        public void Tick(float deltaTime)
        {
            if (!isRunning || isAdvancing)
                return;

            TickCondition(completionConditionRuntime, deltaTime);

            for (int i = 0; i < todoConditionRuntimes.Length; i++)
                TickCondition(todoConditionRuntimes[i], deltaTime);
        }

        private async UniTaskVoid BeginStepAsync(int stepIndex, TutorialProgressSnapshot restoreSnapshot)
        {
            if (!isRunning)
                return;

            if (stepIndex < 0 || stepIndex >= activeSteps.Count)
            {
                Debug.LogError($"{nameof(TutorialRuntimeService)}: step index {stepIndex} is out of range.", activeAuthoring);
                Stop();
                return;
            }

            ClearCurrentStepRuntime();
            currentStepIndex = stepIndex;
            TutorialStepAuthoring step = activeSteps[stepIndex];
            currentStepId = ResolveRuntimeStepId(step, stepIndex);
            shouldLockUntilStepComplete = step.PlayerControlPolicy == TutorialPlayerControlPolicy.LockUntilStepComplete;

            try
            {
                if (step.PlayerControlPolicy != TutorialPlayerControlPolicy.None)
                    ApplyInputLock();

                ActionExecutionResult enterResult = await ExecuteInlineActionAsync(step.OnEnter, runCancellation.Token);
                if (!HandleActionResult(enterResult, "enter action"))
                    return;

                if (step.PlayerControlPolicy == TutorialPlayerControlPolicy.LockDuringEnterActions)
                    ClearInputLock();

                PrepareStepRuntime(step, restoreSnapshot);
                EvaluateStepCompletion();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, activeAuthoring);
                Stop();
            }
        }

        private void PrepareStepRuntime(TutorialStepAuthoring step, TutorialProgressSnapshot restoreSnapshot)
        {
            IReadOnlyList<TutorialToDoEntryAuthoring> todoEntries = step.ToDoEntries ?? Array.Empty<TutorialToDoEntryAuthoring>();
            currentTodoCompleted = new bool[todoEntries.Count];
            todoConditionRuntimes = new ITutorialConditionRuntime[todoEntries.Count];

            bool useRestore = restoreSnapshot.IsValid && restoreSnapshot.StepIndex == currentStepIndex;
            if (useRestore)
            {
                int count = Mathf.Min(currentTodoCompleted.Length, restoreSnapshot.TodoSnapshots.Length);
                for (int i = 0; i < count; i++)
                    currentTodoCompleted[i] = restoreSnapshot.TodoSnapshots[i].Completed;
            }

            string[] labels = new string[todoEntries.Count];
            for (int i = 0; i < todoEntries.Count; i++)
                labels[i] = todoEntries[i]?.LabelText ?? string.Empty;

            if (todoEntries.Count > 0)
            {
                if (todoListUI == null)
                {
                    Debug.LogError($"{nameof(TutorialRuntimeService)}: ToDo entries are defined, but {nameof(UITutorialToDoListMB)} is not assigned.", activeAuthoring);
                    Stop();
                    return;
                }

                todoListUI.ShowChecklist(labels, currentTodoCompleted);
            }
            else
            {
                todoListUI?.Hide();
            }

            if (step.CompletionCondition == null || (useRestore && restoreSnapshot.CompletionConditionCompleted))
            {
                currentCompletionConditionCompleted = true;
            }
            else
            {
                currentCompletionConditionCompleted = false;
                object restoredState = useRestore ? restoreSnapshot.CompletionConditionState : null;
                completionConditionRuntime = StartConditionRuntime(
                    step.CompletionCondition,
                    restoredState,
                    CompleteStepCondition);

                if (completionConditionRuntime != null && completionConditionRuntime.IsCompleted)
                    CompleteStepCondition();
            }

            for (int i = 0; i < todoEntries.Count; i++)
            {
                if (currentTodoCompleted[i])
                    continue;

                TutorialToDoEntryAuthoring entry = todoEntries[i];
                object restoredState = useRestore && i < restoreSnapshot.TodoSnapshots.Length
                    ? restoreSnapshot.TodoSnapshots[i].ConditionState
                    : null;
                int capturedIndex = i;
                todoConditionRuntimes[i] = StartConditionRuntime(
                    entry.Condition,
                    restoredState,
                    () => CompleteToDo(capturedIndex));

                if (todoConditionRuntimes[i] != null && todoConditionRuntimes[i].IsCompleted)
                    CompleteToDo(capturedIndex);
            }
        }

        private ITutorialConditionRuntime StartConditionRuntime(
            TutorialConditionAuthoring condition,
            object restoredState,
            Action onCompleted)
        {
            if (condition == null)
                return null;

            ITutorialConditionRuntime runtime = condition.CreateRuntime();
            if (runtime == null)
                throw new InvalidOperationException($"{condition.GetType().Name} returned a null tutorial condition runtime.");

            runtime.Start(new TutorialConditionContext(sceneKernel, activeAuthoring, playerEntity, actionActor), restoredState);
            runtime.Completed += onCompleted;
            return runtime;
        }

        private void CompleteStepCondition()
        {
            currentCompletionConditionCompleted = true;
            DisposeCondition(ref completionConditionRuntime);
            EvaluateStepCompletion();
        }

        private void CompleteToDo(int index)
        {
            if (index < 0 || index >= currentTodoCompleted.Length || currentTodoCompleted[index])
                return;

            currentTodoCompleted[index] = true;
            todoListUI?.SetItemCompleted(index, true);

            if (index < todoConditionRuntimes.Length)
                DisposeCondition(ref todoConditionRuntimes[index]);

            EvaluateStepCompletion();
        }

        private void EvaluateStepCompletion()
        {
            if (!isRunning || isAdvancing)
                return;

            if (!currentCompletionConditionCompleted)
                return;

            for (int i = 0; i < currentTodoCompleted.Length; i++)
            {
                if (!currentTodoCompleted[i])
                    return;
            }

            AdvanceStepAsync().Forget();
        }

        private async UniTaskVoid AdvanceStepAsync()
        {
            if (!isRunning || isAdvancing)
                return;

            isAdvancing = true;
            TutorialStepAuthoring step = activeSteps[currentStepIndex];

            try
            {
                ActionExecutionResult completeResult = await ExecuteInlineActionAsync(step.OnComplete, runCancellation.Token);
                if (!HandleActionResult(completeResult, "complete action"))
                    return;

                bool releaseInputLock = shouldLockUntilStepComplete;
                ClearCurrentStepRuntime();
                if (releaseInputLock)
                    ClearInputLock();

                int nextStepIndex = ResolveNextStepIndex(step);
                if (nextStepIndex < 0 || nextStepIndex >= activeSteps.Count)
                {
                    FinishTutorial();
                    return;
                }

                isAdvancing = false;
                BeginStepAsync(nextStepIndex, TutorialProgressSnapshot.None).Forget();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, activeAuthoring);
                Stop();
            }
            finally
            {
                if (isRunning && isAdvancing)
                    isAdvancing = false;
            }
        }

        private void FinishTutorial()
        {
            StopCompleted();
        }

        private void StopCompleted()
        {
            Stop();
            lastRunCompleted = true;
        }

        private int ResolveNextStepIndex(TutorialStepAuthoring step)
        {
            if (step.NextStepId.IsAssigned)
                return explicitStepIndexById.TryGetValue(step.NextStepId.Value, out int index) ? index : -1;

            return currentStepIndex + 1;
        }

        private int ResolveStartStepIndex(TutorialProgressSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                return 0;

            if (snapshot.StepId.IsAssigned)
            {
                BuildExplicitStepIndex();
                if (explicitStepIndexById.TryGetValue(snapshot.StepId.Value, out int stepIndexById))
                    return stepIndexById;
            }

            if (snapshot.StepIndex >= 0 && snapshot.StepIndex < activeSteps.Count)
                return snapshot.StepIndex;

            return 0;
        }

        private void BuildExplicitStepIndex()
        {
            explicitStepIndexById.Clear();

            for (int i = 0; i < activeSteps.Count; i++)
            {
                TutorialStepAuthoring step = activeSteps[i];
                if (step != null && step.StepId.IsAssigned)
                    explicitStepIndexById[step.StepId.Value] = i;
            }
        }

        private TutorialStepId ResolveRuntimeStepId(TutorialStepAuthoring step, int stepIndex)
        {
            if (step != null && step.StepId.IsAssigned)
                return step.StepId;

            return new TutorialStepId(stepIndex.ToString());
        }

        private async UniTask<ActionExecutionResult> ExecuteInlineActionAsync(
            InlineAction inlineAction,
            CancellationToken cancellationToken)
        {
            if (inlineAction == null)
                return ActionExecutionResult.Completed();

            if (!actionActor.IsValid)
                return ActionExecutionResult.Failed("Tutorial action actor is invalid.");

            if (sceneKernel.Actions == null)
                return ActionExecutionResult.Failed("SceneKernel.Actions is not available.");

            return await sceneKernel.Actions.ExecuteAsync(actionActor, inlineAction.Compile(), playerEntity, cancellationToken);
        }

        private bool HandleActionResult(ActionExecutionResult result, string phase)
        {
            if (result.IsCompleted)
                return true;

            if (runCancellation != null && runCancellation.IsCancellationRequested)
                return false;

            Debug.LogError($"{nameof(TutorialRuntimeService)}: tutorial {phase} failed. {result.Message}", activeAuthoring);
            Stop();
            return false;
        }

        private void ApplyInputLock()
        {
            if (inputLockActive || !playerEntity.IsValid || sceneKernel.ValueStore == null)
                return;

            sceneKernel.ValueStore.SetBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, InputLockTag, false);
            sceneKernel.ValueStore.SetBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, InputLockTag, false);
            sceneKernel.ValueStore.SetBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, InputLockTag, false);
            inputLockActive = true;
        }

        private void ClearInputLock()
        {
            if (!inputLockActive || !playerEntity.IsValid || sceneKernel.ValueStore == null)
            {
                inputLockActive = false;
                return;
            }

            sceneKernel.ValueStore.RemoveBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, InputLockTag);
            sceneKernel.ValueStore.RemoveBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, InputLockTag);
            sceneKernel.ValueStore.RemoveBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, InputLockTag);
            inputLockActive = false;
        }

        private bool TryResolveActionActor(
            TutorialStageAuthoringMB authoring,
            EntityRef fallbackPlayer,
            out EntityRef actor)
        {
            EntityResolveContext context = new(sceneKernel, fallbackPlayer, fallbackPlayer);
            int resolvedCount = ScopedEntityResolveUtility.ResolveTargets(
                context,
                EntityResolveScope.Entity,
                authoring.ActionActor,
                actorResolveBuffer);

            if (resolvedCount > 0)
            {
                actor = actorResolveBuffer[0];
                return actor.IsValid;
            }

            actor = default;
            return false;
        }

        private void ClearCurrentStepRuntime()
        {
            DisposeCondition(ref completionConditionRuntime);

            for (int i = 0; i < todoConditionRuntimes.Length; i++)
                DisposeCondition(ref todoConditionRuntimes[i]);

            todoConditionRuntimes = Array.Empty<ITutorialConditionRuntime>();
            currentTodoCompleted = Array.Empty<bool>();
            currentCompletionConditionCompleted = false;
            shouldLockUntilStepComplete = false;
        }

        private static void DisposeCondition(ref ITutorialConditionRuntime runtime)
        {
            if (runtime == null)
                return;

            runtime.Dispose();
            runtime = null;
        }

        private void TickCondition(ITutorialConditionRuntime runtime, float deltaTime)
        {
            if (runtime == null)
                return;

            try
            {
                runtime.Tick(deltaTime);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, activeAuthoring);
                Stop();
            }
        }

        private object CaptureConditionState(ITutorialConditionRuntime runtime)
        {
            try
            {
                return runtime.CaptureState();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, activeAuthoring);
                return null;
            }
        }

        private static void LogValidationErrors(TutorialStageAuthoringMB authoring, TutorialValidationContext validation)
        {
            for (int i = 0; i < validation.Errors.Count; i++)
                Debug.LogError($"{nameof(TutorialRuntimeService)}: {validation.Errors[i]}", authoring);
        }
    }
}
