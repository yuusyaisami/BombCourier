using System;
using UnityEngine;

namespace BC.ActionSystem
{
    // 再生中のすべての SE を即座に停止する Action ステップ。
    [Serializable]
    public sealed class StopAllSEStepRuntime : IActionNodeDefinition
    {
        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime();
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private bool dispatched;

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (dispatched)
                    return ActionNodeStatus.Continue;

                dispatched = true;

                BC.Audio.AudioSystemMB system = BC.Audio.AudioSystemMB.Instance;
                if (system == null)
                {
                    Debug.LogWarning($"{nameof(StopAllSEStepRuntime)}: {nameof(BC.Audio.AudioSystemMB)} が存在しません。ステップをスキップします。");
                    return ActionNodeStatus.Continue;
                }

                system.StopAllSE();
                return ActionNodeStatus.Continue;
            }
        }
    }
}
