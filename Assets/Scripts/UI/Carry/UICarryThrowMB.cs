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
