using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BC.Animation;
using BC.Base;
using BC.Bomb;
using BC.Camera;
using BC.Gimmick;
using BC.Item;
using BC.Stage;
using BC.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
namespace BC.Manager
{
    public class GameLogicManagerMB : UnityEngine.MonoBehaviour
    {
        // ゲームのロジックを管理するクラス
        // 例えば、ゲームの状態管理、スコア管理、レベル管理などを担当することができます。
        public static GameLogicManagerMB Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        [Header("References")]
        [SerializeField] private UIFadeEffectMB uiFadeEffectMB; // UIのフェードエフェクトを管理するクラス。シーン全体のフェードイン・フェードアウトなどを担当する。
        [SerializeField] private UIGameSceneManagerMB uiGameSceneManagerMB; // ゲームシーンのUIを管理するクラス。ステージ選択画面やゲームオーバー画面などのUIを担当する。
        [SerializeField] private EntityMB playerPrefab; // プレイヤーのプレハブ
        [Header("Debug")][SerializeField] private Transform debugStageInstance; // デバッグ用のステージインスタンス。エディタで直接割り当てることができます。
        // 爆弾Ref
        private BombMB currentBomb;

        private GodHandObjectMB currentGodHand; // 現在つかまっているGodHandの参照。複数のGodHandが存在する場合に、どのGodHandにつかまっているかを管理するために使用する。
        private MapRuntimeMB currentMapRuntime; // 現在ロードされているマップのランタイム参照。
        private GameObject stageInstance; // 現在のステージのインスタンス。ステージをリセットするときに使用する。
        private PlayerMB playerInstance; // プレイヤーのインスタンス。プレイヤーをスポーンさせるときに使用する。
        private GoalData currentGoalData; // 現在のゴールのデータ。ゴールに到達したときの処理に使用する。
        private CameraPathSequenceAuthoringMB currentCameraPath; // 現在のカメラパス。
        private EntityRef playerRef; // プレイヤーのEntityRef。プレイヤーの状態を管理するために使用する。
        public Action<PlayerMB> OnPlayerSpawned; // プレイヤーがスポーンしたときに呼び出されるイベント
        public Action<BombMB> OnCurrentBombChanged; // 現在の爆弾が変わったときに呼び出されるイベント
        public Action<BombMB> OnStartBombFuse; // 爆弾のカウントダウンが開始されたときに呼び出されるイベント
        public Action OnEndBombFuse; // 爆弾のカウントダウンが終了したときに呼び出されるイベント
        public Action ReloadState; // ステージをリロードする必要があるときに呼び出されるイベント
        public Action ExplodedState; // 爆弾が爆発したときに呼び出されるイベント
        public Action ExplodedBeforeGoalOpenedState; // 爆弾が爆発し、かつGoal Gateがまだ開いていないときに呼び出されるイベント
        private SceneKernel sceneKernel; // シーンカーネルの参照。シーン全体の状態を管理するために使用する。
        private float timeSinceStartBomb; //爆弾のカウントダウンが開始してからの経過時間を管理するための変数
        private float currentClearTimeThreshold = 60f; // 爆弾のカウントダウンが開始してからこの時間以内にゴールした場合、Fast Clear とみなすための閾値。必要に応じて調整してください。
        private EntityRef gameLogicManagerRef; // GameLogicManager自身のEntityRef。シーンカーネルに登録している場合に使用する。
        public EntityRef SelfEntityRef => gameLogicManagerRef; // GameLogicManagerのEntityRefを外部から参照できるようにするプロパティ
        private BonusObjectMB currentBonusObject; // 現在のステージのBonusObjectへの参照。スコア計算などに使用する。
        private int currentGameStage;

        public BombMB CurrentBomb => currentBomb;
        public PlayerMB PlayerInstance => playerInstance;

        private PlayerMB ResolvePlayerInstance()
        {
            if (playerInstance != null)
            {
                return playerInstance;
            }

            PlayerMB foundPlayer = transform.GetComponentInChildren<PlayerMB>(true);

            if (foundPlayer == null)
            {
                foundPlayer = UnityEngine.Object.FindAnyObjectByType<PlayerMB>();
            }

            if (foundPlayer != null)
            {
                playerInstance = foundPlayer;

                EntityMB entityMB = foundPlayer.GetComponent<EntityMB>();
                playerRef = entityMB != null ? entityMB.Entity : default;
            }

            return playerInstance;
        }

        private void Start()
        {
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
            if (GameStateManagerMB.Instance != null)
            {
                GameStateManagerMB.Instance.StateMachine.Unsubscribe(OnStageChanged);
            }

        }
        private void Update()
        {
            if (currentBomb != null && GameStateManagerMB.Instance.CurrentState == GameState.FusePlaying)
            {
                timeSinceStartBomb += Time.deltaTime;
            }
        }

