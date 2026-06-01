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
            talkRequestData.EnsureInlineActionFlagsInitialized();

            List<ActionChildSlotDescriptor> slots = null;

            if (talkRequestData.useOnStartTalkAction)
            {
                slots ??= new List<ActionChildSlotDescriptor>();
                slots.Add(new ActionChildSlotDescriptor(
                    StartSlotId,
                    "Start Talk",
                    0,
                    talkRequestData.OnStartTalkAction,
                    talkRequestData.OnStartTalkAction != null,
                    "Start",
                    "talkRequestData.onStartTalkAction"));
            }

            if (talkRequestData.useOnCompleteTalkAction)
            {
                slots ??= new List<ActionChildSlotDescriptor>();
                slots.Add(new ActionChildSlotDescriptor(
                    CompleteSlotId,
                    "Complete Talk",
                    1,
                    talkRequestData.OnCompleteTalkAction,
                    talkRequestData.OnCompleteTalkAction != null,
                    "Complete",
                    "talkRequestData.onCompleteTalkAction"));
            }

            return slots ?? (IReadOnlyList<ActionChildSlotDescriptor>)Array.Empty<ActionChildSlotDescriptor>();
        }

        public override void Validate(ActionValidationContext context)
        {
            if (talkRequestData.talkStateId == TalkStateId.None)
                context.AddError("Talk state is required.");
        }

        public override void Compile(ActionCompileContext context)
        {
            talkRequestData.EnsureInlineActionFlagsInitialized();
            context.AddStep(new ShowTalkStepRuntime(talkRequestData));
        }
    }

    [Serializable]
    public sealed class ShowDialogueStepAuthoring : ActionStepAuthoring
    {
        private const string StartSlotId = "dialogue.start";
        private const string CompleteSlotId = "dialogue.complete";

        [SerializeField] private DialogueRequestData dialogueRequestData;

        public override IReadOnlyList<ActionChildSlotDescriptor> GetChildActionSlots()
        {
            dialogueRequestData.EnsureInlineActionFlagsInitialized();

            List<ActionChildSlotDescriptor> slots = null;

            if (dialogueRequestData.useOnStartDialogueAction)
            {
                slots ??= new List<ActionChildSlotDescriptor>();
                slots.Add(new ActionChildSlotDescriptor(
                    StartSlotId,
                    "Start Dialogue",
                    0,
                    dialogueRequestData.OnStartDialogueAction,
                    dialogueRequestData.OnStartDialogueAction != null,
                    "Start",
                    "dialogueRequestData.onStartDialogueAction"));
            }

            if (dialogueRequestData.useOnCompleteDialogueAction)
            {
                slots ??= new List<ActionChildSlotDescriptor>();
                slots.Add(new ActionChildSlotDescriptor(
                    CompleteSlotId,
                    "Complete Dialogue",
                    1,
                    dialogueRequestData.OnCompleteDialogueAction,
                    dialogueRequestData.OnCompleteDialogueAction != null,
                    "Complete",
                    "dialogueRequestData.onCompleteDialogueAction"));
            }

            return slots ?? (IReadOnlyList<ActionChildSlotDescriptor>)Array.Empty<ActionChildSlotDescriptor>();
        }

        public override void Validate(ActionValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(dialogueRequestData.dialogueText))
                context.AddError("Dialogue text is required.");
        }

        public override void Compile(ActionCompileContext context)
        {
            dialogueRequestData.EnsureInlineActionFlagsInitialized();
            context.AddStep(new ShowDialogueStepRuntime(dialogueRequestData));
        }
    }
}
