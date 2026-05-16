// Tooltipを実際に出したいときに使う
using BC.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BC.UI
{
    public sealed class TooltipTargetMB : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string defaultTooltipText; // ツールチップに表示するテキストを割り当てるためのフィールド

        private string tooltipText; // ツールチップに表示するテキストを保持するフィールド
        private bool isHovered; // ポインタがオブジェクトの上にあるかどうかを管理するフラグ

        public string TooltipText
        {
            get => tooltipText;
            set
            {
                tooltipText = value;

                if (isHovered)
                    ShowCurrentTooltip();
            }
        }

        private void Awake()
        {
            tooltipText = defaultTooltipText; // 初期化時にdefaultTooltipTextの値をtooltipTextにコピーする
        }

        private void OnDisable()
        {
            HandleHoverExit();
        }

        private void OnDestroy()
        {
            HandleHoverExit();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HandleHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HandleHoverExit();
        }

        // マウスがオブジェクトに入ったときにツールチップを表示するためのイベントハンドラー
        private void OnMouseEnter()
        {
            HandleHoverEnter();
        }

        // マウスがオブジェクトから出たときにツールチップを非表示にするためのイベントハンドラー
        private void OnMouseExit()
        {
            HandleHoverExit();
        }

        private void HandleHoverEnter()
        {
            isHovered = true;
            ShowCurrentTooltip();
        }

        private void HandleHoverExit()
        {
            if (!isHovered)
                return;

            isHovered = false;
            TooltipManagerMB.Instance?.HideTooltip(this);
        }

        private void ShowCurrentTooltip()
        {
            if (!isActiveAndEnabled)
                return;

            TooltipManagerMB.Instance?.ShowTooltip(this, tooltipText, transform);
        }
    }
}