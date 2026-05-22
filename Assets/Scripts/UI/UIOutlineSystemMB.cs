using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class UIOutlineSystemMB : MonoBehaviour
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Outline outline;
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField] private Vector2 effectDistance = new(2f, -2f);
        [SerializeField] private bool useGraphicAlpha = true;

        private void Awake()
        {
            EnsureComponents();
            ApplySettings();
            SetHighlighted(false);
        }

        private void Reset()
        {
            EnsureComponents();
            ApplySettings();
            SetHighlighted(false);
        }

        private void OnValidate()
        {
            EnsureComponents();
            ApplySettings();
        }

        public void Configure(Graphic graphic, Color color, Vector2 distance)
        {
            targetGraphic = graphic != null ? graphic : GetComponent<Graphic>();
            outlineColor = color;
            effectDistance = distance;
            EnsureComponents();
            ApplySettings();
        }

        public void SetHighlighted(bool isHighlighted)
        {
            EnsureComponents();

            if (outline != null)
            {
                outline.enabled = isHighlighted;
            }
        }

        private void EnsureComponents()
        {
            targetGraphic ??= GetComponent<Graphic>();
            outline ??= GetComponent<Outline>();

            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }
        }

        private void ApplySettings()
        {
            if (outline == null)
                return;

            outline.effectColor = outlineColor;
            outline.effectDistance = effectDistance;
            outline.useGraphicAlpha = useGraphicAlpha;
        }
    }
}
