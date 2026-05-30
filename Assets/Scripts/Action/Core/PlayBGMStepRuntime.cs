using System;
using UnityEngine;

namespace BC.ActionSystem
{
    // AudioDataSO を指定して BGM を再生 (クロスフェード) する Action ステップ。
    // 発火後は即 Continue を返す (fire-and-forget)。
    // フェード完了まで待ちたい場合は WaitFrames や WaitTime を後続に置く。
    [Serializable]
    public sealed class PlayBGMStepRuntime : IActionNodeDefinition
    {
        private readonly BC.Audio.AudioDataSO audioData;
        private readonly bool loop;
        private readonly float crossfadeDuration;

        public PlayBGMStepRuntime(BC.Audio.AudioDataSO audioData, bool loop, float crossfadeDuration)
        {
            this.audioData = audioData;
            this.loop = loop;
            this.crossfadeDuration = crossfadeDuration;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(audioData, loop, crossfadeDuration);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly BC.Audio.AudioDataSO audioData;
            private readonly bool loop;
            private readonly float crossfadeDuration;
            private bool dispatched;

            public Runtime(BC.Audio.AudioDataSO audioData, bool loop, float crossfadeDuration)
            {
                this.audioData = audioData;
                this.loop = loop;
                this.crossfadeDuration = crossfadeDuration;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (dispatched)
                    return ActionNodeStatus.Continue;

                dispatched = true;

                if (audioData == null || audioData.Clip == null)
                {
                    Debug.LogWarning($"{nameof(PlayBGMStepRuntime)}: AudioData またはそのクリップが null です。ステップをスキップします。");
                    return ActionNodeStatus.Continue;
                }

                BC.Audio.AudioSystemMB system = BC.Audio.AudioSystemMB.Instance;
                if (system == null)
                {
                    Debug.LogWarning($"{nameof(PlayBGMStepRuntime)}: {nameof(BC.Audio.AudioSystemMB)} が存在しません。ステップをスキップします。");
                    return ActionNodeStatus.Continue;
                }

                system.PlayBGM(audioData, loop, crossfadeDuration);
                return ActionNodeStatus.Continue;
            }
        }
    }
}
