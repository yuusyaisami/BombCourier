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
            request.EnsureDefaultsInitialized();
            ScreenOverlayShowRequestData sanitizedRequest = request.Sanitize();

            if (!sanitizedRequest.displayId.IsValid || !sanitizedRequest.HasVisibleContent)
                return false;

            EnsureScreenOverlayLayer();
            return screenOverlayLayerUI != null && screenOverlayLayerUI.Show(sanitizedRequest);
        }

        public bool HideOverlay(ScreenOverlayHideRequestData request)
        {
            if (!request.displayId.IsValid)
                return false;

            EnsureScreenOverlayLayer();
            return screenOverlayLayerUI != null && screenOverlayLayerUI.Hide(request.displayId);
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
