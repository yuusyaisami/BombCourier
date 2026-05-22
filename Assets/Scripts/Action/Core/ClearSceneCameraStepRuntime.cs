using System;
using BC.Base;
using BC.Camera;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ClearSceneCameraStepRuntime : IActionNodeDefinition
    {
        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime();
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel?.Cameras == null)
                {
                    Debug.LogWarning($"{nameof(ClearSceneCameraStepRuntime)}: {nameof(SceneCameraService)} is not available.");
                    return ActionNodeStatus.Failed;
                }

                context.SceneKernel.Cameras.ClearActionCamera(context.ExecutionHandle);
                return ActionNodeStatus.Continue;
            }
        }
    }
}