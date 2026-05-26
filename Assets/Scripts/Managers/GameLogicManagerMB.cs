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
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
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

            // debug用のstageIndexを更新する
            currentGameStage = debugStageIndex;
        }
        [Header("References")]
        [SerializeField] private UIManagerMB uiManager; // UI参照集約サービス。
        [SerializeField] private EntityMB playerPrefab; // スポーン用プレイヤー prefab。
        [Header("Debug")][SerializeField] private Transform debugStageInstance; // デバッグ用に直接参照する stage instance。
        [Header("Debug")][SerializeField] private int debugStageIndex; // デバッグ用に直接指定する stage index。
        // 現在のゲーム進行にぶら下がる主要参照群。
        private BombMB currentBomb;

        private GodHandObjectMB currentGodHand; // つかまっている GodHand の参照。
        private MapRuntimeMB currentMapRuntime; // 現在ロード中の map runtime。
        private GameObject stageInstance; // 現在の stage instance。
        private PlayerMB playerInstance; // 現在の player instance。
        private GoalData currentGoalData; // 現在のゴールデータ。
        private CameraPathSequenceAuthoringMB currentCameraPath; // 現在の camera path。
        private EntityRef playerRef; // player の EntityRef。
        public Action<PlayerMB> OnPlayerSpawned; // player spawn 通知。
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
        private string currentStageName = string.Empty;
        private readonly Stack<RetryCheckpointSnapshot> retryCheckpointStack = new();
        private bool hasStartedAnyBombFuseThisStage;
        private bool resetArmed;
        private bool hasLoggedMissingUIManager;
        private bool hasLoggedMissingFadeUI;
        private bool hasLoggedMissingGameSceneUI;

        public BombMB CurrentBomb => currentBomb;
        public PlayerMB PlayerInstance => playerInstance;
        public RetryActionMode CurrentRetryActionMode => ResolveRetryActionMode();
        public bool HasRetryCheckpoint => retryCheckpointStack.Count > 0;
        public bool HasStartedAnyBombFuseThisStage => hasStartedAnyBombFuseThisStage;
        public bool ShouldAutoShowResetPrompt =>
            IsRetryActionAvailable() &&
            ResolveRetryActionMode() == RetryActionMode.ResetStage &&
            !hasStartedAnyBombFuseThisStage;

        public bool AreAllSceneBombsExploded()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return false;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                // Destroy 済みの Bomb 参照は Unity 上 null 扱いになるので、null も爆発済みとして扱う。
                BombMB bomb = currentMapRuntime.Bombs[i];
                if (bomb != null && !bomb.HasExploded)
                    return false;
            }

            return true;
        }

        // UI 表示向け: 現在ステージに「未爆発で有効な爆弾」が残っているかを返す。
        public bool HasAnyActiveSceneBomb()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return false;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                BombMB bomb = currentMapRuntime.Bombs[i];
                if (bomb != null && bomb.gameObject.activeInHierarchy && !bomb.HasExploded)
                    return true;
            }

            return false;
        }

        // ボーナス取得判定向け: シーン内に fuse 中(起動中)の爆弾が 1 つでもあるかを返す。
        public bool HasAnyFusingSceneBomb()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return false;

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                BombMB bomb = currentMapRuntime.Bombs[i];
                if (bomb != null && bomb.gameObject.activeInHierarchy && bomb.FuseStarted && !bomb.HasExploded)
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

            // checkpoint が無いときは、ゲーム進行中(Setup/Fuse/Exploded)なら常に Reset を許可する。
            return RetryActionMode.ResetStage;
        }

        // リトライを許可するゲーム状態かを判断する。
        public bool IsRetryActionAvailable()
        {
            GameStateManagerMB stateManager = GameStateManagerMB.Instance;
            if (stateManager == null)
                return false;

            // ゴールゲート破壊後は通常の checkpoint/retry を案内しないが、
            // 破壊後にプレイヤーが死亡した場合だけは救済として retry を許可する。
            if (IsGoalGateOpened() && !IsPlayerDeadForRetry())
                return false;

            return stateManager.CurrentState == GameState.SetupPlaying ||
                   stateManager.CurrentState == GameState.FusePlaying ||
                     stateManager.CurrentState == GameState.Exploded ||
                     stateManager.CurrentState == GameState.GameOver;
        }

        private bool IsPlayerDeadForRetry()
        {
            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            PlayerMoveController moveController = resolvedPlayer != null ? resolvedPlayer.PlayerMoveController : null;
            return moveController != null && moveController.MoveMotor != null && moveController.MoveMotor.IsDead;
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

            // Fuse 開始後は checkpoint を上書きしない。
            // 再所持時にここを許可すると、Reload の復帰地点が後ろへずれてしまう。
            if (hasStartedAnyBombFuseThisStage || targetBomb.FuseStarted || targetBomb.HasExploded)
            {
                SetCurrentBomb(targetBomb);
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
                CaptureRetryBombSnapshots()));

            resetArmed = false;
            SetCurrentBomb(targetBomb);
        }

        // Player の参照が無効ならシーン内から探し直してキャッシュする。
        private PlayerMB ResolvePlayerInstance()
        {
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
            ResolveUIManager();

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

        private UIManagerMB ResolveUIManager()
        {
            uiManager ??= UIManagerMB.Instance;
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManagerMB>(FindObjectsInactive.Include);

            if (uiManager == null && !hasLoggedMissingUIManager)
            {
                hasLoggedMissingUIManager = true;
                Debug.LogWarning($"{nameof(GameLogicManagerMB)}: {nameof(UIManagerMB)} is not found in scene.", this);
            }

            return uiManager;
        }

        private UIFadeEffectMB ResolveFadeEffectUI()
        {
            UIFadeEffectMB fadeUI = ResolveUIManager() != null ? uiManager.FadeEffect : null;
            if (fadeUI == null && !hasLoggedMissingFadeUI)
            {
                hasLoggedMissingFadeUI = true;
                Debug.LogWarning($"{nameof(GameLogicManagerMB)}: {nameof(UIFadeEffectMB)} is not available via {nameof(UIManagerMB)}.", this);
            }

            return fadeUI;
        }

        private UIGameSceneManagerMB ResolveGameSceneUI()
        {
            UIGameSceneManagerMB gameSceneUI = ResolveUIManager() != null ? uiManager.GameSceneManager : null;
            if (gameSceneUI == null && !hasLoggedMissingGameSceneUI)
            {
                hasLoggedMissingGameSceneUI = true;
                Debug.LogWarning($"{nameof(GameLogicManagerMB)}: {nameof(UIGameSceneManagerMB)} is not available via {nameof(UIManagerMB)}.", this);
            }

            return gameSceneUI;
        }

        private static async UniTask StartFadeAsyncSafe(UIFadeEffectMB fadeUI, FadeType fadeType, float amount, float duration)
        {
            if (fadeUI == null)
                return;

            await fadeUI.StartFadeAsync(fadeType, amount, duration);
        }

        private void SetGameScenePanelsVisible(bool visible, float duration = 0.5f)
        {
            UIGameSceneManagerMB gameSceneUI = ResolveGameSceneUI();
            if (gameSceneUI == null)
                return;

            gameSceneUI.ShowTopPanel(visible, duration);
            gameSceneUI.ShowBottomPanel(visible, duration);
        }

        private void OnDestroy()
        {
            if (GameStateManagerMB.Instance != null)
            {
                GameStateManagerMB.Instance.StateMachine.Unsubscribe(OnStageChanged);
            }

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
                LoadStageAsync(currentGameStage).Forget(); // 最初のステージをロードする
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
            else if (newState == GameState.GameOver)
            {
                // ゲームオーバーになったときの処理
            }
        }
        public async UniTask GoalAsync()
        {
            if (currentGoalData == null) return;

            if (currentGoalData.GoalCamera == null)
            {
                Debug.LogError("GameLogicManagerMB: currentGoalData.GoalCamera is not assigned.", this);
                return;
            }
            // Bombのカウントダウン時間をValueStoreに反映
            if (sceneKernel != null && gameLogicManagerRef.IsValid)
            {
                sceneKernel.ValueStore.Set<float>(playerRef, ValueKeys.Kernel.Evaluation.CountdownTime, timeSinceStartBomb);
                sceneKernel.ValueStore.Set<float>(playerRef, ValueKeys.Kernel.Evaluation.FastClearThreshold, currentClearTimeThreshold);
            }
            // スコア計算を行う
            bool isFastClear = timeSinceStartBomb <= currentClearTimeThreshold;
            sceneKernel.ValueStore.Set<bool>(playerRef, ValueKeys.Kernel.Evaluation.IsFastClear, isFastClear);
            // アイテム取得はできたかどうか
            bool isBonusItem = currentBonusObject != null && currentBonusObject.IsCollected;
            sceneKernel.ValueStore.Set<bool>(playerRef, ValueKeys.Kernel.Evaluation.IsBonusItem, isBonusItem);


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
            SetGameScenePanelsVisible(false); // ゲームシーンのUIを非表示にする

            // Playerを止める
            await moveController.MoveToAsync(currentGoalData.Target, 0.1f);
            if (sceneKernel != null && playerRef.IsValid)
            {
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, false);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, false);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Interaction.CanInteract, EntityMoveMotorMB.GameLogicTag, false);
            }

            if (currentGodHand != null)
            {
                currentGodHand.SetTargetPosition(); // GodHandをゴールの位置に移動させる
            }


        }
        private async UniTask NextStageAsync()
        {
            InputManagerMB.Instance?.LockCursor(); // プレイヤーの入力を無効にする
            PlayerAnimationMB playerAnimationController = playerInstance.GetComponentInChildren<PlayerAnimationMB>();
            playerAnimationController?.SetNextStageActive(true); // プレイヤーのアニメーションパラメーターを更新して、次のステージに進むためのアニメーションを再生する
            currentGodHand.Catch(playerInstance); // プレイヤーをGodHandにつかまらせる
            // cinemachineCameraの方向を、Playerに向ける
            await LookAtAsync(currentGoalData.GoalCamera.transform, playerInstance.transform.position);
            await UniTask.Delay(700);
            await currentGodHand.MoveToAsync(currentGodHand.OriginalPosition, 1.7f); // GodHandを移動させる
            await StartFadeAsyncSafe(ResolveFadeEffectUI(), FadeType.TopBottom, 1f, 0.5f); // フェードアウトさせる
            // 次のステージに進むための処理
            await LoadStageAsync(currentGameStage + 1);
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
            LoadGameStage(playIntro);

            await UniTask.CompletedTask;
        }
        // ゲームステージをロードするための具体的な処理
        private async UniTask PlayCameraPathSequence()
        {
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

            // Intro 開始前から path camera を最前面にし、初期フレームの priority 競合を防ぐ。
            sceneKernel?.Cameras?.ShowPresentationCamera(CameraManager.Instance.PathCamera, playerRef);

            // さきにカメラの位置を初期化しておく。これがないと、カメラパスの開始地点がプレイヤーの位置に引っ張られてしまう。
            CameraManager.Instance.SetPathCameraPosition(currentCameraPath, playerRef);
            await UniTask.Delay(200);
            SetGameScenePanelsVisible(false, 0f); // ゲームシーンのUIを表示する
            await StartFadeAsyncSafe(ResolveFadeEffectUI(), FadeType.TopBottom, 0.2f, 0.5f); // フェードインさせる

            bool introCompletedActionInvoked = false;
            async UniTask InvokeIntroCompletedActionAsync()
            {
                if (introCompletedActionInvoked)
                    return;

                introCompletedActionInvoked = true;
                await StartFadeAsyncSafe(ResolveFadeEffectUI(), FadeType.TopBottom, 1f, 0.5f); // フェードインさせる
            }

            UniTask introPathTask = CameraManager.Instance.PlayPathAsync(currentCameraPath, playerRef, async () =>
            {
                await InvokeIntroCompletedActionAsync();
            }).Preserve();

            CancellationTokenSource skipWaitCts = null;
            UniTask introSkipTask = UniTask.Never(CancellationToken.None);

            UIIntroPathSkipMB introSkipUI = ResolveUIManager() != null ? uiManager.IntroPathSkipUI : null;
            if (introSkipUI != null)
            {
                introSkipUI.SetStageName(currentStageName);
                introSkipUI.SetStageIndex(currentGameStage + 1);
                introSkipUI.Show();
                skipWaitCts = new CancellationTokenSource();
                introSkipTask = introSkipUI.WaitForSkipHoldAsync(skipWaitCts.Token);
            }

            int completedIndex = await UniTask.WhenAny(introPathTask, introSkipTask);

            // Skip が先に成立したときは path 再生を即時キャンセルし、
            // onComplete の async action をここで実行して終了フローを揃える。
            if (completedIndex == 1)
            {
                CameraManager.Instance.CancelPath();
                await InvokeIntroCompletedActionAsync();
                await introPathTask;
            }

            if (skipWaitCts != null)
            {
                skipWaitCts.Cancel();
                skipWaitCts.Dispose();
            }

            if (introSkipUI != null)
                await introSkipUI.HideAsync();

            // Intro path 再生が終わったら、SetupPlaying まで待たずに presentation を解除する。
            // これで Player Spawn 演出中は TPS カメラが有効になる。
            sceneKernel?.Cameras?.ClearPresentationCamera();

            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            if (resolvedPlayer != null)
            {
                resolvedPlayer.PlayRespawnEffect(); // プレイヤーのスポーンエフェクトを再生する
                RegisterThirdPersonTargetForHandoff(resolvedPlayer, useCameraTarget: false);
            }
            // Intro から TPS に戻る瞬間の 180 度スナップを防ぐため、
            // TPS look yaw を intro 終了時の camera forward に揃えておく。
            AlignThirdPersonYawToCurrentCamera(resolvedPlayer);
            // 少し待つ
            await UniTask.Delay(800);
            await StartFadeAsyncSafe(ResolveFadeEffectUI(), FadeType.TopBottom, 0f, 0.5f); // フェードインさせる

            await UniTask.Delay(200);
            await resolvedPlayer.ShowPlayerAsync(true); // プレイヤーを表示する
            RegisterThirdPersonTargetForHandoff(resolvedPlayer, useCameraTarget: true);


            SetGameScenePanelsVisible(true); // ゲームシーンのUIを表示する
            GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);

        }

        // リロードするためのもの、マップの作り替えではなく、マップ内にあるcapture対象とPlayer、Bombなどの状態を戻す
        private async UniTask ReloadStageAsync()
        {
            if (retryCheckpointStack.Count == 0)
            {
                if (resetArmed)
                    GameStateManagerMB.Instance?.ChangeState(GameState.ResetStage);
                else
                    GameStateManagerMB.Instance?.ChangeState(GameState.SetupPlaying);

                return;
            }

            RetryCheckpointSnapshot retryCheckpoint = retryCheckpointStack.Pop();

            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            PlayerItemHandleStateMB itemHandleState = resolvedPlayer != null
                ? resolvedPlayer.GetComponent<PlayerItemHandleStateMB>()
                : null;
            itemHandleState?.RestoreRetryCheckpointState();


            await StartFadeAsyncSafe(ResolveFadeEffectUI(), FadeType.TopBottom, 1f, 0.5f); // フェードインさせる
            StageManagerMB.Instance.ReloadStage(retryCheckpoint.StageCheckpoint);
            RestoreRetryBombSnapshots(retryCheckpoint.BombSnapshots);
            await StartFadeAsyncSafe(ResolveFadeEffectUI(), FadeType.TopBottom, 0f, 0.5f); // フェードインさせる

            if (resolvedPlayer != null)
            {
                resolvedPlayer.ResetPlayer();
                resolvedPlayer.TeleportToSpawnPoint(retryCheckpoint.PlayerPosition, retryCheckpoint.PlayerRotation);
            }

            BombMB retryBomb = ResolveRetryBomb(retryCheckpoint.PreferredBomb);

            SetCurrentBomb(retryBomb);
            timeSinceStartBomb = 0f;
            resetArmed = retryCheckpointStack.Count == 0;
            ReloadState?.Invoke();

            await UniTask.CompletedTask;
            GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);
        }

        private async UniTask ResetStageAsync()
        {
            LoadGameStage(playIntro: false);
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

        private RetryBombSnapshot[] CaptureRetryBombSnapshots()
        {
            if (currentMapRuntime == null || currentMapRuntime.Bombs == null || currentMapRuntime.Bombs.Count == 0)
                return Array.Empty<RetryBombSnapshot>();

            var snapshots = new List<RetryBombSnapshot>(currentMapRuntime.Bombs.Count);

            for (int i = 0; i < currentMapRuntime.Bombs.Count; i++)
            {
                BombMB bomb = currentMapRuntime.Bombs[i];
                RetryBombSnapshot snapshot = CaptureRetryBombSnapshot(bomb);
                if (!snapshot.IsValid)
                    continue;

                snapshots.Add(snapshot);
            }

            return snapshots.ToArray();
        }

        private static RetryBombSnapshot CaptureRetryBombSnapshot(BombMB bomb)
        {
            if (bomb == null)
                return default;

            Transform bombTransform = bomb.transform;
            return new RetryBombSnapshot(
                bomb,
                bombTransform.parent,
                bombTransform.localPosition,
                bombTransform.localRotation,
                bombTransform.localScale,
                bomb.gameObject.activeSelf,
                bomb.CaptureRetryCheckpointState());
        }

        private static void RestoreRetryBombSnapshots(IReadOnlyList<RetryBombSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
                return;

            for (int i = 0; i < snapshots.Count; i++)
                RestoreRetryBombSnapshot(snapshots[i]);
        }

        private static void RestoreRetryBombSnapshot(in RetryBombSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                return;

            BombMB bomb = snapshot.Bomb;
            if (bomb == null)
                return;

            Transform bombTransform = bomb.transform;

            if (bombTransform.parent != snapshot.Parent)
                bombTransform.SetParent(snapshot.Parent, false);

            bombTransform.localPosition = snapshot.LocalPosition;
            bombTransform.localRotation = snapshot.LocalRotation;
            bombTransform.localScale = snapshot.LocalScale;
            bomb.gameObject.SetActive(snapshot.ActiveSelf);
            bomb.RestoreCheckpointState(snapshot.CheckpointState);
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
                RegisterStageEntities(result.stageInstance);
                playerInstance = ResolvePlayerInstance();
                OnPlayerSpawned?.Invoke(playerInstance);
                playerRef = playerInstance != null ? playerInstance.GetComponent<EntityMB>().Entity : default;
                sceneKernel.Cameras?.SetTrackedPlayer(playerRef);

                // デバッグ用
                if (!playerRef.IsValid) Debug.LogError("GameLogicManagerMB: PlayerRef is not valid.", this);
            }

            // StageManager が解決したランタイム参照だけを使って、ゲームロジック側の入力値を更新する。
            stageInstance = result.stageInstance;
            currentMapRuntime = result.mapRuntime;
            currentGoalData = result.goalData;
            currentCameraPath = result.cameraPath;
            currentGodHand = result.godHandObjects.Count > 0 ? result.godHandObjects[0] : null;
            currentBonusObject = result.bonusObject;
            currentStageName = string.IsNullOrWhiteSpace(result.StageName)
                ? $"Stage {currentGameStage + 1}"
                : result.StageName;
            SetCurrentBomb(result.bombs.Count > 0 ? result.bombs[0] : null);
            currentClearTimeThreshold = result.ClearTimeThreshold;
            ResetRetryActionContext();

            if (currentGoalData == null) Debug.LogError("GameLogicManagerMB: GoalData is not resolved from the stage runtime.", this);
            if (currentCameraPath == null) Debug.LogError("GameLogicManagerMB: Camera path is not resolved from the stage runtime.", this);


            if (playIntro)
            {
                GameStateManagerMB.Instance.ChangeState(GameState.Intro);
            }
            else
            {
                StartGameplayWithoutIntroAsync().Forget();
            }
        }

        private void RegisterStageEntities(GameObject rootObject)
        {
            if (sceneKernel == null || rootObject == null)
                return;

            var bootstrapper = new SceneEntityBootstrapper(sceneKernel, rootObject.transform);
            bootstrapper.RegisterSceneEntities();
        }

        private static void AlignThirdPersonYawToCurrentCamera(PlayerMB player)
        {
            if (player == null || CameraManager.Instance == null)
                return;

            ThirdPersonCameraController controller = player.GetComponentInChildren<ThirdPersonCameraController>(true);
            if (controller == null)
                return;

            Transform referenceTransform = CameraManager.Instance.PathCamera != null
                ? CameraManager.Instance.PathCamera.transform
                : CameraManager.Instance.ThirdPersonCamera != null
                    ? CameraManager.Instance.ThirdPersonCamera.transform
                    : null;

            if (referenceTransform == null)
                return;

            Vector3 forward = referenceTransform.forward;
            forward.y = 0.0f;

            if (forward.sqrMagnitude <= 0.0001f)
                return;

            controller.SyncYawToWorldForward(forward.normalized);
        }

        // Player 非表示中は camera target が無効化されることがあるため、
        // handoff 中だけ root を追従し、表示後に本来の camera target へ戻す。
        private static void RegisterThirdPersonTargetForHandoff(PlayerMB player, bool useCameraTarget)
        {
            if (player == null || CameraManager.Instance == null)
                return;

            Transform target = player.transform;

            if (useCameraTarget)
            {
                ThirdPersonCameraController controller = player.GetComponentInChildren<ThirdPersonCameraController>(true);
                if (controller != null && controller.CameraTarget != null)
                    target = controller.CameraTarget;
            }

            CameraManager.Instance.RegisterThirdPersonTarget(target);
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
            // プレイヤーを削除する
            EntityRef playerEntity = playerRef;
            if (!playerEntity.IsValid)
            {
                TryResolveBoundPlayerRef(playerInstance, out playerEntity);
            }

            if (playerEntity.IsValid)
            {
                sceneKernel.Spawner.Despawn(playerEntity, EntityDespawnMode.Destroy);
            }

            playerInstance = null;
            playerRef = default;

            sceneKernel?.Cameras?.SetTrackedPlayer(default);
        }

        private async UniTask StartGameplayWithoutIntroAsync()
        {
            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            if (resolvedPlayer == null)
            {
                Debug.LogError("GameLogicManagerMB: PlayerInstance is not resolved for retry reset.", this);
                return;
            }

            resolvedPlayer.PlayRespawnEffect();
            RegisterThirdPersonTargetForHandoff(resolvedPlayer, useCameraTarget: false);
            await resolvedPlayer.ShowPlayerAsync(false);
            RegisterThirdPersonTargetForHandoff(resolvedPlayer, useCameraTarget: true);
            SetGameScenePanelsVisible(true);
            GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);
        }

        private void ResetRetryActionContext()
        {
            retryCheckpointStack.Clear();
            hasStartedAnyBombFuseThisStage = false;
            resetArmed = false;
            timeSinceStartBomb = 0f;
            StageManagerMB.Instance?.ClearStageCheckpoint();
        }

        // ゲーム内にプレイヤーがいた場合はTeleportのみ、いない場合はSpawnしてからTeleportする
        public void SpawnAndTeleportPlayer(EntityMB player, Vector3 position = default, Quaternion rotation = default)
        {
            if (player == null)
            {
                Debug.LogError("GameLogicManagerMB: playerPrefab is not assigned.", this);
                return;
            }

            PlayerMB existingPlayer = ResolvePlayerInstance();
            if (TryResolveBoundPlayerRef(existingPlayer, out EntityRef existingPlayerRef))
            {
                existingPlayer.TeleportToSpawnPoint(position, rotation);
                playerInstance = existingPlayer;
                OnPlayerSpawned?.Invoke(playerInstance);
                playerRef = existingPlayerRef;
                sceneKernel.Cameras?.SetTrackedPlayer(playerRef);
            }
            else
            {
                EntitySpawnResult result = SpawnPlayer(player, position, rotation);
                playerInstance = ResolveSpawnedPlayer(result.GameObject);

                if (playerInstance == null)
                {
                    Debug.LogError("GameLogicManagerMB: Spawned player prefab does not contain PlayerMB.", result.GameObject);
                    return;
                }

                OnPlayerSpawned?.Invoke(playerInstance);
                playerRef = result.Entity;
                sceneKernel.Cameras?.SetTrackedPlayer(playerRef);
            }
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
                RegisterThirdPersonTargetForHandoff(playerInstance, useCameraTarget: false);
            }
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, true);
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, false);
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Interaction.CanInteract, EntityMoveMotorMB.GameLogicTag, false);
        }
        public EntitySpawnResult SpawnPlayer(EntityMB player, Vector3 position, Quaternion rotation)
        {
            EntitySpawnResult result = sceneKernel.Spawner.Spawn(new EntitySpawnRequest(player.gameObject, transform, position, rotation));
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



    }

    internal readonly struct RetryCheckpointSnapshot
    {
        public RetryCheckpointSnapshot(
            StageCheckpointSnapshot stageCheckpoint,
            Vector3 playerPosition,
            Quaternion playerRotation,
            BombMB preferredBomb,
            RetryBombSnapshot[] bombSnapshots)
        {
            StageCheckpoint = stageCheckpoint;
            PlayerPosition = playerPosition;
            PlayerRotation = playerRotation;
            PreferredBomb = preferredBomb;
            BombSnapshots = bombSnapshots ?? Array.Empty<RetryBombSnapshot>();
        }

        public StageCheckpointSnapshot StageCheckpoint { get; }
        public Vector3 PlayerPosition { get; }
        public Quaternion PlayerRotation { get; }
        public BombMB PreferredBomb { get; }
        public IReadOnlyList<RetryBombSnapshot> BombSnapshots { get; }
    }

    internal readonly struct RetryBombSnapshot
    {
        public RetryBombSnapshot(
            BombMB bomb,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            bool activeSelf,
            object checkpointState)
        {
            Bomb = bomb;
            Parent = parent;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            ActiveSelf = activeSelf;
            CheckpointState = checkpointState;
        }

        public BombMB Bomb { get; }
        public Transform Parent { get; }
        public Vector3 LocalPosition { get; }
        public Quaternion LocalRotation { get; }
        public Vector3 LocalScale { get; }
        public bool ActiveSelf { get; }
        public object CheckpointState { get; }
        public bool IsValid => Bomb != null && CheckpointState != null;
    }
}