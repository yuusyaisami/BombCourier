using System;
using System.Threading;
using BC.Base;
using BC.Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    public readonly struct TalkChoiceOptionDefinition
    {
        public readonly string DisplayText;
        public readonly ActionBlockDefinition OutcomeBlock;

        public TalkChoiceOptionDefinition(string displayText, ActionBlockDefinition outcomeBlock)
        {
            DisplayText = displayText ?? string.Empty;
            OutcomeBlock = outcomeBlock;
        }
    }

    [Serializable]
    public sealed class ShowTalkChoiceStepRuntime : IActionNodeDefinition
    {
        private readonly TalkChoiceRequestData requestData;
        private readonly TalkChoiceOptionDefinition[] options;

        public ShowTalkChoiceStepRuntime(
            TalkChoiceRequestData requestData,
            TalkChoiceOptionDefinition[] options)
        {
            this.requestData = requestData;
            this.options = options ?? Array.Empty<TalkChoiceOptionDefinition>();
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(requestData, options);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly TalkChoiceRequestData requestData;
            private readonly TalkChoiceOptionDefinition[] options;

            private CancellationTokenSource cancellationTokenSource;
            private TalkChoiceSelectionResult selectionResult;
            private IActionNodeRuntime outcomeRuntime;
            private bool started;
            private bool selectionCompleted;
            private bool outcomePrepared;
            private bool failed;

            public Runtime(
                TalkChoiceRequestData requestData,
                TalkChoiceOptionDefinition[] options)
            {
                this.requestData = requestData;
                this.options = options;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (failed)
                    return ActionNodeStatus.Failed;

                if (!started)
                {
                    if (!BeginSelection())
                    {
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }
                }

                if (!selectionCompleted)
                    return ActionNodeStatus.Running;

                if (!PrepareOutcome(context))
                    return ActionNodeStatus.Failed;

                if (outcomeRuntime == null)
                    return ActionNodeStatus.Continue;

                ActionNodeStatus status = outcomeRuntime.Tick(context, ref remainingOperations);
                if (status == ActionNodeStatus.Failed)
                    failed = true;

                return status;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                outcomeRuntime?.Cancel(context);
            }

            private bool BeginSelection()
            {
                started = true;

                TalkSystemManagerMB talkSystemManager = TalkSystemManagerMB.Instance;
                if (talkSystemManager == null)
                {
                    Debug.LogWarning($"{nameof(ShowTalkChoiceStepRuntime)}: {nameof(TalkSystemManagerMB)} is not available.");
                    return false;
                }

                cancellationTokenSource = new CancellationTokenSource();
                RunSelectionAsync(talkSystemManager, cancellationTokenSource.Token).Forget();
                return true;
            }

            private async UniTaskVoid RunSelectionAsync(
                TalkSystemManagerMB talkSystemManager,
                CancellationToken cancellationToken)
            {
                try
                {
                    selectionResult = await talkSystemManager.ShowChoicesAsync(requestData, cancellationToken);
                    selectionCompleted = selectionResult.HasSelection;
                    failed = !selectionCompleted;
                }
                catch (OperationCanceledException)
                {
                    selectionCompleted = false;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    failed = true;
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
            }

            private bool PrepareOutcome(in ActionExecutionContext context)
            {
                if (outcomePrepared)
                    return true;

                outcomePrepared = true;

                if (context.LocalValueStore == null)
                {
                    Debug.LogWarning($"{nameof(ShowTalkChoiceStepRuntime)}: LocalValueStore is not available.");
                    return false;
                }

                context.LocalValueStore.Set(ValueKeys.Local.Choice.HasSelection, true);
                context.LocalValueStore.Set(ValueKeys.Local.Choice.SelectedIndex, selectionResult.SelectedIndex);
                context.LocalValueStore.Set(ValueKeys.Local.Choice.SelectedText, selectionResult.SelectedText);

                if (selectionResult.SelectedIndex < 0 || selectionResult.SelectedIndex >= options.Length)
                {
                    Debug.LogWarning($"{nameof(ShowTalkChoiceStepRuntime)}: selection index {selectionResult.SelectedIndex} is out of range.");
                    return false;
                }

                ActionBlockDefinition outcomeBlock = options[selectionResult.SelectedIndex].OutcomeBlock;
                if (outcomeBlock == null || outcomeBlock.Count == 0)
                    return true;

                outcomeRuntime = outcomeBlock.CreateRuntime();
                return true;
            }
        }
    }
}