using System;
using UnityEngine;

namespace BC.ActionSystem
{
    // SE を AudioDataSO で指定して再生する Action ステップの Authoring。
    // loopCount: 1 = 単発, 2+ = 指定回数ループ, -1 = 無制限ループ
    [Serializable]
    public sealed class PlaySEStepAuthoring : ActionStepAuthoring
    {
        [SerializeField] private BC.Audio.AudioDataSO audioData;

        [Tooltip("1 = 単発, 2+ = 指定回数ループ, -1 = 無制限ループ")]
        [SerializeField, Min(-1)] private int loopCount = 1;

        public override void Validate(ActionValidationContext context)
        {
            if (audioData == null)
                context.AddError($"{nameof(PlaySEStepAuthoring)}: AudioData が未設定です。");
            else if (audioData.Clip == null)
                context.AddError($"{nameof(PlaySEStepAuthoring)}: AudioData にクリップが設定されていません。");
        }

        public override void Compile(ActionCompileContext context)
        {
            context.AddStep(new PlaySEStepRuntime(audioData, loopCount));
        }
    }
}
