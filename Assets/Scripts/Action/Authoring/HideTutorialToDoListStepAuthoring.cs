using System;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class HideTutorialToDoListStepAuthoring : ActionStepAuthoring
    {
        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new HideTutorialToDoListStepRuntime());
        }
    }
}
