using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.ActionSystem
{
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
    }
    [Serializable]
    public abstract class ActionStepAuthoring
    {
        public string DisplayName;

        public abstract void Validate(ActionValidationContext context);

        public abstract void Compile(ActionCompileContext context);
    }
}