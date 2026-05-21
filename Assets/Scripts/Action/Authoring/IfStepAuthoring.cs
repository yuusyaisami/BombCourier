using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class IfStepAuthoring : ActionStepAuthoring
    {
        private const string TrueSlotId = "if.true";
        private const string FalseSlotId = "if.false";

        [SerializeField] private ReactiveBool condition = ReactiveBool.LiteralValue(true);
        [SerializeField] private InlineAction whenTrue;
        [SerializeField] private InlineAction whenFalse;

        public override IReadOnlyList<ActionChildSlotDescriptor> GetChildActionSlots()
        {
            return new[]
            {
                new ActionChildSlotDescriptor(
                    TrueSlotId,
                    "True",
                    0,
                    whenTrue,
                    whenTrue != null,
                    "True",
                    "whenTrue"),
                new ActionChildSlotDescriptor(
                    FalseSlotId,
                    "False",
                    1,
                    whenFalse,
                    whenFalse != null,
                    "False",
                    "whenFalse"),
            };
        }

        public override void Validate(ActionValidationContext context)
        {
            if (whenTrue == null && whenFalse == null)
            {
                context.AddError("If step requires at least one inline action branch.");
                return;
            }

            ValidateBranch(whenTrue, "True", context);
            ValidateBranch(whenFalse, "False", context);
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddNode(new IfStepRuntime(
                condition,
                whenTrue?.CompileBlock(),
                whenFalse?.CompileBlock()));
        }

        private static void ValidateBranch(
            InlineAction branch,
            string branchLabel,
            ActionValidationContext context)
        {
            if (branch == null)
                return;

            ActionValidationContext branchContext = new();
            branch.Validate(branchContext);

            for (int index = 0; index < branchContext.Errors.Count; index++)
            {
                context.AddError($"If {branchLabel} branch: {branchContext.Errors[index]}");
            }
        }
    }
}
