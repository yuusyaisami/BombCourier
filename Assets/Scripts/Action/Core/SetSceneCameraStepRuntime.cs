using System;
using BC.Base;
using BC.Camera;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetSceneCameraStepRuntime : IActionNodeDefinition
    {
        private readonly string channel;
        private readonly CinemachineCamera camera;

        public SetSceneCameraStepRuntime(string channel, CinemachineCamera camera)
        {
            this.channel = string.IsNullOrWhiteSpace(channel) ? "ActionCamera" : channel;
            this.camera = camera;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(channel, camera);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly string channel;
            private readonly CinemachineCamera camera;

            public Runtime(string channel, CinemachineCamera camera)
            {
                this.channel = string.IsNullOrWhiteSpace(channel) ? "ActionCamera" : channel;
                this.camera = camera;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel?.Cameras == null)
                {
                    Debug.LogWarning($"{nameof(SetSceneCameraStepRuntime)}: {nameof(SceneCameraService)} is not available.");
                    return ActionNodeStatus.Failed;
                }

                if (camera == null)
                {
                    Debug.LogWarning($"{nameof(SetSceneCameraStepRuntime)}: camera is not assigned.");
                    return ActionNodeStatus.Failed;
                }

                context.SceneKernel.Cameras.SetActionCameraRequest(channel, camera);
                return ActionNodeStatus.Continue;
            }
        }
    }
}