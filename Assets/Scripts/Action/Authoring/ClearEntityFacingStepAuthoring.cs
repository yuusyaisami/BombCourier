using System;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ClearEntityFacingStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private EntityTargetReference target = EntityTargetReference.Self();
        [SerializeField] private string channel = EntityFacingChannels.Action;

        public override void Validate(ActionValidationContext context)
        {
            context.ValidateEntityTarget(target);

            if (string.IsNullOrWhiteSpace(channel))
                context.AddError("Facing channel is not assigned.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ClearEntityFacingStepRuntime(target, channel));
        }
    }
}