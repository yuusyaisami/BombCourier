using System;
using System.Collections.Generic;
using BC.Managers;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ShowTalkStepAuthoring : ActionStepAuthoring
    {
        private const string StartSlotId = "talk.start";
        private const string CompleteSlotId = "talk.complete";

        [SerializeField] private TalkRequestData talkRequestData;

        public override IReadOnlyList<ActionChildSlotDescriptor> GetChildActionSlots()
        {
            return new[]
            {
                new ActionChildSlotDescriptor(
                    StartSlotId,
                    "Start Talk",
                    0,
                    talkRequestData.onStartTalkAction,
                    talkRequestData.onStartTalkAction != null,
                    "Start",
                    "talkRequestData.onStartTalkAction"),
                new ActionChildSlotDescriptor(
                    CompleteSlotId,
                    "Complete Talk",
                    1,
                    talkRequestData.onCompleteTalkAction,
                    talkRequestData.onCompleteTalkAction != null,
                    "Complete",
                    "talkRequestData.onCompleteTalkAction"),
            };
        }

        public override void Validate(ActionValidationContext context)
        {
            if (talkRequestData.talkStateId == TalkStateId.None)
                context.AddError("Talk state is required.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ShowTalkStepRuntime(talkRequestData));
        }
    }
}
