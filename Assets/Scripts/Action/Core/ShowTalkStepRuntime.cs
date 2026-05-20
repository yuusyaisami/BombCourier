using System;
using System.Threading;
using BC.Base;
using BC.Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ShowTalkStepRuntime : IActionNodeDefinition
    {
        private readonly TalkRequestData talkRequestData;

        public ShowTalkStepRuntime(TalkRequestData talkRequestData)
        {
            this.talkRequestData = talkRequestData;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(talkRequestData);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly TalkRequestData talkRequestData;

            private CancellationTokenSource cancellationTokenSource;
            private bool started;
            private bool completed;
            private bool failed;

            public Runtime(TalkRequestData talkRequestData)
            {
                this.talkRequestData = talkRequestData;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (failed)
                {
                    return ActionNodeStatus.Failed;
                }

                if (completed)
                {
                    return ActionNodeStatus.Continue;
                }

                if (!started)
                {
                    started = true;

                    if (!context.ActorEntity.IsValid)
                    {
                        Debug.LogWarning($"{nameof(ShowTalkStepRuntime)}: actor entity is invalid.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    TalkSystemManagerMB talkSystemManager = TalkSystemManagerMB.Instance;
                    if (talkSystemManager == null)
                    {
                        Debug.LogWarning($"{nameof(ShowTalkStepRuntime)}: {nameof(TalkSystemManagerMB)} is not available.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    cancellationTokenSource = new CancellationTokenSource();
                    RunAsync(talkSystemManager, context.ActorEntity, context.TriggerEntity, cancellationTokenSource.Token).Forget();
                }

                return ActionNodeStatus.Running;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                CancelPendingTask();
            }

            private async UniTaskVoid RunAsync(
                TalkSystemManagerMB talkSystemManager,
                EntityRef actor,
                EntityRef viewer,
                CancellationToken cancellationToken)
            {
                try
                {
                    await talkSystemManager.ShowTalk(actor, viewer, talkRequestData).AttachExternalCancellation(cancellationToken);
                    completed = true;
                }
                catch (OperationCanceledException)
                {
                    completed = true;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    failed = true;
                }
                finally
                {
                    DisposeCancellationTokenSource();
                }
            }

            private void CancelPendingTask()
            {
                if (cancellationTokenSource == null)
                {
                    return;
                }

                cancellationTokenSource.Cancel();
                DisposeCancellationTokenSource();
            }

            private void DisposeCancellationTokenSource()
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }
    }
}