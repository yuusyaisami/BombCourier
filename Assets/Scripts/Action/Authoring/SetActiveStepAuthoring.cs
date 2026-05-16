using System;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetActiveStepAuthoring : ActionStepAuthoring
    {
        [SerializeField]
        private EntityTargetReference _target = EntityTargetReference.Self();

        [SerializeField]
        private bool _active;

        public override void Validate(ActionValidationContext context)
        {
            context.ValidateEntityTarget(_target);
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new SetActiveStepRuntime(_target, _active));
        }
    }
}