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
}