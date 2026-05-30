using System;
using UnityEngine;

namespace BC.ActionSystem
{
    // AudioDataSO を指定して SE を再生する Action ステップ。
    // 発火後は即 Continue を返す (fire-and-forget)。
    // SE が鳴り終わるまで Action をブロックしたい場合は WaitFrames を後続に置く。
    [Serializable]
    public sealed class PlaySEStepRuntime : IActionNodeDefinition
    {
        private readonly BC.Audio.AudioDataSO audioData;
        private readonly int loopCount;

        public PlaySEStepRuntime(BC.Audio.AudioDataSO audioData, int loopCount)
        {
            this.audioData = audioData;
            this.loopCount = loopCount;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(audioData, loopCount);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly BC.Audio.AudioDataSO audioData;
            private readonly int loopCount;
            private bool dispatched;

            public Runtime(BC.Audio.AudioDataSO audioData, int loopCount)
            {
                this.audioData = audioData;
                this.loopCount = loopCount;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (dispatched)
                    return ActionNodeStatus.Continue;

                dispatched = true;

                if (audioData == null || audioData.Clip == null)
                {
                    Debug.LogWarning($"{nameof(PlaySEStepRuntime)}: AudioData またはそのクリップが null です。ステップをスキップします。");
                    return ActionNodeStatus.Continue;
                }

                BC.Audio.AudioSystemMB system = BC.Audio.AudioSystemMB.Instance;
                if (system == null)
                {
                    Debug.LogWarning($"{nameof(PlaySEStepRuntime)}: {nameof(BC.Audio.AudioSystemMB)} が存在しません。ステップをスキップします。");
                    return ActionNodeStatus.Continue;
                }

                system.PlaySE(audioData, loopCount);
                return ActionNodeStatus.Continue;
            }
        }
    }
}
