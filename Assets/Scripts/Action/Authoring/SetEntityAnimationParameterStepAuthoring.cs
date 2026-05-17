using System;
using BC.Animation;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetEntityAnimationParameterStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private EntityTargetReference target = EntityTargetReference.Self();
        [SerializeField] private EntityAnimatorParameterWriteMode writeMode;
        [SerializeField] private string parameterName;
        [SerializeField] private bool boolValue;
        [SerializeField] private float floatValue;
        [SerializeField] private int intValue;

        public override void Validate(ActionValidationContext context)
        {
            context.ValidateEntityTarget(target);

            if (string.IsNullOrWhiteSpace(parameterName))
                context.AddError("Animator parameter name is not assigned.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new SetEntityAnimationParameterStepRuntime(
                target,
                writeMode,
                parameterName,
                boolValue,
                floatValue,
                intValue));
        }
    }
}