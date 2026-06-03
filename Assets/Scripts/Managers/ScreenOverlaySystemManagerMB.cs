using BC.UI;
using UnityEngine;

namespace BC.Managers
{
    [DisallowMultipleComponent]
    public sealed class ScreenOverlaySystemManagerMB : MonoBehaviour
    {
        public static ScreenOverlaySystemManagerMB Instance { get; private set; }

        [Header("References")]
        [SerializeField] private UIManagerMB uiManager;
        [SerializeField] private UIScreenOverlayLayerMB screenOverlayLayerUI;

        private bool hasLoggedMissingLayer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                EnsureScreenOverlayLayer();
                return;
            }

            if (Instance != this)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool ShowOverlay(ScreenOverlayShowRequestData request)
        {
            return TryShowOverlay(request, out _);
        }

        public bool HideOverlay(ScreenOverlayHideRequestData request)
        {
            return TryHideOverlay(request, out _);
        }

        public bool TryShowOverlay(ScreenOverlayShowRequestData request, out string failureReason)
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

            EnsureScreenOverlayLayer();
            if (screenOverlayLayerUI == null)
            {
                failureReason = $"{nameof(UIScreenOverlayLayerMB)} is not available in scene/UIManager.";
                return false;
            }

            return screenOverlayLayerUI.TryShow(sanitizedRequest, out failureReason);
        }

        public bool TryHideOverlay(ScreenOverlayHideRequestData request, out string failureReason)
        {
            if (!request.displayId.IsValid)
            {
                failureReason = "Screen overlay display id is not set.";
                return false;
            }

            EnsureScreenOverlayLayer();
            if (screenOverlayLayerUI == null)
            {
                failureReason = $"{nameof(UIScreenOverlayLayerMB)} is not available in scene/UIManager.";
                return false;
            }

            return screenOverlayLayerUI.TryHide(request.displayId, out failureReason);
        }

        private void EnsureScreenOverlayLayer()
        {
            if (screenOverlayLayerUI != null)
                return;

            uiManager ??= UIManagerMB.Instance;
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManagerMB>(FindObjectsInactive.Include);

            if (uiManager != null)
                screenOverlayLayerUI = uiManager.ScreenOverlayLayerUI;

            screenOverlayLayerUI ??= GetComponentInChildren<UIScreenOverlayLayerMB>(true);
            if (screenOverlayLayerUI != null)
                return;

            if (!hasLoggedMissingLayer)
            {
                hasLoggedMissingLayer = true;
                Debug.LogWarning($"{nameof(ScreenOverlaySystemManagerMB)}: {nameof(UIScreenOverlayLayerMB)} was not found in scene/UIManager. Screen overlay display is unavailable.", this);
            }
        }
    }
}
