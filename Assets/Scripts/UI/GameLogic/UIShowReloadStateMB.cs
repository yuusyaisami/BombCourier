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
        [SerializeField, Min(0.01f)] private float autoReloadFadeInDuration = 0.25f;

        private PlayerMoveController playerMoveController;
        private RetryActionMode shownRetryMode = RetryActionMode.None;
        private float reloadInputHoldTime;
        private float stationaryTimer;
        private float autoReloadFadeTimer;
        private bool isVisible;
        private bool isBoundToGameLogic;
        private bool wasAutoReloadVisible;
        private bool autoReloadFadeActive;

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
                autoReloadFadeTimer = 0f;
                autoReloadFadeActive = false;
                wasAutoReloadVisible = false;
                SetVisible(false, RetryActionMode.None);
                UpdateProgress();
                return;
            }

            bool isInputPressed = reloadInputActionReference != null &&
                                  reloadInputActionReference.action != null &&
                                  reloadInputActionReference.action.IsPressed();

            bool isPlayerDead = IsPlayerDead();
            bool isSpecialPresentation = IsRetryBlockedBySpecialPresentation();
            bool isResetMode = retryMode == RetryActionMode.ResetStage;
            bool isReloadMode = retryMode == RetryActionMode.ReloadCheckpoint;
            bool isInputLockedByPresentation = !isPlayerDead && playerMoveController != null && !playerMoveController.CanMoveByInput;

            bool canDriveRetryByInput = !isSpecialPresentation && !isInputLockedByPresentation;
            bool canShowResetByStationary =
                isResetMode &&
                !gameLogic.HasStartedAnyBombFuseThisStage &&
                canDriveRetryByInput;

            if (canShowResetByStationary && IsPlayerStationary())
                stationaryTimer += Time.deltaTime;
            else
                stationaryTimer = 0f;

            bool shouldAutoShowReset = canShowResetByStationary && stationaryTimer >= stationaryPromptDelay;
            bool shouldAutoShowReload = isReloadMode && gameLogic.AreAllSceneBombsExploded();

            if (!wasAutoReloadVisible && shouldAutoShowReload)
            {
                autoReloadFadeActive = true;
                autoReloadFadeTimer = 0f;
            }
            else if (!shouldAutoShowReload)
            {
                autoReloadFadeActive = false;
                autoReloadFadeTimer = 0f;
            }

            wasAutoReloadVisible = shouldAutoShowReload;

            bool shouldShowByDeath = isPlayerDead && !isSpecialPresentation;
            bool shouldShow = shouldShowByDeath || shouldAutoShowReset || shouldAutoShowReload || (canDriveRetryByInput && (isInputPressed || reloadInputHoldTime > 0f));
            SetVisible(shouldShow, retryMode);

            if (!canDriveRetryByInput)
            {
                reloadInputHoldTime = 0f;
                UpdateProgress();
                UpdateAutoReloadFade();
                return;
            }

            if (!isVisible)
            {
                reloadInputHoldTime = 0f;
                UpdateProgress();
                UpdateAutoReloadFade();
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
            UpdateAutoReloadFade();
        }

        private bool IsRetryBlockedBySpecialPresentation()
        {
            GameStateManagerMB stateManager = GameStateManagerMB.Instance;
            if (stateManager == null)
                return true;

            if (stateManager.CurrentState == GameState.Intro ||
                stateManager.CurrentState == GameState.Goaling ||
                stateManager.CurrentState == GameState.NextStage ||
                stateManager.CurrentState == GameState.Loading ||
                stateManager.CurrentState == GameState.Starting ||
                stateManager.CurrentState == GameState.Reload ||
                stateManager.CurrentState == GameState.ResetStage)
            {
                return true;
            }

            return false;
        }

        private bool IsPlayerDead()
        {
            if (playerMoveController == null || playerMoveController.MoveMotor == null)
                return false;

            return playerMoveController.MoveMotor.IsDead;
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
                if (!visible)
                    canvasGroup.alpha = 0f;
                else if (!autoReloadFadeActive)
                    canvasGroup.alpha = 1f;

                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (visible)
            {
                PlayPromptAnimation(retryMode);
            }
        }

        private void UpdateAutoReloadFade()
        {
            if (!autoReloadFadeActive || canvasGroup == null || !isVisible)
                return;

            autoReloadFadeTimer += Time.deltaTime;
            float duration = Mathf.Max(0.01f, autoReloadFadeInDuration);
            float t = Mathf.Clamp01(autoReloadFadeTimer / duration);
            canvasGroup.alpha = t;

            if (t >= 1f)
                autoReloadFadeActive = false;
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