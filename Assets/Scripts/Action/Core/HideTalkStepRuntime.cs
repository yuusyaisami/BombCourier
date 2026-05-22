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

                    if (context.SceneKernel == null || context.SceneKernel.EntityComponents == null)
                    {
                        Debug.LogWarning($"{nameof(HideTalkStepRuntime)}: scene kernel entity components are not available.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    if (!context.SceneKernel.EntityComponents.TryResolve(context.ActorEntity, out TalkAdapterMB talkAdapter))
                    {
                        Debug.LogWarning($"{nameof(HideTalkStepRuntime)}: {nameof(TalkAdapterMB)} was not found on {context.ActorEntity}.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    cancellationTokenSource = new CancellationTokenSource();
                    RunAsync(talkAdapter, cancellationTokenSource.Token).Forget();
                }

                return ActionNodeStatus.Running;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                CancelPendingTask();
            }

            private async UniTaskVoid RunAsync(
                TalkAdapterMB talkAdapter,
                CancellationToken cancellationToken)
            {
                try
                {
                    completed = await talkAdapter.TryHideTalkAsync(requestData, cancellationToken);
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