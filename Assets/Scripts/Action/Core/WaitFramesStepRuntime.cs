using System;

namespace BC.ActionSystem
{
    public sealed class WaitFramesStepRuntime : IActionNodeDefinition
    {
        private readonly int frames;

        public WaitFramesStepRuntime(int frames)
        {
            this.frames = Math.Max(0, frames);
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(frames);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private int remainingFrames;

            public Runtime(int frames)
            {
                remainingFrames = frames;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (remainingFrames <= 0)
                    return ActionNodeStatus.Continue;

                remainingFrames--;
                return ActionNodeStatus.Running;
            }
        }
    }
}
