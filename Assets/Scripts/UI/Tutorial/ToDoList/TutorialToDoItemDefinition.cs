using System;
using UnityEngine;

namespace BC.UI
{
    // チュートリアル ToDoList の1アイテムのオーサリングデータ。
    // TutorialToDoListSO に含まれ、UITutorialToDoListMB が読み取る。
    [Serializable]
    public sealed class TutorialToDoItemDefinition
    {
        [Tooltip("ToDoリストに表示するテキスト（例:「アイテムを拾う」）。")]
        [SerializeField] private string labelText;

        [SerializeReference]
        [Tooltip("達成条件。ManualOnly の場合は SetItemCompleted(i) を外部から呼ぶ。")]
        private TutorialToDoConditionAuthoring condition;

        public string                           LabelText => labelText;
        public TutorialToDoConditionAuthoring   Condition => condition;
    }
}
