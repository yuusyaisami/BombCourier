using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UITalkChoiceItemMB : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI choiceText;
        [SerializeField, Range(0.1f, 1.0f)] private float unselectedBackgroundAlphaMultiplier = 0.55f;
        [SerializeField, Range(0.8f, 1.2f)] private float selectedScale = 1.0f;
        [SerializeField, Range(0.8f, 1.2f)] private float unselectedScale = 0.98f;

        private RectTransform rectTransform;
        private Color backgroundBaseColor;
        private bool backgroundColorCaptured;

        private void Awake()
        {
            ResolveReferences();
            CaptureBasePresentation();
        }

        private void OnValidate()
        {
            ResolveReferences();
            CaptureBasePresentation();
        }

        private void Reset()
        {
            ResolveReferences();
            CaptureBasePresentation();
        }

        public void ConfigureRuntimeTemplate()
        {
            EnsureRuntimeTemplateStructure();
            ResolveReferences();
            CaptureBasePresentation();
            SetSelected(false);
        }

        public void Apply(string text)
        {
            ResolveReferences();

            if (choiceText != null)
                choiceText.text = text ?? string.Empty;
        }

        public void SetSelected(bool isSelected)
        {
            ResolveReferences();
            CaptureBasePresentation();

            if (backgroundImage != null)
            {
                Color color = backgroundBaseColor;
                color.a = isSelected
                    ? backgroundBaseColor.a
                    : backgroundBaseColor.a * Mathf.Clamp01(unselectedBackgroundAlphaMultiplier);
                backgroundImage.color = color;
                backgroundImage.raycastTarget = false;
            }

            if (choiceText != null)
                choiceText.raycastTarget = false;

            if (rectTransform != null)
            {
                float scale = isSelected ? selectedScale : unselectedScale;
                rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void ResolveReferences()
        {
            rectTransform ??= transform as RectTransform;
            backgroundImage ??= GetComponent<Image>();
            choiceText ??= GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void CaptureBasePresentation()
        {
            if (backgroundColorCaptured || backgroundImage == null)
                return;

            backgroundBaseColor = backgroundImage.color;
            backgroundColorCaptured = true;
        }

        private void EnsureRuntimeTemplateStructure()
        {
            rectTransform ??= transform as RectTransform;
            if (rectTransform == null)
                return;

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();

            layoutElement.minHeight = 52f;
            layoutElement.flexibleWidth = 1f;

            if (backgroundImage == null)
                backgroundImage = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();

            if (choiceText == null)
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(rectTransform, false);
                choiceText = textObject.GetComponent<TextMeshProUGUI>();
            }

            RectTransform textRect = choiceText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 8f);
            textRect.offsetMax = new Vector2(-18f, -8f);

            choiceText.fontSize = 28f;
            choiceText.alignment = TextAlignmentOptions.MidlineLeft;
            choiceText.textWrappingMode = TextWrappingModes.Normal;
            choiceText.overflowMode = TextOverflowModes.Overflow;
            choiceText.color = Color.white;
        }
    }
}