using System;
using UnityEngine;

namespace BC.UI
{
    // 操作ガイドの1エントリの定義。インスペクターでオーサリングし、
    // UIOperationGuideMB または UIOperationGuideExtenderMB に渡す。
    [Serializable]
    public sealed class OperationGuideEntryDefinition
    {
        [Tooltip("PlayerInputCatalogMB に登録したアクション ID。対応アイコンを表示する。")]
        [SerializeField] private string actionCatalogId;

        [Tooltip("エントリの説明ラベル（例:「ジャンプ」「拾う」）。")]
        [SerializeField] private string labelText;

        [SerializeReference]
        [Tooltip("表示条件。null のとき常に表示。AlwaysVisibleCondition も利用可。")]
        private OperationVisibilityConditionAuthoring visibilityCondition;

        public string ActionCatalogId => actionCatalogId;
        public string LabelText => labelText;
        public OperationVisibilityConditionAuthoring VisibilityCondition => visibilityCondition;
    }
}
