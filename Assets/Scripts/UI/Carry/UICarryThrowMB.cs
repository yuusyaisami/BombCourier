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
        private CancellationTokenSource tokenSource;
        private void Reset()
        {
            throwPowerSlider = GetComponentInChildren<Slider>();
        }

        private void Awake()
        {
            canvasGroup.alpha = 0f;
        }

        void Start()
        {
            // PlayerMBはGameLogicから
            SetPlayerMB(GameLogicManagerMB.Instance.PlayerInstance);
            GameLogicManagerMB.Instance.OnPlayerSpawned += SetPlayerMB;
        }
        void OnDestroy()
        {
            GameLogicManagerMB.Instance.OnPlayerSpawned -= SetPlayerMB;
            tokenSource?.Cancel();
            tokenSource?.Dispose();
            tokenSource = null;
        }
        private void Update()
        {

        }
        private void SetPlayerMB(PlayerMB player)
        {
            if (player != null)
            {
                itemHandState = player.GetComponent<PlayerItemHandleStateMB>();
                if (itemHandState != null)
                {
                    // PlayerMBのイベントにスライダー更新関数を登録
                    itemHandState.OnThrowChargeStart += StartThrowCharge;
                    itemHandState.OnThrowChargeEnd += EndThrowCharge;
                }
            }
            else
            {
                // 解除
                EndThrowCharge();
                itemHandState = null;
            }
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
            Debug.Log("StartThrowCharge called. CurrentThrowChargeRatio: " + itemHandState.CurrentThrowChargeRatio);
            throwPowerSlider.value = 0f;
            canvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutSine);
            tokenSource?.Cancel(); // 既存の更新タスクがあればキャンセル
            tokenSource = new CancellationTokenSource();
            UpdateThrowPowerSlider(tokenSource.Token);
        }
        private void EndThrowCharge()
        {
            canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.OutSine);
            throwPowerSlider.value = 0f;
            tokenSource?.Cancel();
            tokenSource = null;
        }
    }
}