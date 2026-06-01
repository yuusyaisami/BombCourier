using System;
using BC.Camera;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class RegisterOverlayCameraStepRuntime : IActionNodeDefinition
    {
        private readonly string cameraTag;
        private readonly CinemachineCamera camera;
        private readonly bool activateImmediately;

        public RegisterOverlayCameraStepRuntime(string cameraTag, CinemachineCamera camera, bool activateImmediately)
        {
            this.cameraTag = cameraTag;
            this.camera = camera;
            this.activateImmediately = activateImmediately;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(cameraTag, camera, activateImmediately);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly string cameraTag;
            private readonly CinemachineCamera camera;
            private readonly bool activateImmediately;

            public Runtime(string cameraTag, CinemachineCamera camera, bool activateImmediately)
            {
                this.cameraTag = cameraTag;
                this.camera = camera;
                this.activateImmediately = activateImmediately;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel?.Cameras == null)
                {
                    Debug.LogWarning($"{nameof(RegisterOverlayCameraStepRuntime)}: {nameof(SceneCameraService)} is not available.");
                    return ActionNodeStatus.Failed;
                }

                if (camera == null)
                {
                    Debug.LogWarning($"{nameof(RegisterOverlayCameraStepRuntime)}: camera is not assigned.");
                    return ActionNodeStatus.Failed;
                }

                if (string.IsNullOrWhiteSpace(cameraTag))
                {
                    Debug.LogWarning($"{nameof(RegisterOverlayCameraStepRuntime)}: camera tag is empty.");
                    return ActionNodeStatus.Failed;
                }

                context.SceneKernel.Cameras.RegisterOverlayCamera(cameraTag, camera, activateImmediately);
                return ActionNodeStatus.Continue;
            }
        }
    }
}
