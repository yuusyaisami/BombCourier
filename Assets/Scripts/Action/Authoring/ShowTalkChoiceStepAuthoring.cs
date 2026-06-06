using System;
using System.Collections.Generic;
using BC.Managers;
using UnityEngine;
using UnityEngine.Localization;

namespace BC.ActionSystem
{
    public enum TalkChoiceOptionOutcomeKind
    {
        None = 0,
        InlineAction = 1,
        ValueStoreWrite = 2,
    }

    [Serializable]
    public sealed class TalkChoiceOptionAuthoring
    {
        [SerializeField, HideInInspector] private string stableId;
        [SerializeField] private LocalizedStringTable table;
        [SerializeField] private string entry;
        [SerializeField] private bool applySetTable;
        [SerializeField] private string displayText;
        [SerializeField] private TalkChoiceOptionOutcomeKind outcomeKind;
        [SerializeField] private InlineAction inlineAction;
        [SerializeField] private ValueStoreWriteAuthoring valueStoreWrite = new();

        public string StableId => stableId;
        public LocalizedStringTable Table => table;
        public string Entry => entry;
        public bool ApplySetTable => applySetTable;
        public string DisplayText => displayText;
        public TalkChoiceOptionOutcomeKind OutcomeKind => outcomeKind;
        public InlineAction InlineAction => inlineAction;
        public ValueStoreWriteAuthoring ValueStoreWrite => valueStoreWrite;

        internal string EnsureStableId()
        {
            if (string.IsNullOrWhiteSpace(stableId))
                stableId = Guid.NewGuid().ToString("N");

            return stableId;
        }

        internal void ResetStableId()
        {
            stableId = Guid.NewGuid().ToString("N");
        }
    }

    [Serializable]
    public sealed class ShowTalkChoiceStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private TalkChoiceOptionAuthoring[] options = Array.Empty<TalkChoiceOptionAuthoring>();
        [SerializeField] private int defaultSelectionIndex;
        [SerializeField] private bool wrapSelection = true;

        public override IReadOnlyList<ActionChildSlotDescriptor> GetChildActionSlots()
        {
            EnsureStableOptionIds();

            if (options == null || options.Length == 0)
                return Array.Empty<ActionChildSlotDescriptor>();

            List<ActionChildSlotDescriptor> slots = null;

            for (int i = 0; i < options.Length; i++)
            {
                TalkChoiceOptionAuthoring option = options[i];

                if (option == null || option.OutcomeKind != TalkChoiceOptionOutcomeKind.InlineAction)
                    continue;

                slots ??= new List<ActionChildSlotDescriptor>();
                string optionLabel = string.IsNullOrWhiteSpace(option.DisplayText)
                    ? $"Option {i + 1}"
                    : option.DisplayText;

                slots.Add(new ActionChildSlotDescriptor(
                    $"choice.{option.EnsureStableId()}",
                    optionLabel,
                    i,
                    option.InlineAction,
                    option.InlineAction != null,
                    $"#{i + 1}",
                    $"options.Array.data[{i}].inlineAction"));
            }

            return slots ?? (IReadOnlyList<ActionChildSlotDescriptor>)Array.Empty<ActionChildSlotDescriptor>();
        }

        public override void Validate(ActionValidationContext context)
        {
            EnsureStableOptionIds();

            if (options == null || options.Length == 0)
            {
                context.AddError("Talk choice requires at least one option.");
                return;
            }

            for (int i = 0; i < options.Length; i++)
            {
                TalkChoiceOptionAuthoring option = options[i];

                if (option == null)
                {
                    context.AddError($"Talk choice option at index {i} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(option.Entry) && string.IsNullOrWhiteSpace(option.DisplayText))
                    context.AddError($"Talk choice option {i} needs a localization Entry or fallback text.");

                ValidateOptionOutcome(option, i, context);
            }
        }

        public override void Compile(ActionCompileContext context)
        {
            EnsureStableOptionIds();

            TalkChoiceOptionDefinition[] compiledOptions = new TalkChoiceOptionDefinition[options.Length];
            TalkChoiceOptionRequestData[] requestOptions = new TalkChoiceOptionRequestData[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                TalkChoiceOptionAuthoring option = options[i];
                requestOptions[i] = new TalkChoiceOptionRequestData(
                    option != null ? option.Table : null,
                    option?.Entry ?? string.Empty,
                    option != null && option.ApplySetTable,
                    option?.DisplayText ?? string.Empty);
                compiledOptions[i] = new TalkChoiceOptionDefinition(
                    option?.DisplayText ?? string.Empty,
                    CompileOptionOutcome(option));
            }

            context.AddStep(new ShowTalkChoiceStepRuntime(
                new TalkChoiceRequestData(requestOptions, defaultSelectionIndex, wrapSelection),
                compiledOptions));
        }

        private static void ValidateOptionOutcome(
            TalkChoiceOptionAuthoring option,
            int optionIndex,
            ActionValidationContext context)
        {
            switch (option.OutcomeKind)
            {
                case TalkChoiceOptionOutcomeKind.None:
                    return;

                case TalkChoiceOptionOutcomeKind.InlineAction:
                    {
                        if (option.InlineAction == null)
                        {
                            context.AddError($"Talk choice option {optionIndex} inline action is missing.");
                            return;
                        }

                        ActionValidationContext optionContext = new();
                        option.InlineAction.Validate(optionContext);
                        PrefixErrors(optionContext, context, optionIndex);
                        return;
                    }

                case TalkChoiceOptionOutcomeKind.ValueStoreWrite:
                    {
                        if (option.ValueStoreWrite == null)
                        {
                            context.AddError($"Talk choice option {optionIndex} ValueStore write is missing.");
                            return;
                        }

                        ActionValidationContext optionContext = new();
                        ValueStoreWriteAuthoringUtility.Validate(option.ValueStoreWrite, optionContext);
                        PrefixErrors(optionContext, context, optionIndex);
                        return;
                    }

                default:
                    context.AddError($"Talk choice option {optionIndex} outcome kind is not supported.");
                    return;
            }
        }

        private static ActionBlockDefinition CompileOptionOutcome(TalkChoiceOptionAuthoring option)
        {
            if (option == null)
                return null;

            return option.OutcomeKind switch
            {
                TalkChoiceOptionOutcomeKind.InlineAction => option.InlineAction?.CompileBlock(),
                TalkChoiceOptionOutcomeKind.ValueStoreWrite => CompileValueStoreWrite(option.ValueStoreWrite),
                _ => null,
            };
        }

        private static ActionBlockDefinition CompileValueStoreWrite(ValueStoreWriteAuthoring write)
        {
            if (write == null)
                return null;

            ActionCompileContext compileContext = new();
            compileContext.AddStep(ValueStoreWriteAuthoringUtility.CreateRuntime(write));
            return compileContext.BuildBlock();
        }

        private void EnsureStableOptionIds()
        {
            if (options == null || options.Length == 0)
                return;

            HashSet<string> usedIds = new(StringComparer.Ordinal);

            for (int i = 0; i < options.Length; i++)
            {
                TalkChoiceOptionAuthoring option = options[i];

                if (option == null)
                    continue;

                string stableId = option.EnsureStableId();

                if (usedIds.Add(stableId))
                    continue;

                option.ResetStableId();
                usedIds.Add(option.StableId);
            }
        }

        private static void PrefixErrors(
            ActionValidationContext source,
            ActionValidationContext destination,
            int optionIndex)
        {
            for (int i = 0; i < source.Errors.Count; i++)
            {
                destination.AddError($"Talk choice option {optionIndex}: {source.Errors[i]}");
            }
        }
    }
}
