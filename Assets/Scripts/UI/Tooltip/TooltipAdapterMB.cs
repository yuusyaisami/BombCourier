// TooltipPrefabをアタッチするためのMonoBehaviour
using DG.Tweening;
using Febucci.TextAnimatorCore;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    public sealed class TooltipAdapterMB : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage; // ツールチップの背景イメージ
        [SerializeField] private TextAnimator titleText; // タイトルテキスト
        [SerializeField] private Vector3 hiddenScale = Vector3.zero; // 非表示状態のスケール
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;
        [SerializeField] private Vector2 contentPadding = new(20f, 20f); // テキストサイズに足す余白

        public Image BackgroundImage => backgroundImage;
        public RectTransform RectTransform { get; private set; }

        private string lastText;
        private Tween scaleTween;

        private void Awake()
        {
            RectTransform = transform as RectTransform;
        }

        public Vector2 ShowTooltip(string title)
        {
            string resolvedTitle = title ?? string.Empty;
            bool wasHidden = !gameObject.activeSelf || transform.localScale.sqrMagnitude <= 0.0001f;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (resolvedTitle != lastText)
                SetText(resolvedTitle);

            scaleTween?.Kill();

            if (wasHidden)
            {
                transform.localScale = hiddenScale;
                scaleTween = transform.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            return GetTooltipSize();
        }

        public void HideTooltip()
        {
            if (!gameObject.activeSelf)
                return;

            scaleTween?.Kill();
            scaleTween = transform.DOScale(hiddenScale, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (gameObject != null)
                        gameObject.SetActive(false);
                });
        }

        public void ImmediatelyHideTooltip()
        {
            scaleTween?.Kill();
            transform.localScale = hiddenScale;
            gameObject.SetActive(false);
        }

        private Vector2 SetText(string title)
        {
            lastText = title;

            if (titleText != null)
                titleText.SetText(title);

            // タイトルテキストのサイズに合わせて背景イメージのサイズを調整する
            if (backgroundImage != null && titleText != null)
            {
                Canvas.ForceUpdateCanvases();
                Vector2 textSize = titleText.GetPreferredSize();
                backgroundImage.rectTransform.sizeDelta = textSize + contentPadding;
                LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundImage.rectTransform);
            }

            return GetTooltipSize();
        }

        private Vector2 GetTooltipSize()
        {
            if (backgroundImage != null)
                return backgroundImage.rectTransform.rect.size;

            if (RectTransform != null)
                return RectTransform.rect.size;

            return Vector2.zero;
        }
    }
}