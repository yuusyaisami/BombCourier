using System.Collections.Generic;
using BC.Bomb;
using BC.Gimmick;
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
        public GoalData goalData; // ゴールのデータ
        public GameObject stageInstance; // ステージのインスタンス
        public List<GodHandObjectMB> godHandObjects; // ステージ内のGodHandオブジェクトのリスト
        public MapRuntimeMB mapRuntime; // ステージのRootで参照を集約するランタイム
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
                return CreateEmptyResult();
            }

            StageDataSO data = stageData[stageIndex];
            GameObject stageInstance = Instantiate(data.stagePrefab, stageRoot);
            return ResolveStageRuntime(stageInstance);
        }

        public StageLoadResult ResolveStageRuntime(GameObject stageInstance)
        {
            if (stageInstance == null)
            {
                Debug.LogError($"{nameof(StageManagerMB)}: stageInstance is null.", this);
                return CreateEmptyResult();
            }

            // ステージの必要参照は Root の MapRuntimeMB に集約し、Load 時の全探索をやめる。
            MapRuntimeMB mapRuntime = stageInstance.GetComponent<MapRuntimeMB>();
            if (mapRuntime == null)
            {
                Debug.LogError($"{nameof(StageManagerMB)}: {nameof(MapRuntimeMB)} is not attached to the stage root.", stageInstance);
                return CreateEmptyResult(stageInstance);
            }

            return new StageLoadResult
            {
                bombs = new List<BombMB>(mapRuntime.Bombs),
                spawnPoints = new List<PlayerSpawnPointMB>(mapRuntime.SpawnPoints),
                introCameraPath = mapRuntime.IntroCameraPath,
                goalData = mapRuntime.GoalData,
                stageInstance = stageInstance,
                godHandObjects = new List<GodHandObjectMB>(mapRuntime.GodHandObjects),
                mapRuntime = mapRuntime,
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

        private static StageLoadResult CreateEmptyResult(GameObject stageInstance = null)
        {
            return new StageLoadResult
            {
                bombs = new List<BombMB>(),
                spawnPoints = new List<PlayerSpawnPointMB>(),
                introCameraPath = null,
                goalData = null,
                stageInstance = stageInstance,
                godHandObjects = new List<GodHandObjectMB>(),
                mapRuntime = null,
            };
        }
    }
}