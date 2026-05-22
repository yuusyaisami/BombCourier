using BC.Base;
using BC.Manager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BC.UI
{
    // Retry の種類に応じて Reload / Reset を案内する UI。
    public class UIShowReloadStateMB : MonoBehaviour
    {
        [SerializeField][SerializeReference] private IAnimationSpriteClipSource reloadTextAnimClipSource;
        [SerializeField][SerializeReference] private IAnimationSpriteClipSource resetTextAnimClipSource;
        [SerializeField] private SpriteAnimationPlayerMB spriteAnimationPlayer;
        [SerializeField] private InputActionReference reloadInputActionReference;
        [SerializeField] private Slider reloadProgressSlider;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float stationarySpeedThreshold = 0.05f;
        [SerializeField, Min(0f)] private float stationaryPromptDelay = 1.5f;
        [SerializeField, Min(0.01f)] private float requiredHoldTime = 1.5f;

        private PlayerMoveController playerMoveController;
        private RetryActionMode shownRetryMode = RetryActionMode.None;
        private float reloadInputHoldTime;
        private float stationaryTimer;
        private bool isVisible;
        private bool isBoundToGameLogic;

        private void Start()
        {
            SetVisible(false, RetryActionMode.None, forceRefresh: true);
            TryBindGameLogic();
        }

        private void OnDestroy()
        {
            if (isBoundToGameLogic && GameLogicManagerMB.Instance != null)
            {
                GameLogicManagerMB.Instance.OnPlayerSpawned -= HandlePlayerSpawned;
            }
        }

        private void HandlePlayerSpawned(PlayerMB player)
        {
            playerMoveController = player != null ? player.PlayerMoveController : null;
            stationaryTimer = 0f;
        }

        private void Update()
        {
            TryBindGameLogic();

            GameLogicManagerMB gameLogic = GameLogicManagerMB.Instance;
            if (gameLogic == null || !gameLogic.TryGetRetryActionMode(out RetryActionMode retryMode))
            {
                reloadInputHoldTime = 0f;
                stationaryTimer = 0f;
                SetVisible(false, RetryActionMode.None);
                UpdateProgress();
                return;
            }

            bool isInputPressed = reloadInputActionReference != null &&
                                  reloadInputActionReference.action != null &&
                                  reloadInputActionReference.action.IsPressed();

            bool isStationary = IsPlayerStationary();
            stationaryTimer = isStationary ? stationaryTimer + Time.deltaTime : 0f;
            bool shouldForceShow = gameLogic.AreAllSceneBombsExploded();

            bool shouldShow = shouldForceShow || stationaryTimer >= stationaryPromptDelay || isInputPressed || reloadInputHoldTime > 0f;
            SetVisible(shouldShow, retryMode);

            if (!isVisible)
            {
                reloadInputHoldTime = 0f;
                UpdateProgress();
                return;
            }

            if (isInputPressed)
            {
                reloadInputHoldTime += Time.deltaTime;
                if (reloadInputHoldTime >= requiredHoldTime)
                {
                    reloadInputHoldTime = 0f;
                    UpdateProgress();
                    gameLogic.RequestRetryAction();
                    SetVisible(false, RetryActionMode.None, forceRefresh: true);
                    return;
                }
            }
            else
            {
                reloadInputHoldTime = 0f;
            }

            UpdateProgress();
        }

        private bool IsPlayerStationary()
        {
            if (playerMoveController == null)
                return false;

            return playerMoveController.CurrentVelocity.sqrMagnitude <= stationarySpeedThreshold * stationarySpeedThreshold;
        }

        private void SetVisible(bool visible, RetryActionMode retryMode, bool forceRefresh = false)
        {
            if (!forceRefresh && isVisible == visible && shownRetryMode == retryMode)
                return;

            isVisible = visible;
            shownRetryMode = visible ? retryMode : RetryActionMode.None;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (visible)
            {
                PlayPromptAnimation(retryMode);
            }
        }

        private void PlayPromptAnimation(RetryActionMode retryMode)
        {
            if (spriteAnimationPlayer == null)
                return;

            IAnimationSpriteClipSource clipSource = retryMode == RetryActionMode.ReloadCheckpoint
                ? reloadTextAnimClipSource
                : resetTextAnimClipSource;
            spriteAnimationPlayer.Play(clipSource, SpriteAnimationPlayMode.Once);
        }

        private void UpdateProgress()
        {
            if (reloadProgressSlider == null)
                return;

            float progress = Mathf.Clamp01(reloadInputHoldTime / requiredHoldTime);
            reloadProgressSlider.value = Mathf.SmoothStep(0f, 1f, progress);
        }

        private void TryBindGameLogic()
        {
            if (isBoundToGameLogic || GameLogicManagerMB.Instance == null)
                return;

            GameLogicManagerMB.Instance.OnPlayerSpawned += HandlePlayerSpawned;
            HandlePlayerSpawned(GameLogicManagerMB.Instance.PlayerInstance);
            isBoundToGameLogic = true;
        }
    }
}