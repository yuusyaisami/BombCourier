using System;
using BC.Managers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIToastItemMB : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private bool hideBackgroundGraphic = true;
        [SerializeField] private Image iconImage;
        [SerializeField] private LayoutElement iconLayoutElement;
        [SerializeField] private TextMeshProUGUI messageText;

        private const float DefaultIconSize = 28f;

        private Tween activeTween;
        private IObjectPool<UIToastItemMB> ownerPool;

        private void Awake()
        {
            ResolveReferences();
            ApplyStaticPresentation();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ApplyStaticPresentation();
        }

        private void Reset()
        {
            ResolveReferences();
            ApplyStaticPresentation();
        }

        public void ConfigureRuntimeTemplate()
        {
            EnsureRuntimeTemplateStructure();
            ResolveReferences();
            ApplyStaticPresentation();
            ResetVisualState();
        }

        public void BindPool(IObjectPool<UIToastItemMB> pool)
        {
            ownerPool = pool;
        }

        public void ResetForReuse()
        {
            activeTween?.Kill();
            activeTween = null;

            ResolveReferences();
            ApplyStaticPresentation();
            ResetVisualState();
        }

        public async UniTaskVoid Show(ToastRequestData request)
        {
            try
            {
                ToastRequestData sanitizedRequest = request.Sanitize();
                ResetForReuse();
                ResolveReferences();
                ApplyStaticPresentation();
                ApplyRequest(sanitizedRequest);
                await PlayLifecycleAsync(sanitizedRequest);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                if (this != null)
                    ownerPool?.Release(this);
            }
        }

        private async UniTask PlayLifecycleAsync(ToastRequestData request)
        {
            float fadeInDuration = Mathf.Max(0f, request.fadeInDuration);
            float visibleDuration = Mathf.Max(0f, request.visibleDuration);
            float fadeOutDuration = Mathf.Max(0f, request.fadeOutDuration);

            canvasGroup.alpha = 0f;

            if (fadeInDuration > 0f)
            {
                activeTween = canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutSine);
                await activeTween.AsyncWaitForCompletion();
                activeTween = null;
            }
            else
                canvasGroup.alpha = 1f;

            if (visibleDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(visibleDuration), DelayType.DeltaTime, PlayerLoopTiming.Update, destroyCancellationToken);

            if (fadeOutDuration > 0f)
            {
                activeTween = canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InSine);
                await activeTween.AsyncWaitForCompletion();
                activeTween = null;
            }
            else
                canvasGroup.alpha = 0f;
        }

        private void OnDisable()
        {
            activeTween?.Kill();
            activeTween = null;
        }

        private void ApplyRequest(ToastRequestData request)
        {
            bool hasIcon = request.icon != null;

            if (iconImage != null)
            {
                iconImage.sprite = request.icon;
                iconImage.enabled = hasIcon;
                iconImage.raycastTarget = false;
            }

            if (iconLayoutElement != null)
            {
                float iconSize = hasIcon ? DefaultIconSize : 0f;
                iconLayoutElement.minWidth = iconSize;
                iconLayoutElement.preferredWidth = iconSize;
                iconLayoutElement.minHeight = hasIcon ? DefaultIconSize : 0f;
                iconLayoutElement.preferredHeight = hasIcon ? DefaultIconSize : 0f;
            }

            if (messageText != null)
                messageText.text = request.text ?? string.Empty;
        }

        private void ResolveReferences()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            backgroundImage ??= GetComponent<Image>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (iconImage == null)
                iconImage = ResolveIconImage();

            if (iconLayoutElement == null && iconImage != null)
                iconLayoutElement = iconImage.GetComponent<LayoutElement>();

            if (messageText == null)
                messageText = ResolveMessageText();
        }

        private void ApplyStaticPresentation()
        {
            if (backgroundImage != null)
            {
                backgroundImage.enabled = !hideBackgroundGraphic;
                backgroundImage.raycastTarget = false;
            }

            if (iconImage != null)
            {
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            if (messageText != null)
                messageText.raycastTarget = false;
        }

        private Image ResolveIconImage()
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image namedIconImage))
                return namedIconImage;

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image candidate = images[i];
                if (candidate != null && candidate != backgroundImage)
                    return candidate;
            }

            return null;
        }

        private TextMeshProUGUI ResolveMessageText()
        {
            Transform textTransform = transform.Find("Text");
            if (textTransform != null && textTransform.TryGetComponent(out TextMeshProUGUI namedMessageText))
                return namedMessageText;

            return GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void EnsureRuntimeTemplateStructure()
        {
            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null)
                return;

            HorizontalLayoutGroup horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayoutGroup == null)
                horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

            ContentSizeFitter contentSizeFitter = GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
                contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();

            horizontalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
            horizontalLayoutGroup.spacing = 12f;
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childControlHeight = true;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            layoutElement.minHeight = 0f;
            layoutElement.flexibleWidth = 1f;

            if (iconImage == null)
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
                iconObject.transform.SetParent(rootRect, false);
                iconImage = iconObject.GetComponent<Image>();
            }

            iconLayoutElement ??= iconImage.GetComponent<LayoutElement>() ?? iconImage.gameObject.AddComponent<LayoutElement>();
            iconLayoutElement.minWidth = DefaultIconSize;
            iconLayoutElement.preferredWidth = DefaultIconSize;
            iconLayoutElement.minHeight = DefaultIconSize;
            iconLayoutElement.preferredHeight = DefaultIconSize;
            iconImage.color = Color.white;

            if (messageText == null)
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(rootRect, false);
                messageText = textObject.GetComponent<TextMeshProUGUI>();
            }

            LayoutElement messageLayout = messageText.GetComponent<LayoutElement>() ?? messageText.gameObject.AddComponent<LayoutElement>();
            messageLayout.flexibleWidth = 1f;
            messageLayout.minWidth = 0f;

            messageText.fontSize = 24f;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Ellipsis;
            messageText.color = Color.white;
            messageText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void ResetVisualState()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (iconLayoutElement != null)
            {
                iconLayoutElement.minWidth = 0f;
                iconLayoutElement.preferredWidth = 0f;
                iconLayoutElement.minHeight = 0f;
                iconLayoutElement.preferredHeight = 0f;
            }

            if (messageText != null)
                messageText.text = string.Empty;
        }
    }
}