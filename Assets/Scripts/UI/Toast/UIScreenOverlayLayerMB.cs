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
            return TryShow(request, out _);
        }

        public bool TryShow(ScreenOverlayShowRequestData request, out string failureReason)
        {
            request.EnsureDefaultsInitialized();
            ScreenOverlayShowRequestData sanitizedRequest = request.Sanitize();

            if (!sanitizedRequest.displayId.IsValid)
            {
                failureReason = "Screen overlay display id is not set.";
                return false;
            }

            if (!sanitizedRequest.HasVisibleContent)
            {
                failureReason = $"Screen overlay '{sanitizedRequest.displayId}' has no visible content for '{sanitizedRequest.contentKind}'.";
                return false;
            }

            EnsureStructure();
            if (OverlayRoot == null)
            {
                failureReason = "Overlay root RectTransform is not available.";
                return false;
            }

            RemoveExistingEntry(sanitizedRequest.displayId);
            failureReason = null;

            RuntimeEntry runtimeEntry = sanitizedRequest.contentKind switch
            {
                ScreenOverlayContentKind.Image => CreateGeneratedEntry(sanitizedRequest, out failureReason),
                ScreenOverlayContentKind.Text => CreateGeneratedEntry(sanitizedRequest, out failureReason),
                ScreenOverlayContentKind.Prefab => CreatePrefabEntry(sanitizedRequest, out failureReason),
                _ => null,
            };

            if (runtimeEntry == null)
            {
                failureReason ??= $"Unsupported screen overlay content kind '{sanitizedRequest.contentKind}'.";
                return false;
            }

            runtimeEntries[sanitizedRequest.displayId] = runtimeEntry;
            ApplySiblingOrder();
            failureReason = null;
            return true;
        }

        public bool Hide(ScreenOverlayDisplayId displayId)
        {
            return TryHide(displayId, out _);
        }

        public bool TryHide(ScreenOverlayDisplayId displayId, out string failureReason)
        {
            if (!displayId.IsValid)
            {
                failureReason = "Screen overlay display id is not set.";
                return false;
            }

            if (!runtimeEntries.TryGetValue(displayId, out RuntimeEntry runtimeEntry))
            {
                failureReason = $"Screen overlay '{displayId}' is not currently shown.";
                return false;
            }

            runtimeEntries.Remove(displayId);
            DestroyOverlayObject(runtimeEntry.RootObject);
            ApplySiblingOrder();
            failureReason = null;
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

        private RuntimeEntry CreateGeneratedEntry(ScreenOverlayShowRequestData request, out string failureReason)
        {
            failureReason = null;
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
                    failureReason = $"Unsupported generated screen overlay content kind '{request.contentKind}'.";
                    DestroyOverlayObject(rootObject);
                    return null;
            }

            return new RuntimeEntry(request.sortOrder, nextSequence++, rootObject, rootTransform);
        }

        private RuntimeEntry CreatePrefabEntry(ScreenOverlayShowRequestData request, out string failureReason)
        {
            if (request.prefab == null)
            {
                failureReason = "Screen overlay prefab is not assigned.";
                return null;
            }

            failureReason = null;

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
                failureReason = $"Screen overlay prefab '{request.prefab.name}' does not contain a RectTransform on the root object.";
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