        public void OnStageChanged(GameState newState)
        {
            if (newState == GameState.Starting)
            {
                LoadStageAsync(0).Forget(); // 最初のステージをロードする
            }
            else if (newState == GameState.Intro)
            {
                PlayCameraPathSequence().Forget(); // ステージ開始時のカメラパスを再生する
            }
            else if (newState == GameState.SetupPlaying)
            {
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, true);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, true);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Interaction.CanInteract, EntityMoveMotorMB.GameLogicTag, true);
            }
            else if (newState == GameState.FusePlaying)
            {
                StageManagerMB.Instance.CaptureStageCheckpoint(); // チェックポイントを保存する
            }
            else if (newState == GameState.Exploded)
            {
                // 爆弾が爆発したときの処理
            }
            else if (newState == GameState.Reload)
            {
                ReloadStageAsync().Forget(); // ステージをリロードする

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

            //一時体にCinemaCameraを切り替える。
            Debug.Log("Switching to goal camera: " + currentGoalData.GoalCamera.name);
            CinemachineCamera goalCamera = currentGoalData.GoalCamera;
            goalCamera.Priority = 100; // カメラの優先度を上げて切り替える

            // UIを非表示にする
            uiGameSceneManagerMB.ShowTopPanel(false); // ゲームシーンのUIを非表示にする
            uiGameSceneManagerMB.ShowBottomPanel(false); // ゲームシーンのUIを非表示にする

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
            PlayerAnimationMB playerAnimationController = playerInstance.GetComponentInChildren<PlayerAnimationMB>();
            playerAnimationController?.SetNextStageActive(true); // プレイヤーのアニメーションパラメーターを更新して、次のステージに進むためのアニメーションを再生する
            currentGodHand.Catch(playerInstance); // プレイヤーをGodHandにつかまらせる
            // cinemachineCameraの方向を、Playerに向ける
            await LookAtAsync(currentGoalData.GoalCamera.transform, playerInstance.transform.position);
            await UniTask.Delay(700);
            await currentGodHand.MoveToAsync(currentGodHand.OriginalPosition, 1.7f); // GodHandを移動させる
            await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f); // フェードアウトさせる
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
        private async UniTask LoadStageAsync(int stageIndex)
        {
            currentGameStage = stageIndex;
            LoadGameStage();

            await UniTask.CompletedTask;
        }
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
            uiGameSceneManagerMB.ShowTopPanel(false, 0f); // ゲームシーンのUIを表示する
            uiGameSceneManagerMB.ShowBottomPanel(false, 0f); // ゲームシーンのUIを表示する
            await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 0.2f, 0.5f); // フェードインさせる
            await CameraManager.Instance.PlayPathAsync(currentCameraPath, playerRef, async () =>
            {
                // カメラパスの再生が完了した後の処理
                await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 1f, 0.5f); // フェードインさせる
            });
            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            if (resolvedPlayer != null)
            {
                resolvedPlayer.PlayRespawnEffect(); // プレイヤーのスポーンエフェクトを再生する
            }
            // 少し待つ
            await UniTask.Delay(800);
            await uiFadeEffectMB.StartFadeAsync(FadeType.TopBottom, 0f, 0.5f); // フェードインさせる

            await UniTask.Delay(200);
            await resolvedPlayer.ShowPlayerAsync(true); // プレイヤーを表示する
            uiGameSceneManagerMB.ShowTopPanel(true); // ゲームシーンのUIを表示する
            uiGameSceneManagerMB.ShowBottomPanel(true); // ゲームシーンのUIを表示する
            GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);

        }
        private async UniTask ReloadStageAsync()
        {
            // 現在のステージをリロードする処理
            if (stageInstance != null)
            {
                UnregisterStageEntities(stageInstance);
                Destroy(stageInstance);
                stageInstance = null;
            }
            StageManagerMB.Instance.ReloadStage();
            ResetPlayer(); // プレイヤーをリセットする
            ReloadState?.Invoke();



            await UniTask.CompletedTask;
        }

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

        public void LoadGameStage()
        {
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
            SetCurrentBomb(result.bombs.Count > 0 ? result.bombs[0] : null);
            currentClearTimeThreshold = result.ClearTimeThreshold;

            if (currentGoalData == null) Debug.LogError("GameLogicManagerMB: GoalData is not resolved from the stage runtime.", this);
            if (currentCameraPath == null) Debug.LogError("GameLogicManagerMB: Camera path is not resolved from the stage runtime.", this);


            GameStateManagerMB.Instance.ChangeState(GameState.Intro);
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
            // プレイヤーを削除する
            if (playerInstance != null)
            {
                sceneKernel.Spawner.Despawn(playerRef);
                playerInstance = null;
                playerRef = default;
            }
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
            if (existingPlayer != null)
            {
                existingPlayer.TeleportToSpawnPoint(position, rotation);
                playerInstance = existingPlayer;
                OnPlayerSpawned?.Invoke(playerInstance);
                playerRef = existingPlayer.GetComponent<EntityMB>().Entity;
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
}