using System.Collections.Generic;
using BC.Bomb;
using BC.Gimmick;
using BombCourier.CameraIntro;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Stage
{
    // ステージPrefabのRootに必ず置く、ランタイム参照の集約先。
    // StageManager はこのコンポーネントだけを見て、必要な参照を解決する。
    public sealed class MapRuntimeMB : MonoBehaviour
    {
        [Header("Map References")]
        [SerializeField] private IntroCameraPathAuthoring introCameraPath;
        [SerializeField] private BreakableGateObjectMB goalGate;
        [SerializeField] private GoalData goalData;
        [SerializeField] private List<BombMB> bombs = new List<BombMB>();
        [SerializeField] private List<PlayerSpawnPointMB> spawnPoints = new List<PlayerSpawnPointMB>();
        [SerializeField] private List<GodHandObjectMB> godHandObjects = new List<GodHandObjectMB>();

        public IntroCameraPathAuthoring IntroCameraPath => introCameraPath;
        public GoalData GoalData => goalGate != null && goalGate.GoalData != null ? goalGate.GoalData : goalData;
        public IReadOnlyList<BombMB> Bombs => bombs;
        public IReadOnlyList<PlayerSpawnPointMB> SpawnPoints => spawnPoints;
        public IReadOnlyList<GodHandObjectMB> GodHandObjects => godHandObjects;

        private void Awake()
        {
            // Prefab の保存状態に依存せず、実行時は Root から必ず再収集する。
            AutoCollect();
        }

        private void Reset()
        {
            AutoCollect();
        }

        [Button("Auto Collect")]
        [ContextMenu("Auto Collect")]
        private void AutoCollect()
        {
            // 手動設定を基本にしつつ、Prefab更新時に必要参照を一括で再収集できるようにする。
            bombs = new List<BombMB>(GetComponentsInChildren<BombMB>(true));
            spawnPoints = new List<PlayerSpawnPointMB>(GetComponentsInChildren<PlayerSpawnPointMB>(true));
            godHandObjects = new List<GodHandObjectMB>(GetComponentsInChildren<GodHandObjectMB>(true));
            introCameraPath = GetComponentInChildren<IntroCameraPathAuthoring>(true);

            goalGate = null;
            goalData = null;
            BreakableGateObjectMB[] goalGates = GetComponentsInChildren<BreakableGateObjectMB>(true);
            for (int i = 0; i < goalGates.Length; i++)
            {
                GoalData candidate = goalGates[i] != null ? goalGates[i].GoalData : null;
                if (candidate == null)
                {
                    continue;
                }

                goalGate = goalGates[i];
                goalData = candidate;
                break;
            }
        }
    }
}
