using System;

namespace BC.ActionSystem
{
    // 再生中のすべての SE を即座に停止する Action ステップの Authoring。
    [Serializable]
    public sealed class StopAllSEStepAuthoring : ActionStepAuthoring
    {
        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new StopAllSEStepRuntime());
        }
    }
}
