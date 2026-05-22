using System;
using BC.Managers;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class HideTalkStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private HideTalkRequestData requestData = HideTalkRequestData.Default;

        public override void Validate(ActionValidationContext context)
        {
            if (requestData.applyTalkStateOverride && requestData.talkStateId == TalkStateId.None)
                context.AddError("Hide talk state override is enabled but no talk state is selected.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new HideTalkStepRuntime(requestData));
        }
    }
}