using System.Collections.Generic;
using BC.Bomb;
using BC.Camera;
using BC.Gimmick;
using BC.Stage;
using BC.Stage.Snapshot;
using BC.Tutorial;
using Cysharp.Threading.Tasks;
using UnityEngine;
using BC.Item;
namespace BC.Manager
{
    // ステージ prefab のロード、ランタイム参照の解決、チェックポイント操作をまとめる stage 司令塔。
    public struct StageLoadResult
    {
        public string StageName; // ステージ表示名 (Intro UI などで使う)
        public List<BombMB> bombs; // ステージ内の爆弾のリスト
        public List<PlayerSpawnPointMB> spawnPoints; // ステージ内のプレイヤースポーンポイントのリスト
        public CameraPathSequenceAuthoringMB cameraPath; // カメラパス
        public GoalData goalData; // ゴールのデータ
        public GameObject stageInstance; // ステージのインスタンス
        public List<GodHandObjectMB> godHandObjects; // ステージ内のGodHandオブジェクトのリスト
        public MapRuntimeMB mapRuntime; // ステージのRootで参照を集約するランタイム
        public BonusObjectMB bonusObject; // ステージ内のBonusObjectの参照 (スコア計算に使います。)
        public TutorialStageAuthoringMB tutorialStage; // ステージ固有のチュートリアル定義
        public float ClearTimeThreshold; // ゴールデータにクリアタイムの閾値がある場合はそれを使用し、ない場合はデフォルト値を返す
        public string EntityMaterialDatasetKind; // ステージで使用する EntityMaterialSet の dataset kind
    }

    // ステージの読み込みとチェックポイント処理を担当する MonoBehaviour。
    // 実際のゲーム進行は GameLogicManagerMB が受け持ち、ここは stage 生成/復元に集中する。
    public class StageManagerMB : MonoBehaviour
    {
        public static StageManagerMB Instance { get; private set; }
        private void Awake()
        {
            // 1 シーン 1 インスタンス前提の簡易 singleton。
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // 現在の stage instance を保持し、次回 Load 時に安全に破棄する。
        private Transform stageInstance;
        [SerializeField] private StageRegistrySO stageData; // ステージデータのScriptableObject
        [SerializeField] private StageSnapshotServiceMB snapshotService; // ステージ開始ベースラインのスナップショットサービス
        [SerializeField] private Transform stageRoot; // ステージの親オブジェクト

        // index 指定で stage prefab を instantiate し、その場で runtime 参照へ変換する。
        public StageLoadResult LoadStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= stageData.StageData.Count)
            {
                Debug.LogError($"StageManagerMB: Invalid stage index {stageIndex}.", this);
                return CreateEmptyResult();
            }

            // 前のステージのインスタンスを削除する
            if (this.stageInstance != null)
            {
                Destroy(this.stageInstance.gameObject);
            }

            StageData data = stageData.StageData[stageIndex];
            Transform parent = stageRoot != null && stageRoot.gameObject.scene.name != "DontDestroyOnLoad"
                ? stageRoot
                : null;
            GameObject stageInstance = parent != null
                ? Instantiate(data.stagePrefab, parent)
                : Instantiate(data.stagePrefab);
            this.stageInstance = stageInstance.transform;
            return ResolveStageRuntime(stageInstance, data);
        }

        // 既にある stage instance から runtime 情報だけ取り出したいときの入口。
        public StageLoadResult ResolveStageRuntime(GameObject stageInstance)
        {
            return ResolveStageRuntime(stageInstance, new StageData());
        }

        // MapRuntimeMB に集約された参照を stage 起動時に拾い直す。
        public StageLoadResult ResolveStageRuntime(GameObject stageInstance, StageData data)
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
                StageName = data != null ? data.stageName : string.Empty,
                bombs = new List<BombMB>(mapRuntime.Bombs),
                spawnPoints = new List<PlayerSpawnPointMB>(mapRuntime.SpawnPoints),
                cameraPath = mapRuntime.CameraPath,
                goalData = mapRuntime.GoalData,
                stageInstance = stageInstance,
                godHandObjects = new List<GodHandObjectMB>(mapRuntime.GodHandObjects),
                mapRuntime = mapRuntime,
                ClearTimeThreshold = data.clearTimeThreshold, // ゴールデータにクリアタイムの閾値がある場合はそれを使用し、ない場合はデフォルト値を返す
                bonusObject = mapRuntime.BonusObject, // ステージ内のBonusObjectの参照 (スコア計算に使います。)
                tutorialStage = mapRuntime.TutorialStage,
                EntityMaterialDatasetKind = mapRuntime.EntityMaterialDatasetKind,
            };
        }

        // ステージ開始時状態のベースラインを取得する（ステージ毎に1回）。
        public void CaptureStageStartBaseline()
        {
            if (snapshotService == null)
            {
                Debug.LogError($"{nameof(StageManagerMB)}: snapshotService is not assigned.", this);
                return;
            }

            snapshotService.CaptureBaseline();
        }

        public bool HasBaseline => snapshotService != null && snapshotService.HasBaseline;

        // 開始時状態へ全対象を戻す reload 入口。
        public void RestoreStageStartBaseline()
        {
            if (snapshotService == null)
            {
                Debug.LogError($"{nameof(StageManagerMB)}: snapshotService is not assigned.", this);
                return;
            }

            snapshotService.RestoreBaseline();
        }

        // 退避済みベースラインを消す。
        public void ClearBaseline()
        {
            snapshotService?.ClearBaseline();
        }

        // 失敗時でも呼び出し側が扱いやすいよう、空の結果を返す helper。
        private static StageLoadResult CreateEmptyResult(GameObject stageInstance = null)
        {
            return new StageLoadResult
            {
                StageName = string.Empty,
                bombs = new List<BombMB>(),
                spawnPoints = new List<PlayerSpawnPointMB>(),
                cameraPath = null,
                goalData = null,
                stageInstance = stageInstance,
                godHandObjects = new List<GodHandObjectMB>(),
                mapRuntime = null,
                tutorialStage = null,
            };
        }
    }
}
