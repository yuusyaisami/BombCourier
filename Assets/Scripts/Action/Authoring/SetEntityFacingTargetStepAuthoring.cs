using System;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetEntityFacingTargetStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private EntityTargetReference target = EntityTargetReference.Self();
        [SerializeField] private EntityTargetReference faceTarget = EntityTargetReference.Trigger();
        [SerializeField] private string channel = EntityFacingChannels.Action;

        public override void Validate(ActionValidationContext context)
        {
            context.ValidateEntityTarget(target);
            context.ValidateEntityTarget(faceTarget);

            if (string.IsNullOrWhiteSpace(channel))
                context.AddError("Facing channel is not assigned.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new SetEntityFacingTargetStepRuntime(target, faceTarget, channel));
        }
    }
}