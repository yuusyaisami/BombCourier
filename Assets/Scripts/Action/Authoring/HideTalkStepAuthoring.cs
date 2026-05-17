using System;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class HideTalkStepAuthoring : ActionStepAuthoring
    {
        [SerializeField, Min(0f)] private float duration = 0.3f;

        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new HideTalkStepRuntime(duration));
        }
    }
}