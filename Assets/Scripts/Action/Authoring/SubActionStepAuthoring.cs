using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SubActionStepAuthoring : ActionStepAuthoring
    {
        private const string ActionSlotId = "sub.action";

        [SerializeField] private InlineAction action;

        public override IReadOnlyList<ActionChildSlotDescriptor> GetChildActionSlots()
        {
            return new[]
            {
                new ActionChildSlotDescriptor(
                    ActionSlotId,
                    "Sub Action",
                    0,
                    action,
                    action != null,
                    "Sub",
                    "action"),
            };
        }

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
