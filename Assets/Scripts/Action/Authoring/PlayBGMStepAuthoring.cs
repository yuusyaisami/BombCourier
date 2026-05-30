using System;
using UnityEngine;

namespace BC.ActionSystem
{
    // BGM を AudioDataSO で指定して再生 (クロスフェード) する Action ステップの Authoring。
    [Serializable]
    public sealed class PlayBGMStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private BC.Audio.AudioDataSO audioData;

        [Tooltip("true = ループ再生。false = 1 回再生して停止。")]
        [SerializeField] private bool loop = true;

        [Tooltip("前の BGM とのクロスフェード時間 (秒)。0 にすると即時切り替え。")]
        [SerializeField, Min(0f)] private float crossfadeDuration = 1f;

        public override void Validate(ActionValidationContext context)
        {
            if (audioData == null)
                context.AddError($"{nameof(PlayBGMStepAuthoring)}: AudioData が未設定です。");
            else if (audioData.Clip == null)
                context.AddError($"{nameof(PlayBGMStepAuthoring)}: AudioData にクリップが設定されていません。");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new PlayBGMStepRuntime(audioData, loop, crossfadeDuration));
        }
    }
}
