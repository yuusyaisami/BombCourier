using System.Collections.Generic;
using BC.Bomb;
using BC.Camera;
using BC.Gimmick;
using BC.Item;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace BC.Stage
{
    // ステージPrefabのRootに必ず置く、ランタイム参照の集約先。
    // StageManager はこのコンポーネントだけを見て、必要な参照を解決する。
    public sealed class MapRuntimeMB : MonoBehaviour
    {
        [Header("Map References")]
        [FormerlySerializedAs("introCameraPath")]
        [SerializeField] private CameraPathSequenceAuthoringMB cameraPath;
        [SerializeField] private BreakableGateObjectMB goalGate;
        [SerializeField] private GoalData goalData;
        [SerializeField] private BonusObjectMB bonusObject; // ステージ内のBonusObjectの参照 (スコア計算に使います。)
        [SerializeField] private List<BombMB> bombs = new List<BombMB>();
        [SerializeField] private List<PlayerSpawnPointMB> spawnPoints = new List<PlayerSpawnPointMB>();
        [SerializeField] private List<GodHandObjectMB> godHandObjects = new List<GodHandObjectMB>();

        public CameraPathSequenceAuthoringMB CameraPath => cameraPath;
        [System.Obsolete("Use CameraPath instead.")]
        public CameraPathSequenceAuthoringMB IntroCameraPath => cameraPath;
        public BreakableGateObjectMB GoalGate => goalGate;
        public GoalData GoalData => goalGate != null && goalGate.GoalData != null ? goalGate.GoalData : goalData;
        public IReadOnlyList<BombMB> Bombs => bombs;
        public IReadOnlyList<PlayerSpawnPointMB> SpawnPoints => spawnPoints;
        public IReadOnlyList<GodHandObjectMB> GodHandObjects => godHandObjects;
        public BonusObjectMB BonusObject => bonusObject;

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
            cameraPath = GetComponentInChildren<CameraPathSequenceAuthoringMB>(true);

            goalGate = null;
            goalData = null;
            bonusObject = GetComponentInChildren<BonusObjectMB>(true);
            BreakableGateObjectMB[] goalGates = GetComponentsInChildren<BreakableGateObjectMB>(true);
            for (int i = 0; i < goalGates.Length; i++)
            {
                BreakableGateObjectMB candidateGate = goalGates[i];
                GoalData candidate = candidateGate != null ? candidateGate.GoalData : null;
                if (candidateGate == null || !candidateGate.IsGoalGate || candidate == null)
                {
                    continue;
                }

                goalGate = candidateGate;
                goalData = candidate;
                break;
            }
        }
    }
}
