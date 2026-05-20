using System;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ClearSceneCameraStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private string channel = "ActionCamera";

        public override void Validate(ActionValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(channel))
                context.AddError("Camera request channel is not assigned.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ClearSceneCameraStepRuntime(channel));
        }
    }
}