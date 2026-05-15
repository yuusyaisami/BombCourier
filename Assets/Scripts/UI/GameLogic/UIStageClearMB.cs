using System.Threading;
using BC.Manager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    // ステージクリア時に表示するUIを管理するクラス。
    // GameState が Goaling に遷移したタイミングで表示され、
    // ルート Transform の Y スケールを 0 → 1 にアニメーションする。
    // ボタンは左が「タイトルに戻る」、右が「次のステージ」。
    public class UIStageClearMB : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private Button nextStageButton;

        [Header("Effects")]
        // ゴール時に再生する UI パーティクル（Canvas 上で動作するパーティクルシステムを割り当てる）
        [SerializeField] private ParticleSystem goalParticle;

        [Header("Animation")]
        [SerializeField] private float revealDuration = 0.1f;

        private CancellationTokenSource _cts;

        private void Awake()
        {
            // ゲーム開始時のスケールを (1, 0, 1) に設定して非表示状態にする
            transform.localScale = new Vector3(1f, 0f, 1f);
            returnToTitleButton.gameObject.SetActive(false);
            nextStageButton.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (GameStateManagerMB.Instance == null)
            {
                Debug.LogError($"{nameof(UIStageClearMB)}: GameStateManagerMB.Instance is null.", this);
                return;
            }

            GameStateManagerMB.Instance.StateMachine.Subscribe(OnGameStateChanged);
            // ボタンのクリックイベントにリスナーを登録
            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.AddListener(OnReturnToTitleButtonClicked);
            }
            if (nextStageButton != null)
            {
                nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (GameStateManagerMB.Instance != null)
            {
                GameStateManagerMB.Instance.StateMachine.Unsubscribe(OnGameStateChanged);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Goaling)
            {
                ShowAsync().Forget();
            }
        }

        private void OnReturnToTitleButtonClicked()
        {
            HideAsync().Forget();
            GameStateManagerMB.Instance.StateMachine.ChangeState(GameState.ReturnToTitle);
        }
        private void OnNextStageButtonClicked()
        {
            HideAsync().Forget();
            GameStateManagerMB.Instance.StateMachine.ChangeState(GameState.NextStage);
        }

        private async UniTaskVoid ShowAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            returnToTitleButton.gameObject.SetActive(true);
            nextStageButton.gameObject.SetActive(true);

            // パーティクルを再生
            if (goalParticle != null)
            {
                goalParticle.Play();
            }

            // Y スケールを 0 → 1 にアニメーション
            float elapsed = 0f;
            float duration = Mathf.Max(revealDuration, 0.001f);

            try
            {
                while (elapsed < duration)
                {
                    float t = Mathf.Clamp01(elapsed / duration);
                    transform.localScale = new Vector3(1f, t, 1f);
                    elapsed += Time.unscaledDeltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, _cts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            transform.localScale = Vector3.one;
        }
        private async UniTaskVoid HideAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // Y スケールを 1 → 0 にアニメーション
            float elapsed = 0f;
            float duration = Mathf.Max(revealDuration, 0.001f);

            try
            {
                while (elapsed < duration)
                {
                    float t = Mathf.Clamp01(elapsed / duration);
                    transform.localScale = new Vector3(1f, 1f - t, 1f);
                    elapsed += Time.unscaledDeltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, _cts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            transform.localScale = new Vector3(1f, 0f, 1f);
        }
    }
}
