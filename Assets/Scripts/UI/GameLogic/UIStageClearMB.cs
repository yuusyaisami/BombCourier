using System.Threading;
using BC.ActionSystem;
using BC.Base;
using BC.Manager;
using BC.UI.Components;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Febucci.TextAnimatorForUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        [SerializeField] private UIButtonMB returnToTitleButton;
        [SerializeField] private UIButtonMB nextStageButton;
        [SerializeField] private UIButtonMB replayButton; // もう一度このステージを遊ぶ
        [Header("Score Display")]
        [SerializeField] private Image clearIcon; // クリアアイコンを表示する Image コンポーネント
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

        [Header("Text")]
        [SerializeField] private TypewriterComponent stageClearText; // ステージクリアのテキスト
        [SerializeField] private string stageClearMessage = "STAGE {0} REPORT!"; // ステージクリアメッセージのフォーマット

        [Header("Effects")]
        // ゴール時に再生する UI パーティクル（Canvas 上で動作するパーティクルシステムを割り当てる）
        [SerializeField] private UIFallEffectMB goalParticle;
        [SerializeField] private RectTransform stageClearPanel;
        [SerializeField] private Vector3 shakeStrength = new Vector3(10f, 10f, 0f); // シェイクの強さ


        [Header("Animation")]
        [SerializeField] private float revealDuration = 0.1f;
        [SerializeField] private float delayShowDuration = 0.5f;

        [Header("Actions")]
        [SerializeField] private InlineAction onFadeInAction;
        [SerializeField] private InlineAction onClearStarAction;
        [SerializeField] private InlineAction onBonusItemStarAction;
        [SerializeField] private InlineAction onFastClearStarAction;
        [SerializeField] private InlineAction onReturnToTitleFocusAction;
        [SerializeField] private InlineAction onReturnToTitleClickAction;
        [SerializeField] private InlineAction onNextStageFocusAction;
        [SerializeField] private InlineAction onNextStageClickAction;
        [SerializeField] private InlineAction onReplayFocusAction;
        [SerializeField] private InlineAction onReplayClickAction;

        private CancellationTokenSource _cts;
        private SceneKernel sceneKernel;

        // ボタンが表示・操作可能な状態かを示すフラグ。
        // InputAction 直接購読で Submit/Cancel を処理するためのガード。
        private bool _showingButtons;
        private InputAction _submitAction;
        private InputAction _cancelAction;
        private InputAction _navigateAction;

        private void OnEnable()
        {
            InputActionAsset asset = InputSystem.actions;
            if (asset != null)
            {
                asset.FindActionMap("UI", throwIfNotFound: false)?.Enable();
                _submitAction = asset.FindAction("UI/Submit", throwIfNotFound: false);
                _cancelAction = asset.FindAction("UI/Cancel", throwIfNotFound: false);
                _navigateAction = asset.FindAction("UI/Navigate", throwIfNotFound: false);
            }

            if (_submitAction != null) _submitAction.performed += OnSubmitInputPerformed;
            if (_cancelAction != null) _cancelAction.performed += OnCancelInputPerformed;
            if (_navigateAction != null) _navigateAction.performed += OnNavigateInputPerformed;
        }

        private void OnDisable()
        {
            if (_submitAction != null) _submitAction.performed -= OnSubmitInputPerformed;
            if (_cancelAction != null) _cancelAction.performed -= OnCancelInputPerformed;
            if (_navigateAction != null) _navigateAction.performed -= OnNavigateInputPerformed;
            _showingButtons = false;
        }

        private void OnSubmitInputPerformed(InputAction.CallbackContext ctx)
        {
            if (!_showingButtons) return;

            // EventSystem で選択されているボタンを優先、なければデフォルトボタンをクリック
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            UIButtonMB target = null;

            if (selected != null)
            {
                if (nextStageButton != null && nextStageButton.IsSelectionTarget(selected)) target = nextStageButton;
                else if (replayButton != null && replayButton.IsSelectionTarget(selected)) target = replayButton;
                else if (returnToTitleButton != null && returnToTitleButton.IsSelectionTarget(selected)) target = returnToTitleButton;
            }

            if (target == null)
                target = nextStageButton != null ? nextStageButton : (replayButton != null ? replayButton : returnToTitleButton);

            if (target == null || !target.Interactable) return;

            ExecuteEvents.Execute(
                target.UnityButton.gameObject,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
        }

        private void OnCancelInputPerformed(InputAction.CallbackContext ctx)
        {
            if (!_showingButtons) return;
            OnReturnToTitleButtonClicked();
        }

        private void OnNavigateInputPerformed(InputAction.CallbackContext ctx)
        {
            if (!_showingButtons) return;
            Vector2 dir = ctx.ReadValue<Vector2>();
            if (Mathf.Abs(dir.x) < 0.5f) return;

            // 表示中の非 null ボタン一覧を順に並べてカーソル移動する
            var buttons = new System.Collections.Generic.List<UIButtonMB>();
            if (returnToTitleButton != null) buttons.Add(returnToTitleButton);
            if (replayButton != null) buttons.Add(replayButton);
            if (nextStageButton != null) buttons.Add(nextStageButton);
            if (buttons.Count < 2) return;

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            int currentIndex = -1;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].IsSelectionTarget(selected)) { currentIndex = i; break; }
            }

            int next = (currentIndex + (dir.x > 0 ? 1 : -1) + buttons.Count) % buttons.Count;
            buttons[next].Select();
        }

        private void Awake()
        {
            // ゲーム開始時のスケールを (1, 0, 1) に設定して非表示状態にする
            transform.localScale = new Vector3(1f, 0f, 1f);
            stageClearPanel ??= transform as RectTransform;
            SetButtonGameObjectActive(returnToTitleButton, false);
            SetButtonGameObjectActive(nextStageButton, false);
            SetButtonGameObjectActive(replayButton, false);
        }

        private void Start()
        {
            if (GameStateManagerMB.Instance == null)
            {
                Debug.LogError($"{nameof(UIStageClearMB)}: GameStateManagerMB.Instance is null.", this);
                return;
            }
            // SceneKernel取得
            SceneKernelMB sceneKernelMB = GetComponentInParent<SceneKernelMB>();
            if (sceneKernelMB == null)
            {
                Debug.LogError($"{nameof(UIStageClearMB)}: {nameof(SceneKernelMB)} could not be resolved from parent hierarchy.", this);
                return;
            }

            sceneKernel = sceneKernelMB.Kernel;


            GameStateManagerMB.Instance.StateMachine.Subscribe(OnGameStateChanged);

            if (returnToTitleButton != null)
            {
                returnToTitleButton.RemoveClickListener(OnReturnToTitleButtonClicked);
                returnToTitleButton.AddClickListener(OnReturnToTitleButtonClicked);
                returnToTitleButton.Focused -= OnReturnToTitleButtonFocused;
                returnToTitleButton.Focused += OnReturnToTitleButtonFocused;
            }

            if (nextStageButton != null)
            {
                nextStageButton.RemoveClickListener(OnNextStageButtonClicked);
                nextStageButton.AddClickListener(OnNextStageButtonClicked);
                nextStageButton.Focused -= OnNextStageButtonFocused;
                nextStageButton.Focused += OnNextStageButtonFocused;
            }

            if (replayButton != null)
            {
                replayButton.RemoveClickListener(OnReplayButtonClicked);
                replayButton.AddClickListener(OnReplayButtonClicked);
                replayButton.Focused -= OnReplayButtonFocused;
                replayButton.Focused += OnReplayButtonFocused;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (returnToTitleButton != null)
            {
                returnToTitleButton.RemoveClickListener(OnReturnToTitleButtonClicked);
                returnToTitleButton.Focused -= OnReturnToTitleButtonFocused;
            }

            if (nextStageButton != null)
            {
                nextStageButton.RemoveClickListener(OnNextStageButtonClicked);
                nextStageButton.Focused -= OnNextStageButtonFocused;
            }

            if (replayButton != null)
            {
                replayButton.RemoveClickListener(OnReplayButtonClicked);
                replayButton.Focused -= OnReplayButtonFocused;
            }

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
            ExecuteInlineAction(onReturnToTitleClickAction);
            HideAsync().Forget();
            GameStateManagerMB.Instance.StateMachine.ChangeState(GameState.ReturnToTitle);
        }

        private void OnNextStageButtonClicked()
        {
            ExecuteInlineAction(onNextStageClickAction);
            HideAsync().Forget();
            GameStateManagerMB.Instance.StateMachine.ChangeState(GameState.NextStage);
        }

        private void OnReplayButtonClicked()
        {
            ExecuteInlineAction(onReplayClickAction);
            HideAsync().Forget();
            // 同じステージをイントロ無しで最初からやり直す。
            GameStateManagerMB.Instance.StateMachine.ChangeState(GameState.ResetStage);
        }

        private void OnReturnToTitleButtonFocused(UIButtonMB button)
        {
            ExecuteInlineAction(onReturnToTitleFocusAction);
        }

        private void OnNextStageButtonFocused(UIButtonMB button)
        {
            ExecuteInlineAction(onNextStageFocusAction);
        }

        private void OnReplayButtonFocused(UIButtonMB button)
        {
            ExecuteInlineAction(onReplayFocusAction);
        }

        // sceneKernel がある場合は GameLogicManager の EntityRef を actor にして InlineAction を実行する。
        private void ExecuteInlineAction(InlineAction action)
        {
            if (action == null) return;
            EntityRef actor = GameLogicManagerMB.Instance != null
                ? GameLogicManagerMB.Instance.SelfEntityRef
                : default;
            InlineActionExecutionUtility.ExecuteAndForget(this, actor, action);
        }

        private async UniTaskVoid ShowAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            await UniTask.Delay((int)(delayShowDuration * 1000), cancellationToken: _cts.Token);
            InputManagerMB.EnsureInstance().UnlockCursor();

            // パネルが表示される前に InlineAction を実行する。
            ExecuteInlineAction(onFadeInAction);

            SetButtonGameObjectActive(returnToTitleButton, false);
            SetButtonGameObjectActive(nextStageButton, false);
            SetButtonGameObjectActive(replayButton, false);
            SetButtonInteractable(returnToTitleButton, false);
            SetButtonInteractable(nextStageButton, false);
            SetButtonInteractable(replayButton, false);

            // starsを初期化
            ResetStars();

            // ステージクリアのテキストを設定
            if (stageClearText != null)
            {
                stageClearText.ShowText(string.Format(stageClearMessage, GameLogicManagerMB.Instance.CurrentStageIndex + 1));
            }

            // clearIcon をアニメーと表示
            if (clearIcon != null)
            {
                clearIcon.transform.localScale = new Vector3(1f, 0f, 1f);
                clearIcon.gameObject.SetActive(true);
                await clearIcon.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
            }

            // パーティクルを再生

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

            // ループは elapsed を加算してから抜けるため最終 t が 1 未満で終わる。
            // 星演出・シェイクの前にここで必ず等倍へ確定させ、0.9 付近のまま固定→末尾で急に 1 になる挙動を防ぐ。
            transform.localScale = Vector3.one;

            // スコアに応じて星のスプライトとツールチップを設定
            // 最初の星はクリア時に確定でもらえます(頑張ったね賞)
            clearStar.rectTransform.localScale = new Vector3(2f, 2f, 2f);
            clearStar.sprite = clearStarSprite;
            clearStarTooltipTarget.TooltipText = "やりきることが大事！";
            ExecuteInlineAction(onClearStarAction);
            stageClearPanel.DOShakePosition(0.5f, shakeStrength).ToUniTask().Forget();
            await clearStar.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
            // 画面をシェイク
            await UniTask.Delay(300, cancellationToken: _cts.Token); // 0.3秒待機
            // 3つ目の星は早くクリアするともらえます(スピード賞)


            // 2つ目の星はボーナスアイテムを取るともらえます(ラッキー賞)
            if (sceneKernel.ValueStore.Get<bool>(GameLogicManagerMB.Instance.SelfEntityRef, ValueKeys.Kernel.Evaluation.IsBonusItem))
            {
                bonusItemStar.rectTransform.localScale = new Vector3(2f, 2f, 2f);
                bonusItemStar.sprite = bonusItemStarSprite;
                bonusItemStarTooltipTarget.TooltipText = "ボーナスアイテムをゲット！";
                ExecuteInlineAction(onBonusItemStarAction);
                stageClearPanel.DOShakePosition(0.5f, shakeStrength).ToUniTask().Forget();
                await bonusItemStar.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
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
                ExecuteInlineAction(onFastClearStarAction);
                stageClearPanel.DOShakePosition(0.5f, shakeStrength).ToUniTask().Forget();
                await fastClearStar.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
                await UniTask.Delay(300, cancellationToken: _cts.Token); // 0.3秒待機
            }
            else
            {
                fastClearStar.sprite = TransparentSprite;
                fastClearStarTooltipTarget.TooltipText = "爆弾のFuseタイムが短いともらえるスターです！\n今回のクリアタイム: " + clearTime.ToString("F2") + "秒\n早いクリアの条件: " + fastClearThreshold.ToString("F2") + "秒以下";
            }
            await ShowButtonAsync(returnToTitleButton);
            await ShowButtonAsync(replayButton);
            await ShowButtonAsync(nextStageButton);



            transform.localScale = Vector3.one;
            SetButtonInteractable(returnToTitleButton, true);
            SetButtonInteractable(nextStageButton, true);
            SetButtonInteractable(replayButton, true);

            ConfigureButtonNavigation();
            UINavigationBootstrap.EnsureConfigured();
            _showingButtons = true;
            UIButtonMB defaultButton = nextStageButton != null ? nextStageButton
                : replayButton != null ? replayButton
                : returnToTitleButton;
            if (defaultButton != null)
                defaultButton.Select();
        }
        private async UniTaskVoid HideAsync()
        {
            _showingButtons = false;
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
            if (stageClearText != null)
            {
                stageClearText.ShowText(string.Empty);
            }
            if (clearIcon != null)
            {
                clearIcon.gameObject.SetActive(false);
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
            SetButtonInteractable(returnToTitleButton, false);
            SetButtonInteractable(nextStageButton, false);

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

        private void ConfigureButtonNavigation()
        {
            if (!TryResolveUnityButton(returnToTitleButton, out Button returnButton) ||
                !TryResolveUnityButton(nextStageButton, out Button nextButton))
            {
                return;
            }

            bool hasReplay = TryResolveUnityButton(replayButton, out Button replayBtn);

            if (hasReplay)
            {
                // 横並び3ボタン循環: [returnToTitle] [replay] [nextStage]
                Navigation nav;

                nav = returnToTitleButton.Navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = nextButton;
                nav.selectOnRight = replayBtn;
                nav.selectOnUp = nextButton;
                nav.selectOnDown = replayBtn;
                returnToTitleButton.Navigation = nav;

                nav = replayButton.Navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = returnButton;
                nav.selectOnRight = nextButton;
                nav.selectOnUp = returnButton;
                nav.selectOnDown = nextButton;
                replayButton.Navigation = nav;

                nav = nextStageButton.Navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = replayBtn;
                nav.selectOnRight = returnButton;
                nav.selectOnUp = replayBtn;
                nav.selectOnDown = returnButton;
                nextStageButton.Navigation = nav;
            }
            else
            {
                Navigation returnNavigation = returnToTitleButton.Navigation;
                returnNavigation.mode = Navigation.Mode.Explicit;
                returnNavigation.selectOnRight = nextButton;
                returnNavigation.selectOnLeft = nextButton;
                returnNavigation.selectOnUp = nextButton;
                returnNavigation.selectOnDown = nextButton;
                returnToTitleButton.Navigation = returnNavigation;

                Navigation nextNavigation = nextStageButton.Navigation;
                nextNavigation.mode = Navigation.Mode.Explicit;
                nextNavigation.selectOnRight = returnButton;
                nextNavigation.selectOnLeft = returnButton;
                nextNavigation.selectOnUp = returnButton;
                nextNavigation.selectOnDown = returnButton;
                nextStageButton.Navigation = nextNavigation;
            }
        }

        private async UniTask ShowButtonAsync(UIButtonMB button)
        {
            if (button == null)
                return;

            button.transform.localScale = new Vector3(1f, 0f, 1f);
            SetButtonGameObjectActive(button, true);
            await button.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
        }

        private static void SetButtonGameObjectActive(UIButtonMB button, bool active)
        {
            if (!TryResolveUnityButton(button, out Button unityButton))
                return;

            unityButton.gameObject.SetActive(active);
        }

        private static void SetButtonInteractable(UIButtonMB button, bool interactable)
        {
            if (button == null)
                return;

            button.Interactable = interactable;
        }

        private static bool TryResolveUnityButton(UIButtonMB button, out Button unityButton)
        {
            unityButton = button != null ? button.UnityButton : null;
            return button != null && unityButton != null;
        }
    }
}
