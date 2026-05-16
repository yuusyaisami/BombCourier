using System;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class WaitFramesStepAuthoring : ActionStepAuthoring
    {
        [SerializeField, Min(0)] private int frames = 1;

        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddNode(new WaitFramesStepRuntime(frames));
        }
    }
}
