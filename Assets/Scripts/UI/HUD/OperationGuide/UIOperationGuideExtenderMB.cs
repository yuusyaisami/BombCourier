using System.Collections.Generic;
using UnityEngine;

namespace BC.UI
{
    // ステージやシーン固有の操作ガイドエントリを UIOperationGuideMB へ動的に追加するコンポーネント。
    // OnEnable で自動登録し OnDisable で自動解除するため、
    // ステージオブジェクトの有効/無効に連動させるだけで機能する。
    public sealed class UIOperationGuideExtenderMB : MonoBehaviour
    {
        [Tooltip("エントリを追加する UIOperationGuideMB。null の場合は FindAnyObjectByType で自動解決する。")]
        [SerializeField] private UIOperationGuideMB targetGuide;

        [SerializeField] private List<OperationGuideEntryDefinition> extraEntries = new();

        public IReadOnlyList<OperationGuideEntryDefinition> ExtraEntries => extraEntries;

        private UIOperationGuideMB resolvedGuide;

        private void OnEnable()
        {
            resolvedGuide = targetGuide != null
                ? targetGuide
                : Object.FindAnyObjectByType<UIOperationGuideMB>();
            resolvedGuide?.RegisterExtender(this);
        }

        private void OnDisable()
        {
            resolvedGuide?.UnregisterExtender(this);
            resolvedGuide = null;
        }
    }
}
