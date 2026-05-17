using System;
using System.Threading;
using BC.Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class HideTalkStepRuntime : IActionNodeDefinition
    {
        private readonly float duration;

        public HideTalkStepRuntime(float duration)
        {
            this.duration = Mathf.Max(0f, duration);
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(duration);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly float duration;

            private CancellationTokenSource cancellationTokenSource;
            private bool started;
            private bool completed;
            private bool failed;

            public Runtime(float duration)
            {
                this.duration = Mathf.Max(0f, duration);
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

                    TalkSystemManagerMB talkSystemManager = TalkSystemManagerMB.Instance;
                    if (talkSystemManager == null)
                    {
                        Debug.LogWarning($"{nameof(HideTalkStepRuntime)}: {nameof(TalkSystemManagerMB)} is not available.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    cancellationTokenSource = new CancellationTokenSource();
                    RunAsync(talkSystemManager, cancellationTokenSource.Token).Forget();
                }

                return ActionNodeStatus.Running;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                CancelPendingTask();
            }

            private async UniTaskVoid RunAsync(TalkSystemManagerMB talkSystemManager, CancellationToken cancellationToken)
            {
                try
                {
                    await talkSystemManager.HideTalk(duration).AttachExternalCancellation(cancellationToken);
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