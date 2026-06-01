using System;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class DisableOverlayCameraStepAuthoring : ActionStepAuthoring
    {
        [Tooltip("非アクティブ化して登録解除するカメラのタグ文字列。")]
        [SerializeField] private string cameraTag;

        public override void Validate(ActionValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(cameraTag))
                context.AddError("Camera tag must not be empty.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new DisableOverlayCameraStepRuntime(cameraTag));
        }
    }
}
