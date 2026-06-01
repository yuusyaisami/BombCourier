using System;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class RegisterOverlayCameraStepAuthoring : ActionStepAuthoring
    {
        [Tooltip("このカメラを識別するタグ文字列。後から ActivateOverlayCamera / DisableOverlayCamera に渡す。")]
        [SerializeField] private string cameraTag;

        [Tooltip("登録するカメラ。")]
        [SerializeField] private CinemachineCamera camera;

        [Tooltip("true の場合、登録と同時にこのカメラを active にする。")]
        [SerializeField] private bool activateImmediately;

        public override void Validate(ActionValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(cameraTag))
                context.AddError("Camera tag must not be empty.");

            if (camera == null)
                context.AddError("Camera is not assigned.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new RegisterOverlayCameraStepRuntime(cameraTag, camera, activateImmediately));
        }
    }
}
