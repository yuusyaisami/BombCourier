using System;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SubActionStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private InlineAction action;

        public override void Validate(ActionValidationContext context)
        {
            if (action == null)
            {
                context.AddError("SubAction is missing.");
                return;
            }

            action.Validate(context);
        }

        public override void Compile(ActionCompileContext context)
        {
            if (action == null)
                return;

            context.AddNode(new SubActionStepRuntime(action.CompileBlock()));
        }
    }
}
