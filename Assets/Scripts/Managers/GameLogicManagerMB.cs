using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using BC.Animation;
using BC.Base;
using BC.Bomb;
using BC.Camera;
using BC.Gimmick;
using BC.Item;
using BC.Managers;
using BC.Player;
using BC.Stage;
using BC.UI;
using BC.UI.Title;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BC.Manager
{
    // ゲーム進行全体をまとめるメインの司令塔。
    // ステージ読み込み、プレイヤー生成、爆弾進行、ゴール演出、リトライ、UI への通知をここで束ねる。
    public enum RetryActionMode
    {
        None,
        ResetStage,
        ReloadCheckpoint,
    }

    // 個別の system に散らすより、ゲームの状態遷移を 1 か所で追いやすくするための管理 MonoBehaviour。
    public class GameLogicManagerMB : UnityEngine.MonoBehaviour
    {
        public static GameLogicManagerMB Instance { get; private set; }
        private void Awake()
        {
            // singleton 前提。重複があれば新しい方を破棄する。
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        [Header("References")]
        [SerializeField] private UIFadeEffectMB uiFadeEffectMB; // 画面フェードを担当する UI 側の制御。
        [SerializeField] private UIGameSceneManagerMB uiGameSceneManagerMB; // ゲーム中 UI 全体の表示/非表示を担う。
        [SerializeField] private EntityMB playerPrefab; // スポーン用プレイヤー prefab。
        [Header("Return To Title")]
        [SerializeField, Min(0)] private int titleSceneBuildIndex = 0; // クリア後に戻る title scene の build index。
        [Header("Debug")][SerializeField] private Transform debugStageInstance; // デバッグ用に直接参照する stage instance。
        [SerializeField, Min(0)] private int debugStartStageIndex = 0; // 起動時にロードする stage index。
        // 現在のゲーム進行にぶら下がる主要参照群。
        private BombMB currentBomb;

        private GodHandObjectMB currentGodHand; // つかまっている GodHand の参照。
        private MapRuntimeMB currentMapRuntime; // 現在ロード中の map runtime。
        private GameObject stageInstance; // 現在の stage instance。
        private PlayerMB playerInstance; // 現在の player instance。
        private GoalData currentGoalData; // 現在のゴールデータ。
        private string currentStageName; // 現在の stage 名。
        private CameraPathSequenceAuthoringMB currentCameraPath; // 現在の camera path。
        private EntityRef playerRef; // player の EntityRef。
        public Action<PlayerMB> OnPlayerSpawned; // player spawn 通知。
        public Action<PlayerMB> OnPlayerUpdated; // player 参照更新通知。
        public Action<BombMB> OnCurrentBombChanged; // current bomb 変更通知。
        public Action<BombMB> OnStartBombFuse; // fuse 開始通知。
        public Action OnEndBombFuse; // fuse 終了通知。
        public Action ReloadState; // reload 要求通知。
        public Action ExplodedState; // 爆発通知。
        public Action ExplodedBeforeGoalOpenedState; // ゴール前爆発通知。
        private SceneKernel sceneKernel; // Scene 全体で共有する kernel。
        private float timeSinceStartBomb; // 爆弾開始からの経過時間。
        private float currentClearTimeThreshold = 60f; // fast clear 判定の閾値。
        private EntityRef gameLogicManagerRef; // 自身の EntityRef。
        public EntityRef SelfEntityRef => gameLogicManagerRef; // 外部参照用の self entity。
        private BonusObjectMB currentBonusObject; // 現在の bonus object。
        private int currentGameStage;
        private readonly Stack<RetryCheckpointSnapshot> retryCheckpointStack = new();
        private bool resetArmed;
        private bool hasStartedAnyBombFuseThisStage;
        private bool isShuttingDown;
        private bool introPathSkipRequested;

        public BombMB CurrentBomb => currentBomb;
        public PlayerMB PlayerInstance => playerInstance;
        public int CurrentStageIndex => currentGameStage;
        public RetryActionMode CurrentRetryActionMode => ResolveRetryActionMode();
        public bool HasRetryCheckpoint => retryCheckpointStack.Count > 0;
        public bool HasStartedAnyBombFuseThisStage => hasStartedAnyBombFuseThisStage || HasAnySceneBombFuseStartedOrExploded();

        // 既存 UI 互換: ステージ内の爆弾がすべて爆発済みかを返す。
        public bool AreAllSceneBombsExploded()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return false;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                BombMB bomb = currentMapRuntime.Bombs[i];

                // Destroy 済みで null になった参照は exploded 扱いにする。
                if (bomb == null)
                    continue;

                if (!bomb.HasExploded)
                    return false;
            }

            return true;
        }

        public bool HasAnyActiveSceneBomb()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return false;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                BombMB bomb = currentMapRuntime.Bombs[i];

                if (bomb != null && !bomb.HasExploded)
                    return true;
            }

            return false;
        }

        public bool HasAnyFusingSceneBomb()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return false;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                BombMB bomb = currentMapRuntime.Bombs[i];

                if (bomb != null && bomb.FuseStarted && !bomb.HasExploded)
                    return true;
            }

            return false;
        }

        // 現在の状態から、どのリトライ操作を提示すべきかを返す。
        public bool TryGetRetryActionMode(out RetryActionMode mode)
        {
            mode = RetryActionMode.None;

            if (!IsRetryActionAvailable())
                return false;

            mode = ResolveRetryActionMode();
            return mode != RetryActionMode.None;
        }

        // checkpoint があれば ReloadCheckpoint を優先し、なければ ResetStage を返す。
        private RetryActionMode ResolveRetryActionMode()
        {
            if (retryCheckpointStack.Count > 0)
                return RetryActionMode.ReloadCheckpoint;

            return resetArmed ? RetryActionMode.ResetStage : RetryActionMode.None;
        }

        // リトライを許可するゲーム状態かを判断する。
        public bool IsRetryActionAvailable()
        {
            GameStateManagerMB stateManager = GameStateManagerMB.Instance;
            if (stateManager == null)
                return false;

            return stateManager.CurrentState == GameState.SetupPlaying ||
                   stateManager.CurrentState == GameState.FusePlaying ||
                   stateManager.CurrentState == GameState.Exploded;
        }

        // UI などからリトライを要求されたときの入口。
        public void RequestRetryAction()
        {
            if (!TryGetRetryActionMode(out RetryActionMode mode) || GameStateManagerMB.Instance == null)
                return;

            GameStateManagerMB.Instance.ChangeState(
                mode == RetryActionMode.ReloadCheckpoint
                    ? GameState.Reload
                    : GameState.ResetStage);
        }

        // 爆弾取得前の snapshot を積む。リロード時にこの snapshot を使って戻る。
        public void CaptureRetryCheckpointBeforeBombPickup(BombMB bomb)
        {
            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            StageManagerMB stageManager = StageManagerMB.Instance;
            BombMB targetBomb = bomb != null ? bomb : currentBomb;

            if (resolvedPlayer == null || stageManager == null || targetBomb == null)
            {
                Debug.LogError($"{nameof(GameLogicManagerMB)}: retry checkpoint capture prerequisites are missing.", this);
                return;
            }
            // リトライチェックポイントをキャプチャしてスタックに積む。これには、ステージの状態、プレイヤーの位置と回転、そしてターゲットとなる爆弾の参照が含まれる。
            StageCheckpointSnapshot stageCheckpoint = stageManager.CaptureStageCheckpointSnapshot();
            if (!stageCheckpoint.IsValid)
            {
                Debug.LogError($"{nameof(GameLogicManagerMB)}: stage checkpoint snapshot capture failed.", this);
                return;
            }

            retryCheckpointStack.Push(new RetryCheckpointSnapshot(
                stageCheckpoint,
                resolvedPlayer.transform.position,
                resolvedPlayer.transform.rotation,
                targetBomb,
                targetBomb.CaptureRetryCheckpointState()));

            resetArmed = false;
            SetCurrentBomb(targetBomb);
        }

        // Player の参照が無効ならシーン内から探し直してキャッシュする。
        private PlayerMB ResolvePlayerInstance()
        {
            if (IsDestroyedOrShuttingDown())
                return null;

            if (TryResolveBoundPlayerRef(playerInstance, out EntityRef cachedPlayerRef))
            {
                playerRef = cachedPlayerRef;
                return playerInstance;
            }

            playerInstance = null;
            playerRef = default;

            PlayerMB foundPlayer = FindBoundPlayerInHierarchy(transform);

            if (foundPlayer == null)
            {
                foundPlayer = FindBoundPlayerInScene();
            }

            if (TryResolveBoundPlayerRef(foundPlayer, out EntityRef resolvedPlayerRef))
            {
                playerInstance = foundPlayer;
                playerRef = resolvedPlayerRef;
            }

            return playerInstance;
        }

        // pool に戻って unbind 済みの Player を再利用すると invalid EntityRef を掴むので、bind 済みだけを候補にする。
        private static PlayerMB FindBoundPlayerInHierarchy(Transform root)
        {
            if (root == null)
                return null;

            PlayerMB[] players = root.GetComponentsInChildren<PlayerMB>(true);

            for (int i = 0; i < players.Length; i++)
            {
                if (TryResolveBoundPlayerRef(players[i], out _))
                    return players[i];
            }

            return null;
        }

        private static PlayerMB FindBoundPlayerInScene()
        {
            PlayerMB[] players = UnityEngine.Object.FindObjectsByType<PlayerMB>(FindObjectsInactive.Include);

            for (int i = 0; i < players.Length; i++)
            {
                if (TryResolveBoundPlayerRef(players[i], out _))
                    return players[i];
            }

            return null;
        }

        private static bool TryResolveBoundPlayerRef(PlayerMB candidate, out EntityRef entity)
        {
            entity = default;

            if (candidate == null)
                return false;

            EntityMB entityMB = candidate.GetComponent<EntityMB>();
            if (entityMB == null || !entityMB.HasEntity)
                return false;

            entity = entityMB.Entity;
            return entity.IsValid;
        }

        private void Start()
        {
            // scene kernel を起点に、ゲーム進行と state machine の接続を作る。
            sceneKernel = transform.GetComponentInChildren<SceneKernelMB>().Kernel;
            GameStateManagerMB stateManager = GameStateManagerMB.Instance;
            gameLogicManagerRef = GetComponent<EntityMB>() != null ? GetComponent<EntityMB>().Entity : default;

            if (stateManager == null)
            {
                Debug.LogError("GameLogicManagerMB: GameStateManagerMB.Instance is null.", this);
                return;
            }

            // StateMachine の購読だけ行い、Starting の発火は GameStateManager 側に一任する。
            stateManager.StateMachine.Subscribe(OnStageChanged);

            // 先に GameStateManager が Starting へ遷移していた場合は、その状態を取りこぼさない。
            if (stateManager.CurrentState == GameState.Starting)
            {
                OnStageChanged(GameState.Starting);
            }
        }

        private void OnDestroy()
        {
            isShuttingDown = true;

            if (Instance == this)
            {
                Instance = null;
            }

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.CancelPath();
            }

            if (GameStateManagerMB.Instance != null)
            {
                GameStateManagerMB.Instance.StateMachine.Unsubscribe(OnStageChanged);
            }

        }

        private bool IsDestroyedOrShuttingDown()
        {
            return isShuttingDown || this == null;
        }
        private void Update()
        {
            // 爆弾が進行中のときだけ経過時間を積む。
            if (currentBomb != null && GameStateManagerMB.Instance.CurrentState == GameState.FusePlaying)
            {
                timeSinceStartBomb += Time.deltaTime;
            }
        }

        // GameState の変化に応じて、stage 読み込み、UI 切り替え、演出開始を振り分ける。
        public void OnStageChanged(GameState newState)
        {
            if (newState == GameState.Starting)
            {
                LoadStageAsync(debugStartStageIndex).Forget(); // 起動 stage をロードする
            }
            else if (newState == GameState.Intro)
            {
                PlayCameraPathSequence().Forget(); // ステージ開始時のカメラパスを再生する
            }
            else if (newState == GameState.SetupPlaying)
            {
                sceneKernel.Cameras?.ClearPresentationCamera();
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, true);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, true);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Interaction.CanInteract, EntityMoveMotorMB.GameLogicTag, true);
                GameBGMManagerMB.Instance?.PlayGameplayBGM();
            }
            else if (newState == GameState.FusePlaying)
            {
            }
            else if (newState == GameState.Exploded)
            {
                // 爆弾が爆発したときの処理
            }
            else if (newState == GameState.Reload)
            {
                ReloadStageAsync().Forget(); // ステージをリロードする

            }
            else if (newState == GameState.ResetStage)
            {
                ResetStageAsync().Forget();
            }
            else if (newState == GameState.Goaling)
            {
                GoalAsync().Forget(); // ゴール処理を実行する
            }
            else if (newState == GameState.NextStage)
            {
                NextStageAsync().Forget(); // 次のステージに進む処理を実行する
            }
            else if (newState == GameState.ReturnToTitle)
            {
                ReturnToTitleAsync().Forget();
            }
            else if (newState == GameState.GameOver)
            {
                // ゲームオーバーになったときの処理
            }
        }
        public async UniTask GoalAsync()
        {
            if (IsDestroyedOrShuttingDown())
                return;

            if (currentGoalData == null) return;

            if (currentGoalData.GoalCamera == null)
            {
                Debug.LogError("GameLogicManagerMB: currentGoalData.GoalCamera is not assigned.", this);
                return;
            }
            // Bombのカウントダウン時間をValueStoreに反映
            // 評価データは SelfEntityRef (gameLogicManagerRef) に書き込む。
            // UIStageClearMB は GameLogicManagerMB.Instance.SelfEntityRef でこれを読むため、
            // playerRef に書くと読み取り先が合わず評価星が正しく表示されない。
            if (sceneKernel != null && gameLogicManagerRef.IsValid)
            {
                sceneKernel.ValueStore.Set<float>(gameLogicManagerRef, ValueKeys.Kernel.Evaluation.CountdownTime, timeSinceStartBomb);
                sceneKernel.ValueStore.Set<float>(gameLogicManagerRef, ValueKeys.Kernel.Evaluation.FastClearThreshold, currentClearTimeThreshold);
            }
            // スコア計算を行う
            bool isFastClear = timeSinceStartBomb <= currentClearTimeThreshold;
            sceneKernel.ValueStore.Set<bool>(gameLogicManagerRef, ValueKeys.Kernel.Evaluation.IsFastClear, isFastClear);
            // アイテム取得はできたかどうか
            bool isBonusItem = currentBonusObject != null && currentBonusObject.IsCollected;
            sceneKernel.ValueStore.Set<bool>(gameLogicManagerRef, ValueKeys.Kernel.Evaluation.IsBonusItem, isBonusItem);

            // タイトル画面の解放状態とステージ報酬を永続化する。
            TitleStageProgressServiceMB.SaveStageResultPersisted(currentGameStage, isBonusItem, isFastClear);


            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            if (resolvedPlayer == null)
            {
                Debug.LogError("GameLogicManagerMB: PlayerInstance is not resolved.", this);
                return;
            }

            EntityMoveMotorMB moveController = resolvedPlayer.MoveController;
            if (moveController == null)
            {
                moveController = resolvedPlayer.GetComponent<EntityMoveMotorMB>();
            }

            if (moveController == null)
            {
                Debug.LogError("GameLogicManagerMB: EntityMoveMotorMB is not found on the resolved player.", this);
                return;
            }

            // ゴール演出中のカメラ切り替えは scene camera service に一本化する。
            sceneKernel.Cameras?.ShowPresentationCamera(currentGoalData.GoalCamera, playerRef);

            // UIを非表示にする
            uiGameSceneManagerMB.ShowTopPanel(false); // ゲームシーンのUIを非表示にする
            uiGameSceneManagerMB.ShowBottomPanel(false); // ゲームシーンのUIを非表示にする

            // Playerを止める
            await moveController.MoveToAsync(currentGoalData.Target, 0.1f);

            if (IsDestroyedOrShuttingDown())
                return;

            if (sceneKernel != null && playerRef.IsValid)
            {
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, false);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, false);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Interaction.CanInteract, EntityMoveMotorMB.GameLogicTag, false);
            }

            // ファイナルステージじゃないなら、godhandをゴールの位置に移動させる
            if (!currentGoalData.IsFinalGoal)
            {
                if (currentGodHand != null)
                {
                    currentGodHand.SetTargetPosition(); // GodHandをゴールの位置に移動させる
                }
            }


        }
        private async UniTask NextStageAsync()
        {
            if (IsDestroyedOrShuttingDown() || playerInstance == null || currentGoalData == null || currentGodHand == null)
                return;
            if (currentGoalData.IsFinalGoal)
            {
                await FinalStageGoalAsync();
                return;
            }
            PlayerAnimationMB playerAnimationController = playerInstance.GetComponentInChildren<PlayerAnimationMB>();
            playerAnimationController?.SetNextStageActive(true); // プレイヤーのアニメーションパラメーターを更新して、次のステージに進むためのアニメーションを再生する
            currentGodHand.Catch(playerInstance); // プレイヤーをGodHandにつかまらせる
            // cinemachineCameraの方向を、Playerに向ける
            await LookAtAsync(currentGoalData.GoalCamera.transform, playerInstance.transform.position);
            if (IsDestroyedOrShuttingDown())
                return;

            await UniTask.Delay(700);
            if (IsDestroyedOrShuttingDown())
                return;

            await currentGodHand.MoveToAsync(currentGodHand.OriginalPosition, 1.7f); // GodHandを移動させる
            if (IsDestroyedOrShuttingDown())
                return;

            await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f); // フェードアウトさせる
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); // 黒画面を一度描画してから stage を切り替える

            if (IsDestroyedOrShuttingDown())
                return;

            // マップのロジック状態をリセットする


            // 次のステージに進むための処理
            await LoadStageAsync(currentGameStage + 1);

        }
        private async UniTask FinalStageGoalAsync()
        {
            // ここでは最後のステージのゴールに到達した際の処理を行う

            // エンディング BGM をゲームプレイ BGM からクロスフェードで切り替える
            GameBGMManagerMB.Instance?.PlayEndingBGM();

            await UniTask.Delay(1000); // ゴール到達後の演出のために少し待つ
            await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f); // フェードアウトさせる

            var gameEndUI = UIManagerMB.Instance != null ? UIManagerMB.Instance.GameEndUI : null;
            if (gameEndUI == null)
            {
                Debug.LogError("GameLogicManagerMB: UIGameEndMB is not assigned.", this);
                if (GameStateManagerMB.Instance != null)
                    GameStateManagerMB.Instance.ChangeState(GameState.ReturnToTitle);
                return;
            }

            await gameEndUI.ShowAsync(destroyCancellationToken);

            if (IsDestroyedOrShuttingDown())
                return;

            if (GameStateManagerMB.Instance != null)
                GameStateManagerMB.Instance.ChangeState(GameState.ReturnToTitle);
        }
        private async UniTask LookAtAsync(Transform origin, Vector3 targetPosition, float duration = 1f)
        {
            if (currentGoalData == null || origin == null)
            {
                Debug.LogError("GameLogicManagerMB: GoalCamera is not assigned for LookAtAsync.", this);
                return;
            }

            // originをPlayerに向ける
            float elapsedTime = 0f;
            Quaternion initialRotation = origin.rotation;
            Vector3 directionToTarget = (targetPosition - origin.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            while (elapsedTime < duration)
            {
                if (IsDestroyedOrShuttingDown() || origin == null)
                    return;

                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                origin.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
                await UniTask.Yield();
            }

            await UniTask.CompletedTask;
        }
        // ステージをロードするための内部ロジック。ステージのインスタンス化、プレイヤーのスポーン、カメラの初期化などを行う。
        private async UniTask LoadStageAsync(int stageIndex, bool playIntro = true)
        {
            currentGameStage = stageIndex;
            await LoadGameStageAsync(playIntro);
        }

        private async UniTask ReturnToTitleAsync()
        {
            if (IsDestroyedOrShuttingDown())
                return;

            if (uiGameSceneManagerMB != null)
            {
                uiGameSceneManagerMB.ShowTopPanel(false, 0f);
                uiGameSceneManagerMB.ShowBottomPanel(false, 0f);
            }

            if (uiFadeEffectMB != null)
            {
                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f);
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (IsDestroyedOrShuttingDown())
                return;

            if (titleSceneBuildIndex < 0)
            {
                Debug.LogError("GameLogicManagerMB: titleSceneBuildIndex is invalid.", this);
                return;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(titleSceneBuildIndex, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                Debug.LogError($"GameLogicManagerMB: failed to load title scene build index {titleSceneBuildIndex}.", this);
                return;
            }

            while (!loadOperation.isDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
        // ゲームステージをロードするための具体的な処理
        private async UniTask PlayCameraPathSequence()
        {
            if (IsDestroyedOrShuttingDown())
                return;

            introPathSkipRequested = false;

            if (CameraManager.Instance == null)
            {
                Debug.LogError("GameLogicManagerMB: CameraManager.Instance is null.", this);
                return;
            }

            if (currentCameraPath == null)
            {
                Debug.LogError("GameLogicManagerMB: Camera path is not resolved from MapRuntimeMB.", this);
                return;
            }

            UIIntroPathSkipMB introPathSkipUI = UIManagerMB.Instance != null ? UIManagerMB.Instance.IntroPathSkipUI : null;
            CancellationTokenSource introSkipCts = null;
            UniTask introSkipWatchTask = UniTask.CompletedTask;
            bool introSkipShown = false;

            InputManagerMB.Instance.LockCursor();

            uiGameSceneManagerMB.ShowTopPanel(false, 0f); // ゲームシーンのUIを表示する
            uiGameSceneManagerMB.ShowBottomPanel(false, 0f); // ゲームシーンのUIを表示する

            if (introPathSkipUI != null)
            {
                introPathSkipUI.SetStageIndex(currentGameStage + 1);

                if (!string.IsNullOrWhiteSpace(currentStageName))
                    introPathSkipUI.SetStageName(currentStageName);

                introPathSkipUI.Show();
                introSkipShown = true;
                introSkipCts = new CancellationTokenSource();
                introSkipWatchTask = WatchIntroPathSkipAsync(introPathSkipUI, introSkipCts.Token);
            }

            try
            {
                // 黒画面の間に path camera を開始し、TPS カメラが見える瞬間をなくす。
                CameraManager.Instance.SetPathCameraPosition(currentCameraPath, playerRef);

                // Intro カメラ演出開始: BGM をフェードアウトして Intro SE を再生する。
                // 演出終了後は SetupPlaying 状態で PlayGameplayBGM() が呼ばれ BGM が再開する。
                GameBGMManagerMB.Instance?.StopBGMForIntro();

                UniTask playPathTask = CameraManager.Instance.PlayPathAsync(currentCameraPath, playerRef, async () =>
                {
                    // カメラパスの再生が完了した後の処理
                    if (IsDestroyedOrShuttingDown() || uiFadeEffectMB == null)
                        return;

                    await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f); // フェードインさせる
                });

                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

                if (IsDestroyedOrShuttingDown())
                    return;

                if (!introPathSkipRequested)
                    await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 0.2f, 0.5f); // フェードインさせる

                await playPathTask;

                if (IsDestroyedOrShuttingDown())
                    return;

                // Skip 経路では PlayPath 完了 callback が走らない場合があるため、ここで黒フェードを保証する。
                if (introPathSkipRequested && uiFadeEffectMB != null)
                {
                    await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f);
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                }

                PlayerMB resolvedPlayer = ResolvePlayerInstance();
                if (resolvedPlayer != null)
                {
                    resolvedPlayer.PlayRespawnEffect(); // プレイヤーのスポーンエフェクトを再生する
                }

                if (IsDestroyedOrShuttingDown() || resolvedPlayer == null)
                    return;

                UniTask showPlayerTask = resolvedPlayer.ShowPlayerAsync(true);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); // OnEnable で TPS target を登録させてから画面を開く

                if (IsDestroyedOrShuttingDown())
                    return;

                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 0f, 0.5f); // フェードインさせる
                await showPlayerTask; // プレイヤーを表示する
                uiGameSceneManagerMB.ShowTopPanel(true); // ゲームシーンのUIを表示する
                uiGameSceneManagerMB.ShowBottomPanel(true); // ゲームシーンのUIを表示する

                if (GameStateManagerMB.Instance != null)
                    GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);
            }
            finally
            {
                if (introSkipCts != null)
                {
                    introSkipCts.Cancel();
                }

                try
                {
                    await introSkipWatchTask;
                }
                catch (OperationCanceledException)
                {
                }

                introSkipCts?.Dispose();
                introPathSkipRequested = false;

                if (introSkipShown && introPathSkipUI != null)
                {
                    try
                    {
                        await introPathSkipUI.HideAsync();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }

        }

        private async UniTask WatchIntroPathSkipAsync(UIIntroPathSkipMB introPathSkipUI, CancellationToken cancellationToken)
        {
            if (introPathSkipUI == null)
                return;

            try
            {
                await introPathSkipUI.WaitForSkipHoldAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            introPathSkipRequested = true;

            if (!IsDestroyedOrShuttingDown() && uiFadeEffectMB != null)
            {
                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }

            if (!IsDestroyedOrShuttingDown())
                CameraManager.Instance?.CancelPath();
        }

        // リロードするためのもの、マップの作り替えではなく、マップ内にあるcapture対象とPlayer、Bombなどの状態を戻す
        private async UniTask ReloadStageAsync()
        {
            if (IsDestroyedOrShuttingDown())
                return;

            if (retryCheckpointStack.Count == 0)
            {
                if (resetArmed)
                    GameStateManagerMB.Instance?.ChangeState(GameState.ResetStage);
                else
                    GameStateManagerMB.Instance?.ChangeState(GameState.SetupPlaying);

                return;
            }

            if (uiFadeEffectMB != null)
            {
                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); // 黒画面を描画してからリロードする
            }

            if (IsDestroyedOrShuttingDown())
                return;

            RetryCheckpointSnapshot retryCheckpoint = retryCheckpointStack.Pop();

            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            PlayerItemHandleStateMB itemHandleState = resolvedPlayer != null
                ? resolvedPlayer.GetComponent<PlayerItemHandleStateMB>()
                : null;
            itemHandleState?.RestoreRetryCheckpointState();

            StageManagerMB.Instance.ReloadStage(retryCheckpoint.StageCheckpoint);

            if (retryCheckpoint.Bomb != null && retryCheckpoint.BombRetryState != null)
            {
                if (!retryCheckpoint.Bomb.gameObject.activeSelf)
                    retryCheckpoint.Bomb.gameObject.SetActive(true);

                retryCheckpoint.Bomb.RestoreCheckpointState(retryCheckpoint.BombRetryState);
            }

            if (resolvedPlayer != null)
            {
                resolvedPlayer.ResetPlayer();
                resolvedPlayer.TeleportToSpawnPoint(retryCheckpoint.PlayerPosition, retryCheckpoint.PlayerRotation);
            }

            BombMB retryBomb = ResolveRetryBomb(retryCheckpoint.Bomb);

            SetCurrentBomb(retryBomb);
            timeSinceStartBomb = 0f;
            resetArmed = retryCheckpointStack.Count == 0;
            ReloadState?.Invoke();

            if (GameStateManagerMB.Instance != null)
                GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);

            if (uiFadeEffectMB != null)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); // state 更新後に 1 フレーム置いてから戻す
                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 0f, 0.5f);
            }

            await UniTask.CompletedTask;
        }

        private async UniTask ResetStageAsync()
        {
            if (IsDestroyedOrShuttingDown())
                return;

            if (uiFadeEffectMB != null)
            {
                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }

            if (IsDestroyedOrShuttingDown())
                return;

            await LoadGameStageAsync(playIntro: false);

            if (uiFadeEffectMB != null)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 0f, 0.5f);
            }

            await UniTask.CompletedTask;
        }
        // ステージをリセットするための内部ロジック。現在のステージを完全にリセットして、初期状態に戻す。
        private BombMB ResolveDefaultCurrentBomb()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return null;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                if (currentMapRuntime.Bombs[i] != null)
                    return currentMapRuntime.Bombs[i];
            }

            return null;
        }

        private BombMB ResolveRetryBomb(BombMB preferredBomb)
        {
            if (preferredBomb != null)
                return preferredBomb;

            return ResolveDefaultCurrentBomb();
        }
        // currentBombをセットするためのメソッド。currentBombが変わるたびに、関連するイベントハンドラの登録と解除を行い、currentBombが変わったことを通知するイベントを発火する。
        public void SetCurrentBomb(BombMB bomb)
        {
            if (currentBomb != null)
            {
                currentBomb.StartedFuse -= HandleCurrentBombStartedFuse; // 既にcurrentBombに登録されている爆弾があれば、イベントハンドラを解除する
                currentBomb.Exploded -= HandleCurrentBombExploded; // 爆発イベントのハンドラも解除する
            }
            currentBomb = bomb;
            if (currentBomb != null)
            {
                currentBomb.StartedFuse += HandleCurrentBombStartedFuse;
                currentBomb.Exploded += HandleCurrentBombExploded;
            }
            OnCurrentBombChanged?.Invoke(currentBomb);
        }
        private void HandleCurrentBombStartedFuse(BombMB bomb)
        {
            if (bomb != currentBomb) return; // currentBomb以外の爆弾が起爆した場合は無視する
            hasStartedAnyBombFuseThisStage = true;
            GameStateManagerMB.Instance.ChangeState(GameState.FusePlaying);
            OnStartBombFuse?.Invoke(currentBomb); // 爆弾のカウントダウンが開始されたことを通知するイベントを発火する
            timeSinceStartBomb = 0; // 爆弾のカウントダウン開始からの経過時間をリセットする
        }
        private void HandleCurrentBombExploded(BombMB bomb)
        {
            if (bomb != currentBomb) return; // currentBomb以外の爆弾が爆発した場合は無視する
            GameStateManagerMB.Instance.ChangeState(GameState.Exploded);
            OnEndBombFuse?.Invoke(); // 爆弾のカウントダウンが終了したことを通知するイベントを発火する

            ExplodedState?.Invoke();

            if (!IsGoalGateOpened())
            {
                ExplodedBeforeGoalOpenedState?.Invoke();
            }
        }

        private bool IsGoalGateOpened()
        {
            if (currentMapRuntime == null || currentMapRuntime.GoalGate == null)
                return false;

            return currentMapRuntime.GoalGate.IsBroken;
        }

        public void LoadGameStage(bool playIntro = true)
        {
            LoadGameStageAsync(playIntro).Forget();
        }

        private async UniTask LoadGameStageAsync(bool playIntro = true)
        {
            sceneKernel?.Cameras?.ResetRuntimeState();

            if (stageInstance != null && debugStageInstance == null)
            {
                UnregisterStageEntities(stageInstance);
                Destroy(stageInstance);
                stageInstance = null;
            }

            StageLoadResult result;

            if (debugStageInstance == null)
            {
                result = StageManagerMB.Instance.LoadStage(currentGameStage);
                stageInstance = result.stageInstance;
                RegisterStageEntities(result.stageInstance);
                ResetPlayer(); // プレイヤーをリセットする (loadで古いステージは消えるのですが、PlayerはMap外に残っているので、こちらで明確に消す必要があります)
                // playerをテレポートさせる
                if (result.spawnPoints.Count > 0)
                {
                    // とりあえず最初のスポーンポイントにテレポートさせる
                    PlayerSpawnPointMB spawnPoint = result.spawnPoints[0];
                    SpawnAndTeleportPlayer(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                }
                else
                {
                    Debug.LogError("GameLogicManagerMB: No PlayerSpawnPointMB was found in the loaded stage.", this);
                }
            }
            else
            {
                result = StageManagerMB.Instance.ResolveStageRuntime(debugStageInstance.gameObject);
                stageInstance = result.stageInstance;
                RegisterStageEntities(result.stageInstance);
                playerInstance = ResolvePlayerInstance();
                OnPlayerSpawned?.Invoke(playerInstance);
                OnPlayerUpdated?.Invoke(playerInstance);
                playerRef = playerInstance != null ? playerInstance.GetComponent<EntityMB>().Entity : default;
                sceneKernel.Cameras?.SetTrackedPlayer(playerRef);

                // デバッグ用
                if (!playerRef.IsValid) Debug.LogError("GameLogicManagerMB: PlayerRef is not valid.", this);
            }

            // StageManager が解決したランタイム参照だけを使って、ゲームロジック側の入力値を更新する。
            stageInstance = result.stageInstance;
            currentMapRuntime = result.mapRuntime;
            currentGoalData = result.goalData;
            currentStageName = result.StageName;
            currentCameraPath = result.cameraPath;
            currentGodHand = result.godHandObjects.Count > 0 ? result.godHandObjects[0] : null;
            currentBonusObject = result.bonusObject;
            SetCurrentBomb(result.bombs.Count > 0 ? result.bombs[0] : null);
            currentClearTimeThreshold = result.ClearTimeThreshold;
            ResetRetryActionContext();

            // マップリセット
            ResetSceneKernelValueStore();

            if (currentGoalData == null) Debug.LogError("GameLogicManagerMB: GoalData is not resolved from the stage runtime.", this);
            if (currentCameraPath == null) Debug.LogError("GameLogicManagerMB: Camera path is not resolved from the stage runtime.", this);


            if (playIntro)
            {
                GameStateManagerMB.Instance.ChangeState(GameState.Intro);
            }
            else
            {
                await StartGameplayWithoutIntroAsync();
            }
        }

        private void RegisterStageEntities(GameObject rootObject)
        {
            if (sceneKernel == null || rootObject == null)
                return;

            var bootstrapper = new SceneEntityBootstrapper(sceneKernel, rootObject.transform);
            bootstrapper.RegisterSceneEntities();
        }

        private void UnregisterStageEntities(GameObject rootObject)
        {
            if (sceneKernel == null || rootObject == null)
                return;

            var bootstrapper = new SceneEntityBootstrapper(sceneKernel, rootObject.transform);
            bootstrapper.UnregisterSceneEntities();
        }

        private static PlayerMB ResolveSpawnedPlayer(GameObject rootObject)
        {
            return rootObject != null ? rootObject.GetComponentInChildren<PlayerMB>(true) : null;
        }
        private void ResetPlayer()
        {
            CameraManager.Instance?.ClearThirdPersonTarget();

            // プレイヤーを削除する
            EntityRef playerEntity = playerRef;
            if (!playerEntity.IsValid)
            {
                TryResolveBoundPlayerRef(playerInstance, out playerEntity);
            }

            if (playerEntity.IsValid)
            {
                sceneKernel.Spawner.Despawn(playerEntity);
            }

            playerInstance = null;
            playerRef = default;
            OnPlayerUpdated?.Invoke(playerInstance);

            sceneKernel?.Cameras?.SetTrackedPlayer(default);
        }

        private async UniTask StartGameplayWithoutIntroAsync()
        {
            if (IsDestroyedOrShuttingDown())
                return;

            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            if (resolvedPlayer == null)
            {
                Debug.LogError("GameLogicManagerMB: PlayerInstance is not resolved for retry reset.", this);
                return;
            }

            resolvedPlayer.PlayRespawnEffect();
            await resolvedPlayer.ShowPlayerAsync(false);

            if (IsDestroyedOrShuttingDown())
                return;

            uiGameSceneManagerMB.ShowTopPanel(true);
            uiGameSceneManagerMB.ShowBottomPanel(true);
            GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);
        }

        private void ResetRetryActionContext()
        {
            retryCheckpointStack.Clear();
            resetArmed = false;
            hasStartedAnyBombFuseThisStage = false;
            timeSinceStartBomb = 0f;
            StageManagerMB.Instance?.ClearStageCheckpoint();
        }

        private bool HasAnySceneBombFuseStartedOrExploded()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null)
                return false;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                BombMB bomb = currentMapRuntime.Bombs[i];

                if (bomb == null)
                    continue;

                if (bomb.FuseStarted || bomb.HasExploded)
                    return true;
            }

            return false;
        }

        // ゲーム内にプレイヤーがいた場合はTeleportのみ、いない場合はSpawnしてからTeleportする
        public void SpawnAndTeleportPlayer(EntityMB player, Vector3 position = default, Quaternion rotation = default)
        {
            if (player == null)
            {
                Debug.LogError("GameLogicManagerMB: playerPrefab is not assigned.", this);
                return;
            }

            EntitySpawnResult result = SpawnPlayer(player, position, rotation);
            playerInstance = ResolveSpawnedPlayer(result.GameObject);

            if (playerInstance == null)
            {
                Debug.LogError("GameLogicManagerMB: Spawned player prefab does not contain PlayerMB.", result.GameObject);
                return;
            }

            OnPlayerSpawned?.Invoke(playerInstance);
            OnPlayerUpdated?.Invoke(playerInstance);
            playerRef = result.Entity;
            sceneKernel.Cameras?.SetTrackedPlayer(playerRef);

            // 一時的にPlayerの動きを止める
            if (!playerRef.IsValid)
            {
                Debug.LogError("GameLogicManagerMB: PlayerRef is not valid.", this);
            }
            // いったんプレイヤーを無効にする。既存再利用時だけでなく新規 spawn 時にも隠す。
            if (playerInstance != null)
            {
                Debug.Log("Hiding player during spawn/teleport.");
                playerInstance.HidePlayer();
            }
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, true);
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, false);
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Interaction.CanInteract, EntityMoveMotorMB.GameLogicTag, false);
        }
        public EntitySpawnResult SpawnPlayer(EntityMB player, Vector3 position, Quaternion rotation)
        {
            Transform spawnParent = stageInstance != null ? stageInstance.transform : null;
            EntitySpawnResult result = sceneKernel.Spawner.Spawn(new EntitySpawnRequest(player.gameObject, spawnParent, position, rotation, true, false));
            PlayerMB newPlayer = ResolveSpawnedPlayer(result.GameObject);

            if (newPlayer != null)
            {
                newPlayer.TeleportToSpawnPoint(position, rotation);
            }
            else
            {
                Debug.LogError("GameLogicManagerMB: Spawned player prefab does not contain PlayerMB.", result.GameObject);
            }

            return result;
        }

        private void ResetSceneKernelValueStore()
        {
            if (sceneKernel == null)
                return;

            sceneKernel.ValueStore.SetBoolModifier(gameLogicManagerRef, ValueKeys.GameLogic.Interaction.IsStateBlue, EntityMoveMotorMB.GameLogicTag, false);
            sceneKernel.ValueStore.SetBoolModifier(gameLogicManagerRef, ValueKeys.GameLogic.Interaction.IsStateRed, EntityMoveMotorMB.GameLogicTag, false);
            sceneKernel.ValueStore.SetBoolModifier(gameLogicManagerRef, ValueKeys.GameLogic.Interaction.IsStateGreen, EntityMoveMotorMB.GameLogicTag, false);
            sceneKernel.ValueStore.SetBoolModifier(gameLogicManagerRef, ValueKeys.GameLogic.Interaction.IsStateYellow, EntityMoveMotorMB.GameLogicTag, false);
        }



    }

    internal readonly struct RetryCheckpointSnapshot
    {
        public RetryCheckpointSnapshot(
            StageCheckpointSnapshot stageCheckpoint,
            Vector3 playerPosition,
            Quaternion playerRotation,
            BombMB bomb,
            object bombRetryState)
        {
            StageCheckpoint = stageCheckpoint;
            PlayerPosition = playerPosition;
            PlayerRotation = playerRotation;
            Bomb = bomb;
            BombRetryState = bombRetryState;
        }

        public StageCheckpointSnapshot StageCheckpoint { get; }
        public Vector3 PlayerPosition { get; }
        public Quaternion PlayerRotation { get; }
        public BombMB Bomb { get; }
        public object BombRetryState { get; }
    }
}