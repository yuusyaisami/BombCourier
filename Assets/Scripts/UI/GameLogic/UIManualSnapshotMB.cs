using BC.Inputs;
using BC.Manager;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIManualSnapshotMB : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image actionIconImage;
        [SerializeField] private Sprite fallbackActionIcon;
        [SerializeField] private Image captureProgressImage;
        [SerializeField] private Image unavailableOverlayImage;
        [SerializeField] private InputActionReference captureInputActionReference;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float requiredHoldSeconds = 0.6f;
        [SerializeField, Min(0.0f)] private float fadeDuration = 0.2f;

        private Tween fadeTween;
        private float holdSeconds;
        private bool isVisible;
        private bool waitForCaptureRelease;
        private InputPromptDeviceKind lastPromptDeviceKind = InputPromptDeviceKind.Unknown;

        private void Awake()
        {
            ApplyCanvasState(0.0f);
            isVisible = false;
            ResetHoldProgress();
            SetOverlayVisible(false);
            RefreshPromptIcon(forceRefresh: true);
        }

        private void OnEnable()
        {
            if (InputManagerMB.Instance != null)
                InputManagerMB.Instance.PromptDeviceKindChanged += HandlePromptDeviceKindChanged;

            RefreshPromptIcon(forceRefresh: true);
        }

        private void OnDisable()
        {
            if (InputManagerMB.Instance != null)
                InputManagerMB.Instance.PromptDeviceKindChanged -= HandlePromptDeviceKindChanged;

            fadeTween?.Kill();
            fadeTween = null;
            waitForCaptureRelease = false;
            ResetHoldProgress();
        }

        private void OnDestroy()
        {
            if (InputManagerMB.Instance != null)
                InputManagerMB.Instance.PromptDeviceKindChanged -= HandlePromptDeviceKindChanged;

            fadeTween?.Kill();
            fadeTween = null;
        }

        private void Update()
        {
            RefreshPromptIcon();

            GameLogicManagerMB gameLogic = GameLogicManagerMB.Instance;
            if (gameLogic == null)
            {
                ResetHoldProgress();
                SetOverlayVisible(false);
                SetVisible(false);
                return;
            }

            ManualSnapshotAvailability availability = gameLogic.EvaluateManualSnapshotAvailability();
            SetVisible(availability.ShouldShowUi);
            SetOverlayVisible(availability.ShouldShowUnavailableOverlay);

            if (!availability.ShouldShowUi || !availability.CanCapture)
            {
                ResetHoldProgress();
                return;
            }

            bool isCapturePressed = IsCapturePressed();
            if (!isCapturePressed)
            {
                waitForCaptureRelease = false;
            }
            else if (waitForCaptureRelease)
            {
                ResetHoldProgress();
                return;
            }

            if (isCapturePressed)
            {
                holdSeconds += Time.deltaTime;
                if (holdSeconds >= Mathf.Max(0.05f, requiredHoldSeconds))
                {
                    waitForCaptureRelease = true;
                    ResetHoldProgress();
                    gameLogic.TryCaptureManualSnapshot();
                    return;
                }
            }
            else if (holdSeconds > 0.0f)
            {
                holdSeconds = 0.0f;
            }

            UpdateProgressImage();
        }

        private bool IsCapturePressed()
        {
            InputAction action = captureInputActionReference != null ? captureInputActionReference.action : null;
            return action != null && action.IsPressed();
        }

        private void HandlePromptDeviceKindChanged(InputPromptDeviceKind _)
        {
            RefreshPromptIcon(forceRefresh: true);
        }

        private void RefreshPromptIcon(bool forceRefresh = false)
        {
            if (actionIconImage == null)
                return;

            InputManagerMB inputManager = InputManagerMB.Instance;
            InputPromptDeviceKind currentDeviceKind = inputManager != null
                ? inputManager.CurrentPromptDeviceKind
                : InputPromptDeviceKind.Unknown;

            if (!forceRefresh && currentDeviceKind == lastPromptDeviceKind)
                return;

            Sprite resolvedIcon = InputPromptIconResolver.ResolveIcon(inputManager, captureInputActionReference, fallbackActionIcon);
            if (!ReferenceEquals(actionIconImage.sprite, resolvedIcon))
                actionIconImage.sprite = resolvedIcon;

            actionIconImage.enabled = resolvedIcon != null;
            lastPromptDeviceKind = currentDeviceKind;
        }

        private void ResetHoldProgress()
        {
            holdSeconds = 0.0f;
            UpdateProgressImage();
        }

        private void UpdateProgressImage()
        {
            if (captureProgressImage == null)
                return;

            captureProgressImage.fillAmount = Mathf.Clamp01(holdSeconds / Mathf.Max(0.05f, requiredHoldSeconds));
        }

        private void SetOverlayVisible(bool visible)
        {
            if (unavailableOverlayImage != null)
                unavailableOverlayImage.enabled = visible;
        }

        private void SetVisible(bool visible)
        {
            if (isVisible == visible)
                return;

            isVisible = visible;
            fadeTween?.Kill();
            fadeTween = null;

            if (canvasGroup == null)
                return;

            float targetAlpha = visible ? 1.0f : 0.0f;
            if (fadeDuration <= 0.0f)
            {
                ApplyCanvasState(targetAlpha);
                return;
            }

            fadeTween = canvasGroup
                .DOFade(targetAlpha, fadeDuration)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() => ApplyCanvasState(canvasGroup.alpha))
                .OnComplete(() => ApplyCanvasState(targetAlpha));
        }

        private void ApplyCanvasState(float alpha)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = alpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
