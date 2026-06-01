using System;
using BC.Camera;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ActivateOverlayCameraStepRuntime : IActionNodeDefinition
    {
        private readonly string cameraTag;

        public ActivateOverlayCameraStepRuntime(string cameraTag)
        {
            this.cameraTag = cameraTag;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(cameraTag);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly string cameraTag;

            public Runtime(string cameraTag)
            {
                this.cameraTag = cameraTag;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel?.Cameras == null)
                {
                    Debug.LogWarning($"{nameof(ActivateOverlayCameraStepRuntime)}: {nameof(SceneCameraService)} is not available.");
                    return ActionNodeStatus.Failed;
                }

                if (string.IsNullOrWhiteSpace(cameraTag))
                {
                    Debug.LogWarning($"{nameof(ActivateOverlayCameraStepRuntime)}: camera tag is empty.");
                    return ActionNodeStatus.Failed;
                }

                context.SceneKernel.Cameras.ActivateOverlayCamera(cameraTag);
                return ActionNodeStatus.Continue;
            }
        }
    }
}
