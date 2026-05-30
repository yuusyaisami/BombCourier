using System;
using UnityEngine;

namespace BC.ActionSystem
{
    // 再生中の BGM をフェードアウトして停止する Action ステップの Authoring。
    [Serializable]
    public sealed class StopBGMStepAuthoring : ActionStepAuthoring
    {
        [Tooltip("フェードアウトにかける時間 (秒)。0 にすると即時停止。")]
        [SerializeField, Min(0f)] private float fadeDuration = 1f;

        public override void Validate(ActionValidationContext context)
        {
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new StopBGMStepRuntime(fadeDuration));
        }
    }
}
