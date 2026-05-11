using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BC.Base;
using BC.Bomb;
using BombCourier.CameraIntro;
using Cysharp.Threading.Tasks;
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

        [SerializeField]
        private IntroCameraSequenceRunner introCameraSequenceRunner; // イントロカメラのシーケンスランナー。ステージごとに異なるシーケンスを再生するために使用する。
        [SerializeField] private EntityMB playerPrefab; // プレイヤーのプレハブ
        [Header("Debug")][SerializeField] private Transform debugStageInstance; // デバッグ用のステージインスタンス。エディタで直接割り当てることができます。
        // 爆弾Ref
        private BombMB currentBomb;
        private GameObject stageInstance; // 現在のステージのインスタンス。ステージをリセットするときに使用する。
        private PlayerMB playerInstance; // プレイヤーのインスタンス。プレイヤーをスポーンさせるときに使用する。
        private EntityRef playerRef; // プレイヤーのEntityRef。プレイヤーの状態を管理するために使用する。
        private Action<PlayerMB> onPlayerSpawned; // プレイヤーがスポーンしたときに呼び出されるイベント
        public Action<BombMB> OnCurrentBombChanged; // 現在の爆弾が変わったときに呼び出されるイベント
        public Action ReloadState; // ステージをリロードする必要があるときに呼び出されるイベント
        public Action ExplodedState; // 爆弾が爆発したときに呼び出されるイベント
        private SceneKernel sceneKernel; // シーンカーネルの参照。シーン全体の状態を管理するために使用する。

        private int currentGameStage;

        public BombMB CurrentBomb => currentBomb;

        private void Start()
        {
            sceneKernel = transform.GetComponentInChildren<SceneKernelMB>().Kernel;
            // 最初のステージをロードする
            GameStateManagerMB.Instance.StateMachine.Subscribe(OnStageChanged);
            GameStateManagerMB.Instance.ChangeState(GameState.Starting);

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
                sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMove, PlayerMoveController.GameLogicTag, true);
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
            else if (newState == GameState.StageClear)
            {
                // ステージクリアしたときの処理
            }
            else if (newState == GameState.GameOver)
            {
                // ゲームオーバーになったときの処理
            }
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

            await introCameraSequenceRunner.Play(stageInstance.GetComponentInChildren<IntroCameraPathAuthoring>());
            playerInstance.PlayRespawnEffect(); // プレイヤーのスポーンエフェクトを再生する
            GameStateManagerMB.Instance.ChangeState(GameState.SetupPlaying);
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
            if (debugStageInstance == null)
            {
                StageLoadResult result = StageManagerMB.Instance.LoadStage(currentGameStage);
                if (result.bombs.Count > 0)
                {
                    SetCurrentBomb(result.bombs[0]);
                }
                // playerをテレポートさせる
                if (result.spawnPoints.Count > 0)
                {
                    // とりあえず最初のスポーンポイントにテレポートさせる
                    PlayerSpawnPointMB spawnPoint = result.spawnPoints[0];
                    SpawnAndTeleportPlayer(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                }
                // instanceを保存
                stageInstance = result.stageInstance;
            }
            else
            {
                SetCurrentBomb(transform.GetComponentInChildren<BombMB>());
                playerInstance = transform.GetComponentInChildren<PlayerMB>();
                playerRef = playerInstance.GetComponent<EntityMB>().Entity;
                stageInstance = debugStageInstance.gameObject;
            }


            GameStateManagerMB.Instance.ChangeState(GameState.Intro);
        }
        // ゲーム内にプレイヤーがいた場合はTeleportのみ、いない場合はSpawnしてからTeleportする
        public void SpawnAndTeleportPlayer(EntityMB player, Vector3 position = default, Quaternion rotation = default)
        {
            PlayerMB existingPlayer = transform.GetComponentInChildren<PlayerMB>();
            if (existingPlayer != null)
            {
                existingPlayer.TeleportToSpawnPoint(position, rotation);
                playerInstance = existingPlayer;
                playerRef = existingPlayer.GetComponent<EntityMB>().Entity;
            }
            else
            {
                EntitySpawnResult result = SpawnPlayer(player, position, rotation);
                playerInstance = result.GameObject.GetComponent<PlayerMB>();
                playerRef = result.Entity;
            }
            // 一時的にPlayerの動きを止める
            sceneKernel.ValueStore.SetBoolModifier(playerRef, ValueKeys.Move.CanMove, PlayerMoveController.GameLogicTag, false);
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