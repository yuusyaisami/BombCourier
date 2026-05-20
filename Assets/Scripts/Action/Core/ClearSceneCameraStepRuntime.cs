using System;
using BC.Base;
using BC.Camera;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ClearSceneCameraStepRuntime : IActionNodeDefinition
    {
        private readonly string channel;

        public ClearSceneCameraStepRuntime(string channel)
        {
            this.channel = string.IsNullOrWhiteSpace(channel) ? "ActionCamera" : channel;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(channel);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly string channel;

            public Runtime(string channel)
            {
                this.channel = string.IsNullOrWhiteSpace(channel) ? "ActionCamera" : channel;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel?.Cameras == null)
                {
                    Debug.LogWarning($"{nameof(ClearSceneCameraStepRuntime)}: {nameof(SceneCameraService)} is not available.");
                    return ActionNodeStatus.Failed;
                }

                context.SceneKernel.Cameras.ClearActionCameraRequest(channel);
                return ActionNodeStatus.Continue;
            }
        }
    }
}