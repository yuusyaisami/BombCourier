using System.Threading;
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
        private CancellationTokenSource tokenSource;
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
            tokenSource?.Cancel();
            tokenSource?.Dispose();
            tokenSource = null;
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
        private async void UpdateThrowPowerSlider(CancellationToken token)
        {
            while (isThrowing && !token.IsCancellationRequested)
            {
                throwPowerSlider.value = itemHandState.CurrentThrowChargeRatio;
                await System.Threading.Tasks.Task.Yield();
            }
        }

        private void StartThrowCharge()
        {
            throwPowerSlider.value = 0f;
            SyncVisibility(true);
            tokenSource?.Cancel(); // 既存の更新タスクがあればキャンセル
            tokenSource = new CancellationTokenSource();
            UpdateThrowPowerSlider(tokenSource.Token);
        }
        private void EndThrowCharge()
        {
            SyncVisibility(false);
            throwPowerSlider.value = 0f;
            tokenSource?.Cancel();
            tokenSource = null;
        }

        private void SyncVisibility(bool visible)
        {
            if (canvasGroup == null || isVisible == visible)
                return;

            isVisible = visible;
            canvasGroup.DOFade(visible ? 1f : 0f, 0.2f).SetEase(Ease.OutSine);
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