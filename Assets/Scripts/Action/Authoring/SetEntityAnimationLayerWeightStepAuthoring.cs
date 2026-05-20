using System;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetEntityAnimationLayerWeightStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private EntityTargetReference target = EntityTargetReference.Self();
        [SerializeField] private string layerName;
        [SerializeField, Range(0f, 1f)] private float weight = 1f;
        [SerializeField, Min(0f)] private float duration;

        public override void Validate(ActionValidationContext context)
        {
            context.ValidateEntityTarget(target);

            if (string.IsNullOrWhiteSpace(layerName))
                context.AddError("Animator layer name is not assigned.");

            if (float.IsNaN(weight))
                context.AddError("Animator layer weight is not a valid number.");

            if (float.IsNaN(duration) || duration < 0f)
                context.AddError("Animator layer weight duration must be zero or greater.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new SetEntityAnimationLayerWeightStepRuntime(
                target,
                layerName,
                weight,
                duration));
        }
    }
}