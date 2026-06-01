using System;
using System.Threading;
using BC.Base;
using BC.Camera;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.ActionSystem
{
    // PlayPathCameraStep はカメラパスアニメーションが完了するまで Running を返し続ける非同期ステップ。
    // ShowTalkStepRuntime と同じ started/completed/failed + CancellationTokenSource パターンを踏襲する。
    [Serializable]
    public sealed class PlayPathCameraStepRuntime : IActionNodeDefinition
    {
        private readonly CameraPathSequenceAuthoringMB sequenceSource;

        public PlayPathCameraStepRuntime(CameraPathSequenceAuthoringMB sequenceSource)
        {
            this.sequenceSource = sequenceSource;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(sequenceSource);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly CameraPathSequenceAuthoringMB sequenceSource;

            private CancellationTokenSource cancellationTokenSource;
            private bool started;
            private bool completed;
            private bool failed;

            public Runtime(CameraPathSequenceAuthoringMB sequenceSource)
            {
                this.sequenceSource = sequenceSource;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (failed)
                    return ActionNodeStatus.Failed;

                if (completed)
                    return ActionNodeStatus.Continue;

                if (!started)
                {
                    started = true;

                    if (sequenceSource == null)
                    {
                        Debug.LogWarning($"{nameof(PlayPathCameraStepRuntime)}: sequence source is not assigned.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    CameraManager manager = CameraManager.Instance;
                    if (manager == null)
                    {
                        Debug.LogWarning($"{nameof(PlayPathCameraStepRuntime)}: {nameof(CameraManager)}.{nameof(CameraManager.Instance)} is null.");
                        failed = true;
                        return ActionNodeStatus.Failed;
                    }

                    cancellationTokenSource = new CancellationTokenSource();
                    RunAsync(manager, context.ActorEntity, cancellationTokenSource.Token).Forget();
                }

                return ActionNodeStatus.Running;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                CancelPendingTask();
                CameraManager.Instance?.CancelPath();
            }

            private async UniTaskVoid RunAsync(CameraManager manager, EntityRef actor, CancellationToken cancellationToken)
            {
                try
                {
                    await manager.PlayPathAsync(sequenceSource, actor).AttachExternalCancellation(cancellationToken);
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
                    return;

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
