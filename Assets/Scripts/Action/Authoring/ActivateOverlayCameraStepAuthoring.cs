using System;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ActivateOverlayCameraStepAuthoring : ActionStepAuthoring
    {
        [Tooltip("アクティブにしたいカメラのタグ文字列。RegisterOverlayCamera で登録した tag と一致させる。")]
        [SerializeField] private string cameraTag;

        public override void Validate(ActionValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(cameraTag))
                context.AddError("Camera tag must not be empty.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new ActivateOverlayCameraStepRuntime(cameraTag));
        }
    }
}
