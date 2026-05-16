using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BC.Animation;
using BC.Base;
using BC.Bomb;
using BC.Gimmick;
using BC.Stage;
using BC.UI;
using BombCourier.CameraIntro;
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
        [SerializeField] private IntroCameraSequenceRunner introCameraSequenceRunner; // イントロカメラのシーケンスランナー。ステージごとに異なるシーケンスを再生するために使用する。
        [SerializeField] private EntityMB playerPrefab; // プレイヤーのプレハブ
        [Header("Debug")][SerializeField] private Transform debugStageInstance; // デバッグ用のステージインスタンス。エディタで直接割り当てることができます。
        // 爆弾Ref
        private BombMB currentBomb;

        private GodHandObjectMB currentGodHand; // 現在つかまっているGodHandの参照。複数のGodHandが存在する場合に、どのGodHandにつかまっているかを管理するために使用する。
        private MapRuntimeMB currentMapRuntime; // 現在ロードされているマップのランタイム参照。
        private GameObject stageInstance; // 現在のステージのインスタンス。ステージをリセットするときに使用する。
        private PlayerMB playerInstance; // プレイヤーのインスタンス。プレイヤーをスポーンさせるときに使用する。
        private GoalData currentGoalData; // 現在のゴールのデータ。ゴールに到達したときの処理に使用する。
        private IntroCameraPathAuthoring currentIntroCameraPath; // 現在のイントロカメラパス。
        private EntityRef playerRef; // プレイヤーのEntityRef。プレイヤーの状態を管理するために使用する。
        public Action<PlayerMB> OnPlayerSpawned; // プレイヤーがスポーンしたときに呼び出されるイベント
        public Action<BombMB> OnCurrentBombChanged; // 現在の爆弾が変わったときに呼び出されるイベント
        public Action ReloadState; // ステージをリロードする必要があるときに呼び出されるイベント
        public Action ExplodedState; // 爆弾が爆発したときに呼び出されるイベント
        private SceneKernel sceneKernel; // シーンカーネルの参照。シーン全体の状態を管理するために使用する。

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

        public void OnStageChanged(GameState newState)
        {
            if (newState == GameState.Starting)
            {
                LoadStageAsync(0).Forget(); // 最初のステージをロードする
            }
            else if (newState == GameState.Intro)
            {
                PlayIntroCameraSequence().Forget(); // イントロカメラのシーケンスを再生する
            }
            else if (newState == GameState.SetupPlaying)
            {
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, true);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, true);
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
                LoadStageAsync(currentGameStage + 1).Forget(); // 次のステージをロードする
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

            // Playerを止める
            await moveController.MoveToAsync(currentGoalData.Target, 0.1f);
            if (sceneKernel != null && playerRef.IsValid)
            {
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, false);
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, false);
            }

            if (currentGodHand != null)
            {
                currentGodHand.SetTargetPosition(); // GodHandを元の位置に戻す
            }


        }
        private async UniTask NextStageAsync()
        {
            IPlayerAnimatorParameterController playerParameterController = playerInstance.GetComponent<IPlayerAnimatorParameterController>();
            playerParameterController.SetBool(playerParameterController.IsNextStageParameter, true); // プレイヤーのアニメーションパラメーターを更新して、次のステージに進むためのアニメーションを再生する
            currentGodHand.Catch(playerInstance.transform); // プレイヤーをGodHandにつかまらせる
            // cinemachineCameraの方向を、Playerに向ける
            await LookAtAsync(currentGoalData.GoalCamera.transform, playerInstance.transform.position);
            await currentGodHand.MoveToAsync(1.7f); // GodHandを移動させる
            // 次のステージに進むための処理
            await LoadStageAsync(currentGameStage + 1);
        }
        private async UniTask LookAtAsync(Transform origin, Vector3 targetPosition, float duration = 1f)
        {
            if (currentGoalData == null || currentGoalData.GoalCamera == null)
            {
                Debug.LogError("GameLogicManagerMB: GoalCamera is not assigned for LookAtAsync.", this);
                return;
            }

            // GoalCameraをPlayerに向ける
            CinemachineCamera goalCamera = currentGoalData.GoalCamera;
            float elapsedTime = 0f;
            Quaternion initialRotation = goalCamera.transform.rotation;
            Vector3 directionToTarget = (targetPosition - goalCamera.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                goalCamera.transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
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
        private async UniTask PlayIntroCameraSequence()
        {
            if (introCameraSequenceRunner == null)
            {
                Debug.LogError("GameLogicManagerMB: introCameraSequenceRunner is not assigned.", this);
                return;
            }

            if (currentIntroCameraPath == null)
            {
                Debug.LogError("GameLogicManagerMB: IntroCameraPath is not resolved from MapRuntimeMB.", this);
                return;
            }

            await introCameraSequenceRunner.Play(currentIntroCameraPath);
            PlayerMB resolvedPlayer = ResolvePlayerInstance();
            if (resolvedPlayer != null)
            {
                resolvedPlayer.PlayRespawnEffect(); // プレイヤーのスポーンエフェクトを再生する
            }
            GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);
            Debug.Log("Intro camera sequence completed. Changing state to SetupPlaying.");
        }
        private async UniTask ReloadStageAsync()
        {
            // 現在のステージをリロードする処理
            if (stageInstance != null)
            {
                Destroy(stageInstance);
            }
            StageManagerMB.Instance.ReloadStage();
            playerInstance.ResetPlayer();
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
        }
        private void HandleCurrentBombExploded(BombMB bomb)
        {
            if (bomb != currentBomb) return; // currentBomb以外の爆弾が爆発した場合は無視する
            GameStateManagerMB.Instance.ChangeState(GameState.Exploded);
            ExplodedState?.Invoke();
        }

        public void LoadGameStage()
        {
            StageLoadResult result;

            if (debugStageInstance == null)
            {
                result = StageManagerMB.Instance.LoadStage(currentGameStage);
                // playerをテレポートさせる
                if (result.spawnPoints.Count > 0)
                {
                    // とりあえず最初のスポーンポイントにテレポートさせる
                    PlayerSpawnPointMB spawnPoint = result.spawnPoints[0];
                    SpawnAndTeleportPlayer(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                }
            }
            else
            {
                result = StageManagerMB.Instance.ResolveStageRuntime(debugStageInstance.gameObject);
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
            currentIntroCameraPath = result.introCameraPath;
            currentGodHand = result.godHandObjects.Count > 0 ? result.godHandObjects[0] : null;
            SetCurrentBomb(result.bombs.Count > 0 ? result.bombs[0] : null);

            if (currentGoalData == null) Debug.LogError("GameLogicManagerMB: GoalData is not resolved from the stage runtime.", this);
            if (currentIntroCameraPath == null) Debug.LogError("GameLogicManagerMB: IntroCameraPath is not resolved from the stage runtime.", this);


            GameStateManagerMB.Instance.ChangeState(GameState.Intro);
        }
        // ゲーム内にプレイヤーがいた場合はTeleportのみ、いない場合はSpawnしてからTeleportする
        public void SpawnAndTeleportPlayer(EntityMB player, Vector3 position = default, Quaternion rotation = default)
        {
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
                playerInstance = result.GameObject.GetComponent<PlayerMB>();
                OnPlayerSpawned?.Invoke(playerInstance);
                playerRef = result.Entity;
            }
            // 一時的にPlayerの動きを止める
            if (!playerRef.IsValid)
            {
                Debug.LogError("GameLogicManagerMB: PlayerRef is not valid.", this);
            }
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveBySystem, EntityMoveMotorMB.GameLogicTag, true);
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMoveByInput, EntityMoveMotorMB.GameLogicTag, false);
        }
        public EntitySpawnResult SpawnPlayer(EntityMB player, Vector3 position, Quaternion rotation)
        {
            EntitySpawnResult result = sceneKernel.Spawner.Spawn(new EntitySpawnRequest(player.gameObject, transform, position, rotation));
            PlayerMB newPlayer = result.GameObject.GetComponent<PlayerMB>();
            newPlayer.TeleportToSpawnPoint(position, rotation);
            return result;
        }



    }
}