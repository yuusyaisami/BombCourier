using System;
using BC.Managers;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ShowToastStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private ToastRequestData toastRequestData;

        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ShowToastStepRuntime(toastRequestData));
        }
    }
}