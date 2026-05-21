using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.ActionSystem
{
    public readonly struct ActionChildSlotDescriptor
    {
        public ActionChildSlotDescriptor(
            string slotId,
            string label,
            int order,
            InlineAction action,
            bool isPresent,
            string metadataBadge,
            string serializedPropertyPath = null)
        {
            if (string.IsNullOrWhiteSpace(slotId))
                throw new ArgumentException("Child action slot id must not be empty.", nameof(slotId));

            SlotId = slotId;
            Label = string.IsNullOrWhiteSpace(label) ? slotId : label;
            Order = order;
            Action = action;
            IsPresent = isPresent;
            MetadataBadge = metadataBadge ?? string.Empty;
            SerializedPropertyPath = serializedPropertyPath ?? string.Empty;
        }

        public string SlotId { get; }
        public string Label { get; }
        public int Order { get; }
        public InlineAction Action { get; }
        public bool IsPresent { get; }
        public string MetadataBadge { get; }

        // Editor-only consumers use this to bind the detail pane without hard-coding each step type.
        public string SerializedPropertyPath { get; }
    }

    [Serializable]
    public sealed class InlineAction
    {
        [SerializeReference]
        private List<ActionStepAuthoring> _steps;

        public IReadOnlyList<ActionStepAuthoring> Steps
        {
            get
            {
                if (_steps != null)
                    return _steps;

                return Array.Empty<ActionStepAuthoring>();
            }
        }

        public void Validate(ActionValidationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_steps == null)
                return;

            for (int i = 0; i < _steps.Count; i++)
            {
                ActionStepAuthoring step = _steps[i];

                if (step == null)
                {
                    context.AddError($"Action step at index {i} is missing.");
                    continue;
                }

                step.Validate(context);
            }
        }

        public CompiledAction Compile()
        {
            ActionCompileContext context = new();

            if (_steps != null)
            {
                for (int i = 0; i < _steps.Count; i++)
                {
                    _steps[i]?.Compile(context);
                }
            }

            return context.Build();
        }

        public ActionBlockDefinition CompileBlock()
        {
            ActionCompileContext context = new();

            if (_steps != null)
            {
                for (int i = 0; i < _steps.Count; i++)
                {
                    _steps[i]?.Compile(context);
                }
            }

            return context.BuildBlock();
        }
    }
    [Serializable]
    public abstract class ActionStepAuthoring
    {
        private static readonly IReadOnlyList<ActionChildSlotDescriptor> EmptyChildSlots =
            Array.Empty<ActionChildSlotDescriptor>();

        public string DisplayName;

        public virtual IReadOnlyList<ActionChildSlotDescriptor> GetChildActionSlots()
        {
            return EmptyChildSlots;
        }

        public abstract void Validate(ActionValidationContext context);

        public abstract void Compile(ActionCompileContext context);
    }
}
