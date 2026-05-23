using System.Threading;
using BC.Inputs;
using BC.Manager;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIIntroPathSkipMB : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text stageIndexText;
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private Image actionIconImage;
        [SerializeField] private Sprite fallbackActionIcon;
        [SerializeField] private Image holdProgressFillImage;
        [SerializeField] private InputActionReference skipInputActionReference;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float requiredHoldSeconds = 1.0f;
        [SerializeField, Min(0.0f)] private float fadeDuration = 0.2f;

        private Tween fadeTween;
        private float holdSeconds;
        private InputPromptDeviceKind lastPromptDeviceKind = InputPromptDeviceKind.Unknown;

        public float RequiredHoldSeconds => requiredHoldSeconds;

        private void Awake()
        {
            HideImmediate();
            ResetHoldProgress();
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
        }

        private void OnDestroy()
        {
            fadeTween?.Kill();
            fadeTween = null;

            if (InputManagerMB.Instance != null)
                InputManagerMB.Instance.PromptDeviceKindChanged -= HandlePromptDeviceKindChanged;
        }

        public void SetStageName(string stageName)
        {
            if (stageNameText != null)
                stageNameText.text = string.IsNullOrWhiteSpace(stageName) ? "Stage" : stageName;
        }

        public void SetStageIndex(int stageIndex)
        {
            if (stageIndexText == null)
                return;

            stageIndexText.text = $"Stage {Mathf.Max(1, stageIndex)}";
        }

        public void SetRequiredHoldSeconds(float seconds)
        {
            requiredHoldSeconds = Mathf.Max(0.05f, seconds);
        }

        public void Show()
        {
            fadeTween?.Kill();
            fadeTween = null;

            if (canvasGroup == null)
                return;

            gameObject.SetActive(true);
            canvasGroup.alpha = 0.0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (fadeDuration <= 0.0f)
            {
                canvasGroup.alpha = 1.0f;
                return;
            }

            fadeTween = canvasGroup.DOFade(1.0f, fadeDuration);
        }

        public async UniTask HideAsync()
        {
            fadeTween?.Kill();
            fadeTween = null;

            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (fadeDuration <= 0.0f)
            {
                canvasGroup.alpha = 0.0f;
                gameObject.SetActive(false);
                return;
            }

            fadeTween = canvasGroup.DOFade(0.0f, fadeDuration);
            await fadeTween.AsyncWaitForCompletion();
            gameObject.SetActive(false);
        }

        public void HideImmediate()
        {
            fadeTween?.Kill();
            fadeTween = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public async UniTask WaitForSkipHoldAsync(CancellationToken cancellationToken = default)
        {
            ResetHoldProgress();
            RefreshPromptIcon(forceRefresh: true);

            while (!cancellationToken.IsCancellationRequested)
            {
                RefreshPromptIcon();
                bool isPressed = IsSkipPressed();

                if (isPressed)
                {
                    holdSeconds += Time.deltaTime;
                    if (holdSeconds >= requiredHoldSeconds)
                    {
                        holdSeconds = requiredHoldSeconds;
                        UpdateProgressImage();
                        return;
                    }
                }
                else
                {
                    holdSeconds = 0.0f;
                }

                UpdateProgressImage();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private bool IsSkipPressed()
        {
            InputAction action = skipInputActionReference != null ? skipInputActionReference.action : null;
            bool actionPressed = action != null && action.IsPressed();

            // 画面全体の長押しでも同じスキップ進行にする。
            bool pointerPressed =
                (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);

            return actionPressed || pointerPressed;
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

            Sprite resolvedIcon = InputPromptIconResolver.ResolveIcon(inputManager, skipInputActionReference, fallbackActionIcon);
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
            if (holdProgressFillImage == null)
                return;

            float progress = Mathf.Clamp01(holdSeconds / Mathf.Max(0.05f, requiredHoldSeconds));
            holdProgressFillImage.fillAmount = progress;
        }
    }
}
