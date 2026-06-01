using BC.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
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
        private readonly List<UIToastItemMB> activeItems = new();
        private readonly List<UIToastItemMB> pooledItems = new();
        private IObjectPool<UIToastItemMB> toastItemPool;

        private RectTransform StackRoot => stackRoot != null ? stackRoot : transform as RectTransform;

        private void Awake()
        {
            EnsureStructure();
        }

        private void OnDestroy()
        {
            DestroyAllItemsImmediate(activeItems);
            DestroyAllItemsImmediate(pooledItems);
            activeItems.Clear();
            pooledItems.Clear();
            toastItemPool?.Clear();
            toastItemPool = null;
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

            UIToastItemMB item = AcquireToastItem();
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

        private UIToastItemMB AcquireToastItem()
        {
            EnsurePool();
            UIToastItemMB item = toastItemPool?.Get();
            if (item != null)
                activeItems.Add(item);

            return item;
        }

        private void EnsurePool()
        {
            if (toastItemPool != null)
                return;

            toastItemPool = new ObjectPool<UIToastItemMB>(CreatePooledItem, OnGetFromPool, OnReleaseToPool, OnDestroyPooledItem, false, 0, 32);
        }

        private UIToastItemMB CreatePooledItem()
        {
            UIToastItemMB itemTemplate = EnsureToastItemTemplate();
            if (itemTemplate == null)
                return null;

            UIToastItemMB itemInstance = Instantiate(itemTemplate, StackRoot, false);
            itemInstance.gameObject.name = "ToastItem";
            itemInstance.BindPool(toastItemPool);
            itemInstance.gameObject.SetActive(false);
            return itemInstance;
        }

        private void OnGetFromPool(UIToastItemMB item)
        {
            if (item == null)
                return;

            pooledItems.Remove(item);
            item.transform.SetParent(StackRoot, false);
            item.gameObject.SetActive(true);
            item.ResetForReuse();
        }

        private void OnReleaseToPool(UIToastItemMB item)
        {
            if (item == null)
                return;

            activeItems.Remove(item);
            if (!pooledItems.Contains(item))
                pooledItems.Add(item);

            item.ResetForReuse();
            item.gameObject.SetActive(false);
        }

        private void OnDestroyPooledItem(UIToastItemMB item)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        private static void DestroyAllItemsImmediate(List<UIToastItemMB> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                    Destroy(items[i].gameObject);
            }
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
