using System;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetSceneCameraStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private string channel = "ActionCamera";
        [SerializeField] private CinemachineCamera camera;

        public override void Validate(ActionValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(channel))
                context.AddError("Camera request channel is not assigned.");

            if (camera == null)
                context.AddError("Camera is not assigned.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new SetSceneCameraStepRuntime(channel, camera));
        }
    }
}