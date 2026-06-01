using System;
using BC.Camera;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class PlayPathCameraStepAuthoring : ActionStepAuthoring
    {
        [Tooltip("再生するカメラパスシーケンス。")]
        [SerializeField] private CameraPathSequenceAuthoringMB sequenceSource;

        public override void Validate(ActionValidationContext context)
        {
            if (sequenceSource == null)
                context.AddError("Sequence source is not assigned.");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new PlayPathCameraStepRuntime(sequenceSource));
        }
    }
}
