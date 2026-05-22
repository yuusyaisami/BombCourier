using System;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ClearSceneCameraStepAuthoring : ActionStepAuthoring
    {
        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ClearSceneCameraStepRuntime());
        }
    }
}