using System.Collections.Generic;
using BC.Managers;
using UnityEngine;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIScreenOverlayLayerMB : MonoBehaviour
    {
        [SerializeField] private RectTransform overlayRoot;

        private long nextSequence;
        private readonly Dictionary<ScreenOverlayDisplayId, RuntimeEntry> runtimeEntries = new();

        public int ActiveDisplayCount => runtimeEntries.Count;

        private RectTransform OverlayRoot => overlayRoot != null ? overlayRoot : transform as RectTransform;

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

        private void OnDestroy()
        {
            ClearAllImmediate();
        }

        public bool Show(ScreenOverlayShowRequestData request)
        {
            request.EnsureDefaultsInitialized();
            ScreenOverlayShowRequestData sanitizedRequest = request.Sanitize();

            if (!sanitizedRequest.displayId.IsValid || !sanitizedRequest.HasVisibleContent)
                return false;

            EnsureStructure();
            if (OverlayRoot == null)
                return false;

            RemoveExistingEntry(sanitizedRequest.displayId);

            RuntimeEntry runtimeEntry = sanitizedRequest.contentKind switch
            {
                ScreenOverlayContentKind.Image => CreateGeneratedEntry(sanitizedRequest),
                ScreenOverlayContentKind.Text => CreateGeneratedEntry(sanitizedRequest),
                ScreenOverlayContentKind.Prefab => CreatePrefabEntry(sanitizedRequest),
                _ => null,
            };

            if (runtimeEntry == null)
                return false;

            runtimeEntries[sanitizedRequest.displayId] = runtimeEntry;
            ApplySiblingOrder();
            return true;
        }

        public bool Hide(ScreenOverlayDisplayId displayId)
        {
            if (!displayId.IsValid || !runtimeEntries.TryGetValue(displayId, out RuntimeEntry runtimeEntry))
                return false;

            runtimeEntries.Remove(displayId);
            DestroyOverlayObject(runtimeEntry.RootObject);
            ApplySiblingOrder();
            return true;
        }

        public bool TryGetDisplayRoot(ScreenOverlayDisplayId displayId, out RectTransform root)
        {
            if (runtimeEntries.TryGetValue(displayId, out RuntimeEntry runtimeEntry))
            {
                root = runtimeEntry.RootTransform;
                return root != null;
            }

            root = null;
            return false;
        }

        private void EnsureStructure()
        {
            overlayRoot ??= transform as RectTransform;
            if (overlayRoot == null)
                return;

            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;
            overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        }

        private RuntimeEntry CreateGeneratedEntry(ScreenOverlayShowRequestData request)
        {
            GameObject rootObject = new($"ScreenOverlay_{request.displayId.Value}", typeof(RectTransform), typeof(CanvasGroup), typeof(UIScreenOverlayEntryMB));
            RectTransform rootTransform = rootObject.GetComponent<RectTransform>();
            rootTransform.SetParent(OverlayRoot, false);
            rootTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rootTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rootTransform.pivot = new Vector2(0.5f, 0.5f);
            rootTransform.anchoredPosition = request.anchoredPosition;

            UIScreenOverlayEntryMB entry = rootObject.GetComponent<UIScreenOverlayEntryMB>();
            entry.ConfigureRuntimeTemplate();

            switch (request.contentKind)
            {
                case ScreenOverlayContentKind.Image:
                    entry.ApplyImage(request);
                    break;

                case ScreenOverlayContentKind.Text:
                    entry.ApplyText(request);
                    break;

                default:
                    DestroyOverlayObject(rootObject);
                    return null;
            }

            return new RuntimeEntry(request.sortOrder, nextSequence++, rootObject, rootTransform);
        }

        private RuntimeEntry CreatePrefabEntry(ScreenOverlayShowRequestData request)
        {
            if (request.prefab == null)
                return null;

            GameObject hostObject = new($"ScreenOverlay_{request.displayId.Value}", typeof(RectTransform));
            RectTransform hostTransform = hostObject.GetComponent<RectTransform>();
            hostTransform.SetParent(OverlayRoot, false);
            hostTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hostTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hostTransform.pivot = new Vector2(0.5f, 0.5f);
            hostTransform.anchoredPosition = request.anchoredPosition;

            GameObject instance = Instantiate(request.prefab, hostTransform, false);
            RectTransform instanceRectTransform = instance.GetComponent<RectTransform>();
            if (instanceRectTransform == null)
            {
                Debug.LogWarning($"{nameof(UIScreenOverlayLayerMB)}: Prefab '{request.prefab.name}' does not contain a RectTransform and cannot be shown as a screen overlay.", this);
                DestroyOverlayObject(hostObject);
                return null;
            }

            return new RuntimeEntry(request.sortOrder, nextSequence++, hostObject, hostTransform);
        }

        private void RemoveExistingEntry(ScreenOverlayDisplayId displayId)
        {
            if (!runtimeEntries.TryGetValue(displayId, out RuntimeEntry existingEntry))
                return;

            runtimeEntries.Remove(displayId);
            DestroyOverlayObject(existingEntry.RootObject);
        }

        private void ApplySiblingOrder()
        {
            if (runtimeEntries.Count <= 0)
                return;

            List<RuntimeEntry> orderedEntries = new(runtimeEntries.Values);
            orderedEntries.Sort(static (left, right) =>
            {
                int orderComparison = left.SortOrder.CompareTo(right.SortOrder);
                return orderComparison != 0
                    ? orderComparison
                    : left.Sequence.CompareTo(right.Sequence);
            });

            for (int i = 0; i < orderedEntries.Count; i++)
            {
                if (orderedEntries[i].RootTransform != null)
                    orderedEntries[i].RootTransform.SetSiblingIndex(i);
            }
        }

        private void ClearAllImmediate()
        {
            foreach (RuntimeEntry runtimeEntry in runtimeEntries.Values)
                DestroyOverlayObject(runtimeEntry.RootObject);

            runtimeEntries.Clear();
        }

        private static void DestroyOverlayObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private sealed class RuntimeEntry
        {
            public RuntimeEntry(
                int sortOrder,
                long sequence,
                GameObject rootObject,
                RectTransform rootTransform)
            {
                SortOrder = sortOrder;
                Sequence = sequence;
                RootObject = rootObject;
                RootTransform = rootTransform;
            }

            public int SortOrder { get; }
            public long Sequence { get; }
            public GameObject RootObject { get; }
            public RectTransform RootTransform { get; }
        }
    }
}
