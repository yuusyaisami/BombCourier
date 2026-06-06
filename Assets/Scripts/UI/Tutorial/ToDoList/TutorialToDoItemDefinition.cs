using System;
using UnityEngine;
using UnityEngine.Localization;

namespace BC.UI
{
    // チュートリアル ToDoList の1アイテムのオーサリングデータ。
    // TutorialToDoListSO に含まれ、UITutorialToDoListMB が読み取る。
    [Serializable]
    public sealed class TutorialToDoItemDefinition
    {
        [Tooltip("ToDoリストに表示するフォールバックテキスト（Key が見つからない場合に表示。例:「アイテムを拾う」）。")]
        [SerializeField] private string labelText;

        [Tooltip("ローカライズ用 String Table。Key で引けなければ labelText を表示する。")]
        [SerializeField] private LocalizedStringTable table;
        [Tooltip("ローカライズ用エントリ Key。")]
        [SerializeField] private string entry;

        [SerializeReference]
        [Tooltip("達成条件。ManualOnly の場合は SetItemCompleted(i) を外部から呼ぶ。")]
        private TutorialToDoConditionAuthoring condition;

        public string LabelText => labelText;
        public LocalizedStringTable Table => table;
        public string Entry => entry;
        public TutorialToDoConditionAuthoring Condition => condition;
    }
}
