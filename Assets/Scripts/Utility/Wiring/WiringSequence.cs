using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Base
{
    public enum WiringSequencePlayMode
    {
        Once = 0,
        Loop = 1,
        PingPong = 2,
    }

    [Serializable]
    public sealed class WiringSequenceStep
    {
        [LabelText("Enter Actions")]
        [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
        [SerializeField]
        private WiringAction[] onEnterActions = Array.Empty<WiringAction>();

        [LabelText("Exit Actions")]
        [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
        [SerializeField]
        private WiringAction[] onExitActions = Array.Empty<WiringAction>();

        public int ExecuteEnter(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(onEnterActions, context);
        }

        public int ExecuteExit(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(onExitActions, context);
        }
    }

    [Serializable]
    public sealed class WiringSequenceDefinition
    {
        [ShowIf(nameof(HasMultipleSteps))]
        [SerializeField]
        private WiringSequencePlayMode playMode = WiringSequencePlayMode.Once;

        [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
        [SerializeField]
        private WiringSequenceStep[] steps = Array.Empty<WiringSequenceStep>();

        public WiringSequencePlayMode PlayMode => playMode;
        public int StepCount => steps != null ? steps.Length : 0;

        private bool HasMultipleSteps => StepCount > 1;

        public bool TryGetStep(int index, out WiringSequenceStep step)
        {
            if (steps == null || index < 0 || index >= steps.Length)
            {
                step = null;
                return false;
            }

            step = steps[index];
            return step != null;
        }
    }

    public sealed class WiringSequenceRuntime
    {
        private readonly WiringSequenceDefinition definition;
        private int nextIndex;
        private int activeIndex = -1;
        private int direction = 1;

        public int NextIndex => nextIndex;
        public int ActiveIndex => activeIndex;

        public WiringSequenceRuntime(WiringSequenceDefinition definition)
        {
            this.definition = definition;
        }

        public bool TryEnterNext(in WiringActionContext context, out int enteredIndex)
        {
            enteredIndex = -1;

            if (definition == null || definition.StepCount == 0)
                return false;

            enteredIndex = Mathf.Clamp(nextIndex, 0, definition.StepCount - 1);
            activeIndex = enteredIndex;

            if (definition.TryGetStep(enteredIndex, out WiringSequenceStep step))
                step.ExecuteEnter(context);

            AdvanceNextIndex();
            return true;
        }

        public bool TryExitActive(in WiringActionContext context)
        {
            if (definition == null || activeIndex < 0)
                return false;

            int index = activeIndex;
            activeIndex = -1;

            if (!definition.TryGetStep(index, out WiringSequenceStep step))
                return false;

            step.ExecuteExit(context);
            return true;
        }

        public void Reset()
        {
            nextIndex = 0;
            activeIndex = -1;
            direction = 1;
        }

        private void AdvanceNextIndex()
        {
            int count = definition.StepCount;

            if (count <= 1)
            {
                nextIndex = 0;
                direction = 1;
                return;
            }

            switch (definition.PlayMode)
            {
                case WiringSequencePlayMode.Loop:
                    nextIndex = (nextIndex + 1) % count;
                    break;

                case WiringSequencePlayMode.PingPong:
                    nextIndex += direction;

                    if (nextIndex >= count)
                    {
                        direction = -1;
                        nextIndex = count - 2;
                    }
                    else if (nextIndex < 0)
                    {
                        direction = 1;
                        nextIndex = 1;
                    }
                    break;

                default:
                    nextIndex = Mathf.Min(nextIndex + 1, count - 1);
                    break;
            }
        }
    }
}