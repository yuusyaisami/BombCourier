using System;
using BC.Camera;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class DisableOverlayCameraStepRuntime : IActionNodeDefinition
    {
        private readonly string cameraTag;

        public DisableOverlayCameraStepRuntime(string cameraTag)
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
                    Debug.LogWarning($"{nameof(DisableOverlayCameraStepRuntime)}: {nameof(SceneCameraService)} is not available.");
                    return ActionNodeStatus.Failed;
                }

                if (string.IsNullOrWhiteSpace(cameraTag))
                {
                    Debug.LogWarning($"{nameof(DisableOverlayCameraStepRuntime)}: camera tag is empty.");
                    return ActionNodeStatus.Failed;
                }

                context.SceneKernel.Cameras.DisableOverlayCamera(cameraTag);
                return ActionNodeStatus.Continue;
            }
        }
    }
}
