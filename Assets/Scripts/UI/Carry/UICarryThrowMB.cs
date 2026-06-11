using BC.Base;
using BC.Manager;
using BC.Player;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace BC.UI
{
    // キャリーアイテムを投げる際その強さをUIで表示するためのクラス。
    public class UICarryThrowMB : MonoBehaviour
    {

        [SerializeField] private Slider throwPowerSlider;
        [SerializeField] private CanvasGroup canvasGroup;
        private PlayerItemHandleStateMB itemHandState;
        private bool isThrowing => itemHandState != null && itemHandState.IsThrowCharging;
        private bool isVisible;
        // 表示/非表示 tween はこの UI が所有する。
        // Player 差し替えや Destroy 後に DOTween 側の work が残らないよう、必ず kill する。
        private Tween visibilityTween;
        private void Reset()
        {
            throwPowerSlider = GetComponentInChildren<Slider>();
        }

        private void Awake()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        void Start()
        {
            // PlayerMBはGameLogicから
            GameLogicManagerMB gameLogic = GameLogicManagerMB.Instance;
            if (gameLogic != null)
            {
                SetPlayerMB(gameLogic.PlayerInstance);
                gameLogic.OnPlayerSpawned += SetPlayerMB;
            }
        }
        void OnDestroy()
        {
            GameLogicManagerMB gameLogic = GameLogicManagerMB.Instance;
            if (gameLogic != null)
                gameLogic.OnPlayerSpawned -= SetPlayerMB;

            UnbindItemHandleState();
            visibilityTween?.Kill();
            visibilityTween = null;
        }
        private void Update()
        {
            if (itemHandState == null)
            {
                SyncVisibility(false);
                return;
            }

            // slider 値は毎 frame の状態同期に一本化する。
            // async ループとイベント更新を併用すると、Player 差し替え時に古い state を読み続けやすい。
            throwPowerSlider.value = itemHandState.CurrentThrowChargeRatio;
            SyncVisibility(isThrowing);
        }
        private void SetPlayerMB(PlayerMB player)
        {
            UnbindItemHandleState();

            itemHandState = player != null ? player.GetComponent<PlayerItemHandleStateMB>() : null;

            if (itemHandState != null)
            {
                // Player差し替え後も表示イベントを取りこぼさないよう毎回再登録する。
                itemHandState.OnThrowChargeStart += StartThrowCharge;
                itemHandState.OnThrowChargeEnd += EndThrowCharge;
            }

            SyncVisibility(isThrowing);
        }
        private void StartThrowCharge()
        {
            throwPowerSlider.value = 0f;
            SyncVisibility(true);
        }
        private void EndThrowCharge()
        {
            SyncVisibility(false);
            throwPowerSlider.value = 0f;
        }

        private void SyncVisibility(bool visible)
        {
            if (canvasGroup == null || isVisible == visible)
                return;

            isVisible = visible;
            // 連続した start/end では前の fade が残るため、最新の表示要求だけを有効にする。
            visibilityTween?.Kill();
            visibilityTween = canvasGroup
                .DOFade(visible ? 1f : 0f, 0.2f)
                .SetEase(Ease.OutSine)
                .OnComplete(() => visibilityTween = null);
        }

        private void UnbindItemHandleState()
        {
            if (itemHandState != null)
            {
                itemHandState.OnThrowChargeStart -= StartThrowCharge;
                itemHandState.OnThrowChargeEnd -= EndThrowCharge;
            }

            itemHandState = null;
        }
    }
}
