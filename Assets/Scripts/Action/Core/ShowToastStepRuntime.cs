using System;
using BC.Managers;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ShowToastStepRuntime : IActionNodeDefinition
    {
        private readonly ToastRequestData toastRequestData;

        public ShowToastStepRuntime(ToastRequestData toastRequestData)
        {
            this.toastRequestData = toastRequestData;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(toastRequestData);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly ToastRequestData toastRequestData;
            private bool completed;
            private bool failed;

            public Runtime(ToastRequestData toastRequestData)
            {
                this.toastRequestData = toastRequestData;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (failed)
                    return ActionNodeStatus.Failed;

                if (completed)
                    return ActionNodeStatus.Continue;

                ToastSystemManagerMB toastSystemManager = ToastSystemManagerMB.Instance;
                if (toastSystemManager == null)
                {
                    Debug.LogWarning($"{nameof(ShowToastStepRuntime)}: {nameof(ToastSystemManagerMB)} is not available.");
                    failed = true;
                    return ActionNodeStatus.Failed;
                }

                toastSystemManager.ShowToast(toastRequestData);
                completed = true;
                return ActionNodeStatus.Continue;
            }

            public void Cancel(in ActionExecutionContext context)
            {
            }
        }
    }

    [Serializable]
    public sealed class ShowScreenOverlayStepRuntime : IActionNodeDefinition
    {
        private readonly ScreenOverlayShowRequestData screenOverlayShowRequestData;

        public ShowScreenOverlayStepRuntime(ScreenOverlayShowRequestData screenOverlayShowRequestData)
        {
            this.screenOverlayShowRequestData = screenOverlayShowRequestData;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(screenOverlayShowRequestData);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly ScreenOverlayShowRequestData screenOverlayShowRequestData;
            private bool completed;
            private bool failed;

            public Runtime(ScreenOverlayShowRequestData screenOverlayShowRequestData)
            {
                this.screenOverlayShowRequestData = screenOverlayShowRequestData;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (failed)
                    return ActionNodeStatus.Failed;

                if (completed)
                    return ActionNodeStatus.Continue;

                ScreenOverlaySystemManagerMB screenOverlaySystemManager = ScreenOverlaySystemManagerMB.Instance;
                if (screenOverlaySystemManager == null)
                {
                    Debug.LogWarning($"{nameof(ShowScreenOverlayStepRuntime)}: {nameof(ScreenOverlaySystemManagerMB)} is not available.");
                    failed = true;
                    return ActionNodeStatus.Failed;
                }

                if (!screenOverlaySystemManager.ShowOverlay(screenOverlayShowRequestData))
                {
                    Debug.LogWarning($"{nameof(ShowScreenOverlayStepRuntime)}: screen overlay show failed for '{screenOverlayShowRequestData.displayId}'.");
                    failed = true;
                    return ActionNodeStatus.Failed;
                }

                completed = true;
                return ActionNodeStatus.Continue;
            }

            public void Cancel(in ActionExecutionContext context)
            {
            }
        }
    }

    [Serializable]
    public sealed class HideScreenOverlayStepRuntime : IActionNodeDefinition
    {
        private readonly ScreenOverlayHideRequestData screenOverlayHideRequestData;

        public HideScreenOverlayStepRuntime(ScreenOverlayHideRequestData screenOverlayHideRequestData)
        {
            this.screenOverlayHideRequestData = screenOverlayHideRequestData;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(screenOverlayHideRequestData);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly ScreenOverlayHideRequestData screenOverlayHideRequestData;
            private bool completed;
            private bool failed;

            public Runtime(ScreenOverlayHideRequestData screenOverlayHideRequestData)
            {
                this.screenOverlayHideRequestData = screenOverlayHideRequestData;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (failed)
                    return ActionNodeStatus.Failed;

                if (completed)
                    return ActionNodeStatus.Continue;

                ScreenOverlaySystemManagerMB screenOverlaySystemManager = ScreenOverlaySystemManagerMB.Instance;
                if (screenOverlaySystemManager == null)
                {
                    Debug.LogWarning($"{nameof(HideScreenOverlayStepRuntime)}: {nameof(ScreenOverlaySystemManagerMB)} is not available.");
                    failed = true;
                    return ActionNodeStatus.Failed;
                }

                if (!screenOverlaySystemManager.HideOverlay(screenOverlayHideRequestData))
                {
                    Debug.LogWarning($"{nameof(HideScreenOverlayStepRuntime)}: screen overlay hide failed for '{screenOverlayHideRequestData.displayId}'.");
                    failed = true;
                    return ActionNodeStatus.Failed;
                }

                completed = true;
                return ActionNodeStatus.Continue;
            }

            public void Cancel(in ActionExecutionContext context)
            {
            }
        }
    }
}
