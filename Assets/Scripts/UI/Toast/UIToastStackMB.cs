using BC.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    public enum ToastStackDirection
    {
        Downwards = 0,
        Upwards = 1,
    }

    [DisallowMultipleComponent]
    public sealed class UIToastStackMB : MonoBehaviour
    {
        [SerializeField] private RectTransform stackRoot;
        [SerializeField] private UIToastItemMB toastItemPrefab;
        [SerializeField] private ToastStackDirection growthDirection = ToastStackDirection.Downwards;
        [SerializeField, Min(0.0f)] private float itemSpacing = 10.0f;
        [SerializeField] private Vector2 runtimeScreenOffset = new Vector2(44f, 44f);
        [SerializeField, Min(120.0f)] private float runtimeWidth = 360f;

        private VerticalLayoutGroup verticalLayoutGroup;
        private ContentSizeFitter contentSizeFitter;
        private bool runtimeDefaultsApplied;
        private UIToastItemMB runtimeToastItemTemplate;

        private RectTransform StackRoot => stackRoot != null ? stackRoot : transform as RectTransform;

        private void Awake()
        {
            EnsureStructure();
        }

        private void OnValidate()
        {
            EnsureStructure();
        }

        private void Reset()
        {
            EnsureStructure();
        }

        public void ConfigureRuntimeDefaults()
        {
            runtimeDefaultsApplied = true;
            EnsureStructure();
            EnsureToastItemTemplate();
            ApplyRuntimeAnchors();
        }

        public void ShowToast(ToastRequestData request)
        {
            if (!request.HasVisibleContent)
                return;

            EnsureStructure();

            UIToastItemMB item = CreateToastItemInstance();
            if (item == null)
                return;

            if (growthDirection == ToastStackDirection.Downwards)
                item.transform.SetAsFirstSibling();
            else
                item.transform.SetAsLastSibling();

            item.Show(request).Forget();
        }

        private void EnsureStructure()
        {
            if (stackRoot == null)
                stackRoot = transform as RectTransform;

            if (stackRoot == null)
                return;

            verticalLayoutGroup = stackRoot.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null)
                verticalLayoutGroup = stackRoot.gameObject.AddComponent<VerticalLayoutGroup>();

            contentSizeFitter = stackRoot.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
                contentSizeFitter = stackRoot.gameObject.AddComponent<ContentSizeFitter>();

            verticalLayoutGroup.spacing = Mathf.Max(0f, itemSpacing);
            verticalLayoutGroup.childAlignment = growthDirection == ToastStackDirection.Downwards
                ? TextAnchor.UpperRight
                : TextAnchor.LowerRight;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (runtimeDefaultsApplied)
            {
                EnsureToastItemTemplate();
                ApplyRuntimeAnchors();
            }
        }

        private void ApplyRuntimeAnchors()
        {
            if (StackRoot == null)
                return;

            if (growthDirection == ToastStackDirection.Downwards)
            {
                StackRoot.anchorMin = new Vector2(1f, 1f);
                StackRoot.anchorMax = new Vector2(1f, 1f);
                StackRoot.pivot = new Vector2(1f, 1f);
                StackRoot.anchoredPosition = new Vector2(-runtimeScreenOffset.x, -runtimeScreenOffset.y);
            }
            else
            {
                StackRoot.anchorMin = new Vector2(1f, 0f);
                StackRoot.anchorMax = new Vector2(1f, 0f);
                StackRoot.pivot = new Vector2(1f, 0f);
                StackRoot.anchoredPosition = new Vector2(-runtimeScreenOffset.x, runtimeScreenOffset.y);
            }

            StackRoot.sizeDelta = new Vector2(runtimeWidth, 0f);
        }

        private UIToastItemMB CreateToastItemInstance()
        {
            UIToastItemMB itemTemplate = EnsureToastItemTemplate();
            if (itemTemplate == null)
                return null;

            UIToastItemMB itemInstance = Instantiate(itemTemplate, StackRoot, false);
            itemInstance.gameObject.name = "ToastItem";
            itemInstance.gameObject.SetActive(true);
            return itemInstance;
        }

        private UIToastItemMB EnsureToastItemTemplate()
        {
            if (toastItemPrefab != null)
                return toastItemPrefab;

            if (runtimeToastItemTemplate != null)
                return runtimeToastItemTemplate;

            if (StackRoot == null)
                return null;

            Transform existingTemplate = StackRoot.Find("ToastItemTemplate");
            if (existingTemplate != null)
                runtimeToastItemTemplate = existingTemplate.GetComponent<UIToastItemMB>();

            if (runtimeToastItemTemplate == null)
            {
                GameObject templateObject = new GameObject("ToastItemTemplate", typeof(RectTransform), typeof(UIToastItemMB));
                templateObject.transform.SetParent(StackRoot, false);
                runtimeToastItemTemplate = templateObject.GetComponent<UIToastItemMB>();
            }

            runtimeToastItemTemplate.ConfigureRuntimeTemplate();
            runtimeToastItemTemplate.gameObject.SetActive(false);
            return runtimeToastItemTemplate;
        }
    }
}