using System.Threading;
using BC.Base;
using BC.Manager;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        [Header("Score Display")]
        [SerializeField] private Image clearStar; // スコアを表示するテキストコンポーネント
        [SerializeField] private Sprite clearStarSprite; // 0点のときに表示するスプライト

        [SerializeField] private TooltipTargetMB clearStarTooltipTarget; // スコアのツールチップを表示するための TooltipTargetMB コンポーネント
        [SerializeField] private Image bonusItemStar; // 1点のときに表示するスプライト
        [SerializeField] private Sprite bonusItemStarSprite; // 1点のときに表示するスプライト
        [SerializeField] private TooltipTargetMB bonusItemStarTooltipTarget; // ボーナスアイテムのツールチップを表示するための TooltipTargetMB コンポーネント
        [SerializeField] private Image fastClearStar; // 2点のときに
        [SerializeField] private Sprite fastClearStarSprite; // 2点のときに表示するスプライト
        [SerializeField] private TooltipTargetMB fastClearStarTooltipTarget; // 早いクリアのツールチップを表示するための TooltipTargetMB コンポーネント
        [SerializeField] private Sprite TransparentSprite; // 星がない状態を表す透明なスプライト

        [Header("Effects")]
        // ゴール時に再生する UI パーティクル（Canvas 上で動作するパーティクルシステムを割り当てる）
        [SerializeField] private UIFallEffectMB goalParticle;
        [SerializeField] private RectTransform stageClearPanel;
        [SerializeField] private Vector3 shakeStrength = new Vector3(10f, 10f, 0f); // シェイクの強さ


        [Header("Animation")]
        [SerializeField] private float revealDuration = 0.1f;
        [SerializeField] private float delayShowDuration = 0.5f;

        private CancellationTokenSource _cts;
        private SceneKernel sceneKernel;

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
            // SceneKernel取得
            sceneKernel = GetComponentInParent<SceneKernelMB>().Kernel;


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
            Debug.Log("Next Stage button clicked. Transitioning to next stage.");
        }

        private async UniTaskVoid ShowAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            await UniTask.Delay((int)(delayShowDuration * 1000), cancellationToken: _cts.Token);
            InputManagerMB.EnsureInstance().UnlockCursor();

            returnToTitleButton.gameObject.SetActive(false);
            nextStageButton.gameObject.SetActive(false);
            returnToTitleButton.interactable = false;
            nextStageButton.interactable = false;

            // starsを初期化
            ResetStars();

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



            // スコアに応じて星のスプライトとツールチップを設定
            // 最初の星はクリア時に確定でもらえます(頑張ったね賞)
            clearStar.rectTransform.localScale = new Vector3(2f, 2f, 2f);
            clearStar.sprite = clearStarSprite;
            clearStarTooltipTarget.TooltipText = "やりきることが大事！";
            await clearStar.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
            // 画面をシェイク
            await stageClearPanel.DOShakePosition(0.5f, shakeStrength).AsyncWaitForCompletion();
            await UniTask.Delay(300, cancellationToken: _cts.Token); // 0.3秒待機
            // 3つ目の星は早くクリアするともらえます(スピード賞)


            // 2つ目の星はボーナスアイテムを取るともらえます(ラッキー賞)
            if (sceneKernel.ValueStore.Get<bool>(GameLogicManagerMB.Instance.SelfEntityRef, ValueKeys.Kernel.Evaluation.IsBonusItem))
            {
                bonusItemStar.rectTransform.localScale = new Vector3(2f, 2f, 2f);
                bonusItemStar.sprite = bonusItemStarSprite;
                bonusItemStarTooltipTarget.TooltipText = "ボーナスアイテムをゲット！";

                await bonusItemStar.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
                await stageClearPanel.DOShakePosition(0.5f, shakeStrength).AsyncWaitForCompletion();
                await UniTask.Delay(300, cancellationToken: _cts.Token); // 0.3秒待機
            }
            else
            {
                bonusItemStar.sprite = TransparentSprite;
                bonusItemStarTooltipTarget.TooltipText = "マップ内にはバナパンがあります！探してみてください！";
            }
            // パーティクルを再生
            if (goalParticle != null)
            {
                goalParticle.StartFallEffect(FallEffectPlayMode.Loop);
            }
            float clearTime = sceneKernel.ValueStore.Get<float>(GameLogicManagerMB.Instance.SelfEntityRef, ValueKeys.Kernel.Evaluation.CountdownTime);
            float fastClearThreshold = sceneKernel.ValueStore.Get<float>(GameLogicManagerMB.Instance.SelfEntityRef, ValueKeys.Kernel.Evaluation.FastClearThreshold);
            // 3つ目の星は早くクリアするともらえます(スピード賞)
            if (sceneKernel.ValueStore.Get<bool>(GameLogicManagerMB.Instance.SelfEntityRef, ValueKeys.Kernel.Evaluation.IsFastClear))
            {
                fastClearStar.rectTransform.localScale = new Vector3(2f, 2f, 2f);
                fastClearStar.sprite = fastClearStarSprite;

                // 詳細な条件は ValueStore から取得してツールチップに反映する
                fastClearStarTooltipTarget.TooltipText = "爆弾のFuseタイムが短いともらえるスターです！\n今回のクリアタイム: " + clearTime.ToString("F2") + "秒\n早いクリアの条件: " + fastClearThreshold.ToString("F2") + "秒以下";

                await fastClearStar.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
                await stageClearPanel.DOShakePosition(0.5f, shakeStrength).AsyncWaitForCompletion();
                await UniTask.Delay(300, cancellationToken: _cts.Token); // 0.3秒待機
            }
            else
            {
                fastClearStar.sprite = TransparentSprite;
                fastClearStarTooltipTarget.TooltipText = "爆弾のFuseタイムが短いともらえるスターです！\n今回のクリアタイム: " + clearTime.ToString("F2") + "秒\n早いクリアの条件: " + fastClearThreshold.ToString("F2") + "秒以下";
            }
            returnToTitleButton.transform.localScale = new Vector3(1f, 0f, 1f);
            returnToTitleButton.gameObject.SetActive(true);
            // 原則待たない
            await returnToTitleButton.transform.DOScale(new Vector3(1f, 1f, 1f), 0.1f).SetEase(Ease.OutBack);

            nextStageButton.transform.localScale = new Vector3(1f, 0f, 1f);
            nextStageButton.gameObject.SetActive(true);
            await nextStageButton.transform.DOScale(new Vector3(1f, 1f, 1f), 0.1f).SetEase(Ease.OutBack);



            transform.localScale = Vector3.one;
            returnToTitleButton.interactable = true;
            nextStageButton.interactable = true;
        }
        private async UniTaskVoid HideAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // Y スケールを 1 → 0 にアニメーション
            float elapsed = 0f;
            float duration = Mathf.Max(revealDuration, 0.001f);

            // パーティクルを停止
            if (goalParticle != null)
            {
                goalParticle.EndFallEffect();
            }

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
            returnToTitleButton.interactable = false;
            nextStageButton.interactable = false;

            // 評価の星をリセット
            ResetStars();
        }
        private void ResetStars()
        {
            clearStar.sprite = TransparentSprite;
            clearStarTooltipTarget.TooltipText = "";
            bonusItemStar.sprite = TransparentSprite;
            bonusItemStarTooltipTarget.TooltipText = "";
            fastClearStar.sprite = TransparentSprite;
            fastClearStarTooltipTarget.TooltipText = "";
        }
    }
}
