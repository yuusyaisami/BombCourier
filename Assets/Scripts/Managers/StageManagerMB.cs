using System.Collections.Generic;
using BC.Bomb;
using BC.Stage;
using BombCourier.CameraIntro;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace BC.Manager
{
    public struct StageLoadResult
    {
        public List<BombMB> bombs; // ステージ内の爆弾のリスト
        public List<PlayerSpawnPointMB> spawnPoints; // ステージ内のプレイヤースポーンポイントのリスト
        public IntroCameraPathAuthoring introCameraPath; // イントロカメラのパス 
        public GameObject stageInstance; // ステージのインスタンス
    }
    public class StageManagerMB : MonoBehaviour
    {
        // ステージの管理を行うクラス
        // 例えば、ステージの生成、ステージの状態管理、ステージのイベント管理などを担当することができます。
        public static StageManagerMB Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [SerializeField] private List<StageDataSO> stageData; // ステージデータのScriptableObject
        [SerializeField] private StageCheckpointServiceMB checkpointService; // チェックポイントサービス
        [SerializeField] private Transform stageRoot; // ステージの親オブジェクト

        public StageLoadResult LoadStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= stageData.Count)
            {
                Debug.LogError($"StageManagerMB: Invalid stage index {stageIndex}.", this);
                return default;
            }

            StageDataSO data = stageData[stageIndex];
            GameObject stageInstance = Instantiate(data.stagePrefab, stageRoot);
            // スポーンさせたObjectないからBombMBを探してリストで返す
            List<BombMB> bombs = new List<BombMB>();
            foreach (var bomb in stageInstance.GetComponentsInChildren<BombMB>())
            {
                bombs.Add(bomb);
            }
            // スポーン
            List<PlayerSpawnPointMB> spawnPoints = new List<PlayerSpawnPointMB>();
            foreach (var spawnPoint in stageInstance.GetComponentsInChildren<PlayerSpawnPointMB>())
            {
                spawnPoints.Add(spawnPoint);
            }
            // イントロカメラのパス
            IntroCameraPathAuthoring introCameraPath = stageInstance.GetComponentInChildren<IntroCameraPathAuthoring>();

            return new StageLoadResult
            {
                bombs = bombs,
                spawnPoints = spawnPoints,
                introCameraPath = introCameraPath,
                stageInstance = stageInstance
            };
        }
        public void CaptureStageCheckpoint()
        {
            if (checkpointService == null)
            {
                Debug.LogError($"{nameof(StageManagerMB)}: checkpointService is not assigned.", this);
                return;
            }

            checkpointService.Capture();
        }
        // ステージをリロードする (注意: これはStageSaveが入った後に呼び出すこと, またReloadはSaveの一番最新の状態で呼ぶこと)
        public void ReloadStage()
        {
            if (checkpointService == null)
            {
                Debug.LogError($"{nameof(StageManagerMB)}: checkpointService is not assigned.", this);
                return;
            }

            checkpointService.Restore();

        }
    }
}