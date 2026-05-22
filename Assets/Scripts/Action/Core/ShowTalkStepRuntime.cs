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

                    if (context.SceneKernel == null || context.SceneKernel.EntityComponents == null)
                    {
                        Debug.LogWarning($"{nameof(ShowTalkStepRuntime)}: scene kernel entity components are not available.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    if (!context.SceneKernel.EntityComponents.TryResolve(context.ActorEntity, out TalkAdapterMB talkAdapter))
                    {
                        Debug.LogWarning($"{nameof(ShowTalkStepRuntime)}: {nameof(TalkAdapterMB)} was not found on {context.ActorEntity}.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    cancellationTokenSource = new CancellationTokenSource();
                    RunAsync(talkAdapter, context.TriggerEntity, cancellationTokenSource.Token).Forget();
                }

                return ActionNodeStatus.Running;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                CancelPendingTask();
            }

            private async UniTaskVoid RunAsync(
                TalkAdapterMB talkAdapter,
                EntityRef viewer,
                CancellationToken cancellationToken)
            {
                try
                {
                    completed = await talkAdapter.TryShowTalkAsync(viewer, talkRequestData, cancellationToken);
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