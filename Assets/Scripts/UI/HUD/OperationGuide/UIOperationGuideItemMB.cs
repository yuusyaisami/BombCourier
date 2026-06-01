using BC.Inputs;
using BC.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.UI
{
    // 操作ガイドの1行UI。UIOperationGuideMB がプールするプレハブに使用する。
    // Bind でアクション参照とラベルを設定し、RefreshIcon でデバイス切替に追従する。
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIOperationGuideItemMB : MonoBehaviour
    {
        [SerializeField] private Image promptIconImage;
        [SerializeField] private Sprite fallbackIcon;
        [SerializeField] private TextMeshProUGUI actionLabel;

        private InputActionReference currentActionRef;
        private InputPromptDeviceKind lastPromptDeviceKind = InputPromptDeviceKind.Unknown;

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>アクション参照とラベルをバインドし、アイコンを即時更新する。</summary>
        public void Bind(InputActionReference actionRef, string label)
        {
            currentActionRef = actionRef;
            if (actionLabel != null)
                actionLabel.text = label;

            lastPromptDeviceKind = InputPromptDeviceKind.Unknown; // 強制リフレッシュ
            RefreshIcon(force: true);
        }

        /// <summary>デバイス種別が変わったときにアイコンを更新する。UIOperationGuideMB から呼ぶ。</summary>
        public void RefreshIcon(bool force = false)
        {
            if (promptIconImage == null) return;

            InputManagerMB inputManager = InputManagerMB.Instance;
            InputPromptDeviceKind current = inputManager != null
                ? inputManager.CurrentPromptDeviceKind
                : InputPromptDeviceKind.Unknown;

            if (!force && current == lastPromptDeviceKind) return;

            Sprite icon = InputPromptIconResolver.ResolveIcon(inputManager, currentActionRef, fallbackIcon);
            if (!ReferenceEquals(promptIconImage.sprite, icon))
                promptIconImage.sprite = icon;
            promptIconImage.enabled = icon != null || fallbackIcon != null;
            lastPromptDeviceKind = current;
        }

        /// <summary>エントリの表示/非表示を切り替える。</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
