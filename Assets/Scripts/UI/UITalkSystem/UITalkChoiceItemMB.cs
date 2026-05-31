using System;
using TMPro;
using BC.UI.Effect;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UITalkChoiceItemMB : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI choiceText;
        [SerializeField] private UINoiseOutlineMB noiseOutline;
        [SerializeField, Range(0.1f, 1.0f)] private float unselectedBackgroundAlphaMultiplier = 0.55f;

        private RectTransform rectTransform;
        private LayoutElement layoutElement;
        private Color backgroundBaseColor;
        private bool backgroundColorCaptured;
        private int choiceIndex = -1;
        private Action<int> clickCallback;

        private void Awake()
        {
            ResolveReferences();
            RefreshBasePresentation();
        }

        private void OnValidate()
        {
            ResolveReferences();
            RefreshBasePresentation();
        }

        private void Reset()
        {
            ResolveReferences();
            RefreshBasePresentation();
        }

        public void Initialize(
            int index,
            float itemMinHeight,
            float unselectedAlphaMultiplier,
            Color outlineColor,
            Vector2 outlineDistance,
            Action<int> selectionCallback)
        {
            choiceIndex = index;
            unselectedBackgroundAlphaMultiplier = unselectedAlphaMultiplier;
            clickCallback = selectionCallback;

            ResolveReferences();
            EnsureRuntimeTemplateStructure(itemMinHeight);

            if (noiseOutline != null)
            {
                float outlineWidth = Mathf.Clamp(Mathf.Max(Mathf.Abs(outlineDistance.x), Mathf.Abs(outlineDistance.y)) * 0.01f, 0.01f, 0.12f);
                noiseOutline.SetStyle(outlineColor, outlineWidth);
            }

            RefreshBasePresentation();
            SetSelected(false);
        }

        public void Apply(string text)
        {
            ResolveReferences();

            if (choiceText != null)
            {
                NormalizeChoiceTextRectTransform();
                choiceText.text = text ?? string.Empty;
            }
        }

        public void SetSelected(bool isSelected)
        {
            ResolveReferences();

            if (!backgroundColorCaptured)
            {
                RefreshBasePresentation();
            }

            if (noiseOutline != null)
            {
                noiseOutline.SetFocused(isSelected);
            }

            if (backgroundImage != null)
            {
                Color color = backgroundBaseColor;
                color.a = isSelected
                    ? backgroundBaseColor.a
                    : backgroundBaseColor.a * Mathf.Clamp01(unselectedBackgroundAlphaMultiplier);
                backgroundImage.color = color;
                backgroundImage.raycastTarget = true;
            }

            if (choiceText != null)
            {
                choiceText.raycastTarget = false;
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TryInvokeSelection(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TryInvokeSelection(eventData);
        }

        private void TryInvokeSelection(PointerEventData eventData)
        {
            if (!IsSupportedPointer(eventData))
                return;

            clickCallback?.Invoke(choiceIndex);
        }

        private static bool IsSupportedPointer(PointerEventData eventData)
        {
            if (eventData == null)
                return false;

            // PointerId >= 0 covers touch/pen pointers; left mouse click remains supported for desktop.
            return eventData.pointerId >= 0 || eventData.button == PointerEventData.InputButton.Left;
        }

        private void ResolveReferences()
        {
            rectTransform ??= transform as RectTransform;
            layoutElement ??= GetComponent<LayoutElement>();
            backgroundImage ??= GetComponent<Image>();
            choiceText ??= GetComponentInChildren<TextMeshProUGUI>(true);
            noiseOutline ??= GetComponent<UINoiseOutlineMB>();

            UIOutlineSystemMB legacyOutline = GetComponent<UIOutlineSystemMB>();
            if (legacyOutline != null)
                legacyOutline.enabled = false;
        }

        private void RefreshBasePresentation()
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundBaseColor = backgroundImage.color;
            backgroundColorCaptured = true;
        }

        private void EnsureRuntimeTemplateStructure(float itemMinHeight)
        {
            rectTransform ??= transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            if (layoutElement == null)
            {
                layoutElement = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            }

            float resolvedMinHeight = Mathf.Max(1f, itemMinHeight);
            layoutElement.minHeight = resolvedMinHeight;
            layoutElement.preferredHeight = resolvedMinHeight;
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 0f;

            if (backgroundImage == null)
            {
                backgroundImage = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            }

            backgroundImage.raycastTarget = true;

            if (choiceText == null)
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(rectTransform, false);
                choiceText = textObject.GetComponent<TextMeshProUGUI>();
            }

            NormalizeChoiceTextRectTransform();

            choiceText.fontSize = 28f;
            choiceText.alignment = TextAlignmentOptions.MidlineLeft;
            choiceText.textWrappingMode = TextWrappingModes.Normal;
            choiceText.overflowMode = TextOverflowModes.Overflow;
            choiceText.color = Color.white;
            choiceText.raycastTarget = false;

            if (noiseOutline == null)
            {
                noiseOutline = gameObject.GetComponent<UINoiseOutlineMB>() ?? gameObject.AddComponent<UINoiseOutlineMB>();
            }
        }

        private void NormalizeChoiceTextRectTransform()
        {
            if (choiceText == null)
                return;

            ContentSizeFitter textFitter = choiceText.GetComponent<ContentSizeFitter>();
            if (textFitter != null)
                textFitter.enabled = false;

            LayoutElement textLayout = choiceText.GetComponent<LayoutElement>();
            if (textLayout != null)
                textLayout.ignoreLayout = true;

            RectTransform textRect = choiceText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;
        }
    }
}
