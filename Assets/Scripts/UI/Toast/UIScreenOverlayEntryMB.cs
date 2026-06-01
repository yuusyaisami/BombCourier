using BC.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIScreenOverlayEntryMB : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image imageGraphic;
        [SerializeField] private TextMeshProUGUI textGraphic;

        public RectTransform RootRect => transform as RectTransform;
        public Image ImageGraphic => imageGraphic;
        public TextMeshProUGUI TextGraphic => textGraphic;

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

        public void ApplyImage(ScreenOverlayShowRequestData request)
        {
            request.EnsureDefaultsInitialized();
            ScreenOverlayShowRequestData sanitizedRequest = request.Sanitize();

            ResolveReferences();
            ApplyStaticPresentation();

            if (RootRect != null)
                RootRect.sizeDelta = sanitizedRequest.size;

            if (imageGraphic != null)
            {
                imageGraphic.gameObject.SetActive(true);
                imageGraphic.sprite = sanitizedRequest.sprite;
                imageGraphic.color = sanitizedRequest.imageColor;

                RectTransform imageRect = imageGraphic.rectTransform;
                imageRect.sizeDelta = sanitizedRequest.size;
            }

            if (textGraphic != null)
            {
                textGraphic.gameObject.SetActive(false);
                textGraphic.text = string.Empty;
            }
        }

        public void ApplyText(ScreenOverlayShowRequestData request)
        {
            request.EnsureDefaultsInitialized();
            ScreenOverlayShowRequestData sanitizedRequest = request.Sanitize();

            ResolveReferences();
            ApplyStaticPresentation();

            if (imageGraphic != null)
            {
                imageGraphic.gameObject.SetActive(false);
                imageGraphic.sprite = null;
            }

            if (textGraphic != null)
            {
                textGraphic.gameObject.SetActive(true);
                textGraphic.text = sanitizedRequest.text ?? string.Empty;
                textGraphic.fontSize = sanitizedRequest.fontSize;
                textGraphic.color = sanitizedRequest.textColor;
                textGraphic.alignment = TextAlignmentOptions.Center;
                textGraphic.textWrappingMode = TextWrappingModes.Normal;
                textGraphic.overflowMode = TextOverflowModes.Overflow;

                RectTransform textRect = textGraphic.rectTransform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
                Vector2 preferredSize = new(textGraphic.preferredWidth, textGraphic.preferredHeight);
                textRect.sizeDelta = preferredSize;

                if (RootRect != null)
                    RootRect.sizeDelta = preferredSize;
            }
        }

        private void ResolveReferences()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (imageGraphic == null)
            {
                Transform imageTransform = transform.Find("Image");
                if (imageTransform != null)
                    imageGraphic = imageTransform.GetComponent<Image>();
            }

            if (textGraphic == null)
            {
                Transform textTransform = transform.Find("Text");
                if (textTransform != null)
                    textGraphic = textTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        private void EnsureRuntimeTemplateStructure()
        {
            RectTransform rootRect = RootRect;
            if (rootRect == null)
                return;

            if (imageGraphic == null)
            {
                GameObject imageObject = new("Image", typeof(RectTransform), typeof(Image));
                RectTransform imageRect = imageObject.GetComponent<RectTransform>();
                imageRect.SetParent(rootRect, false);
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageGraphic = imageObject.GetComponent<Image>();
            }

            if (textGraphic == null)
            {
                GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.SetParent(rootRect, false);
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textGraphic = textObject.GetComponent<TextMeshProUGUI>();
            }
        }

        private void ApplyStaticPresentation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (imageGraphic != null)
            {
                imageGraphic.raycastTarget = false;
                imageGraphic.preserveAspect = false;
            }

            if (textGraphic != null)
                textGraphic.raycastTarget = false;
        }

        private void ResetVisualState()
        {
            if (imageGraphic != null)
            {
                imageGraphic.sprite = null;
                imageGraphic.gameObject.SetActive(false);
            }

            if (textGraphic != null)
            {
                textGraphic.text = string.Empty;
                textGraphic.gameObject.SetActive(false);
            }

            if (RootRect != null)
                RootRect.sizeDelta = Vector2.zero;
        }
    }
}
