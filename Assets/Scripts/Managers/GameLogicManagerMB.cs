using System;
using System.Collections.Generic;
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
        // 爆弾Ref
        private BombMB currentBomb;
        private PlayerMB player;
        private Action<PlayerMB> onPlayerSpawned; // プレイヤーがスポーンしたときに呼び出されるイベント
        public Action<BombMB> OnCurrentBombChanged; // 現在の爆弾が変わったときに呼び出されるイベント
        private SceneKernel sceneKernel; // シーンカーネルの参照。シーン全体の状態を管理するために使用する。

        private float currentGameStage;

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
                // イントロが始まったときの処理
            }
            else if (newState == GameState.Playing)
            {
                // プレイが始まったときの処理
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
            LoadGameStage();

            await UniTask.CompletedTask;
        }
        private async UniTask PlayIntroCameraSequence(IntroCameraPathAuthoring path)
        {
            if (introCameraSequenceRunner == null)
            {
                Debug.LogError("GameLogicManagerMB: introCameraSequenceRunner is not assigned.", this);
                return;
            }

            await introCameraSequenceRunner.Play(path);
        }

        public void SetCurrentBomb(BombMB bomb)
        {
            currentBomb = bomb;
            OnCurrentBombChanged?.Invoke(currentBomb);
        }

        public void LoadGameStage()
        {
            StageLoadResult result = StageManagerMB.Instance.LoadStage((int)currentGameStage);
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
            GameStateManagerMB.Instance.ChangeState(GameState.Intro);
        }
        // ゲーム内にプレイヤーがいた場合はTeleportのみ、いない場合はSpawnしてからTeleportする
        public void SpawnAndTeleportPlayer(EntityMB player, Vector3 position = default, Quaternion rotation = default)
        {
            PlayerMB existingPlayer = transform.GetComponentInChildren<PlayerMB>();
            if (existingPlayer != null)
            {
                existingPlayer.TeleportToSpawnPoint(position, rotation);
            }
            else
            {
                EntitySpawnResult result = sceneKernel.Spawner.Spawn(new EntitySpawnRequest(player.gameObject, transform, position, rotation));
                PlayerMB newPlayer = result.GameObject.GetComponent<PlayerMB>();
                newPlayer.TeleportToSpawnPoint(position, rotation);
            }
        }



    }
}