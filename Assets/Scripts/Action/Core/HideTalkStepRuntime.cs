using System;
using System.Threading;
using BC.Base;
using BC.Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class HideTalkStepRuntime : IActionNodeDefinition
    {
        private readonly HideTalkRequestData requestData;

        public HideTalkStepRuntime(HideTalkRequestData requestData)
        {
            this.requestData = requestData;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(requestData);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly HideTalkRequestData requestData;

            private CancellationTokenSource cancellationTokenSource;
            private bool started;
            private bool completed;
            private bool failed;

            public Runtime(HideTalkRequestData requestData)
            {
                this.requestData = requestData;
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
                        Debug.LogWarning($"{nameof(HideTalkStepRuntime)}: actor entity is invalid.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    TalkSystemManagerMB talkSystemManager = TalkSystemManagerMB.Instance;
                    if (talkSystemManager == null)
                    {
                        Debug.LogWarning($"{nameof(HideTalkStepRuntime)}: {nameof(TalkSystemManagerMB)} is not available.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    cancellationTokenSource = new CancellationTokenSource();
                    RunAsync(talkSystemManager, context.ActorEntity, cancellationTokenSource.Token).Forget();
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
                CancellationToken cancellationToken)
            {
                try
                {
                    completed = await talkSystemManager.HideTalk(actor, requestData).AttachExternalCancellation(cancellationToken);
                    failed = !completed;
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
