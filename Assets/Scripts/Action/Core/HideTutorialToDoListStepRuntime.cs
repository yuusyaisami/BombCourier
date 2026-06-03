using System;
using BC.Managers;
using BC.UI;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class HideTutorialToDoListStepRuntime : IActionNodeDefinition
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

                UIManagerMB uiManager = UIManagerMB.Instance;
                if (uiManager == null)
                {
                    Debug.LogWarning($"{nameof(HideTutorialToDoListStepRuntime)}: {nameof(UIManagerMB)} is not available.");
                    return ActionNodeStatus.Failed;
                }

                UITutorialToDoListMB toDoListUI = uiManager.TutorialToDoListUI;
                if (toDoListUI == null)
                {
                    Debug.LogWarning($"{nameof(HideTutorialToDoListStepRuntime)}: {nameof(UITutorialToDoListMB)} is not assigned.");
                    return ActionNodeStatus.Failed;
                }

                toDoListUI.Hide();
                return ActionNodeStatus.Continue;
            }
        }
    }
}
