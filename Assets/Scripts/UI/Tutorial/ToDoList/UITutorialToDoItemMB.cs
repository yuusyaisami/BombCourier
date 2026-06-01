using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    // チュートリアル ToDoList の1行UI。
    // Toggle でチェック状態を表示し、ラベルで内容を説明する。
    // UITutorialToDoListMB がプールするプレハブに使用する。
    public sealed class UITutorialToDoItemMB : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TextMeshProUGUI labelText;

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>ラベルテキストを設定し、チェック状態をリセットする。</summary>
        public void Setup(string text)
        {
            if (labelText != null)
                labelText.text = text;

            if (toggle != null)
            {
                toggle.isOn          = false;
                toggle.interactable  = false; // 表示専用。ユーザー操作を受け付けない。
            }
        }

        /// <summary>チェック状態を更新する。</summary>
        public void SetCompleted(bool completed)
        {
            if (toggle != null)
                toggle.isOn = completed;
        }
    }
}
