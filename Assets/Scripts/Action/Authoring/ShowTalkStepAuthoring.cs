using System;
using BC.Managers;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ShowTalkStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private TalkRequestData talkRequestData;

        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ShowTalkStepRuntime(talkRequestData));
        }
    }
}