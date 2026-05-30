using System;
using UnityEngine;

namespace BC.ActionSystem
{
    // BGM をフェードアウトして停止する Action ステップ。
    // 発火後は即 Continue を返す (fire-and-forget)。
    [Serializable]
    public sealed class StopBGMStepRuntime : IActionNodeDefinition
    {
        private readonly float fadeDuration;

        public StopBGMStepRuntime(float fadeDuration)
        {
            this.fadeDuration = fadeDuration;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(fadeDuration);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly float fadeDuration;
            private bool dispatched;

            public Runtime(float fadeDuration)
            {
                this.fadeDuration = fadeDuration;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (dispatched)
                    return ActionNodeStatus.Continue;

                dispatched = true;

                BC.Audio.AudioSystemMB system = BC.Audio.AudioSystemMB.Instance;
                if (system == null)
                {
                    Debug.LogWarning($"{nameof(StopBGMStepRuntime)}: {nameof(BC.Audio.AudioSystemMB)} が存在しません。ステップをスキップします。");
                    return ActionNodeStatus.Continue;
                }

                system.StopBGM(fadeDuration);
                return ActionNodeStatus.Continue;
            }
        }
    }
}
