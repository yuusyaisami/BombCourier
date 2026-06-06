using System.Collections.Generic;
using BC.Bomb;
using BC.Camera;
using BC.Gimmick;
using BC.Item;
using BC.Rendering;
using BC.Tutorial;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace BC.Stage
{
    // どのライトに影を持たせて遮蔽を出すかのポリシー。
    // RoomMainLight=部屋の主光(Spot/Point), Directional=方向光(太陽), Both=両方, None=影なし。
    public enum MapShadowCasterMode
    {
        None = 0,
        RoomMainLight = 1,
        Directional = 2,
        Both = 3,
    }

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
        [SerializeField] private TutorialStageAuthoringMB tutorialStage;
        [SerializeField] private string entityMaterialDatasetKind = EntityMaterialSetSO.DefaultDatasetKind;

        [Header("Shadow Policy")]
        [Tooltip("実行時にプラットフォーム別の影キャスター設定を適用します。")]
        [SerializeField] private bool applyShadowPolicy = true;
        [Tooltip("PC/スタンドアロンでの影キャスター。既定は部屋の主光。")]
        [SerializeField] private MapShadowCasterMode pcShadowMode = MapShadowCasterMode.RoomMainLight;
        [Tooltip("モバイルでの影キャスター。追加光シャドウは重い/無効なため既定は方向光。")]
        [SerializeField] private MapShadowCasterMode mobileShadowMode = MapShadowCasterMode.Directional;
        [Tooltip("WebGLでの影キャスター。既定は方向光。")]
        [SerializeField] private MapShadowCasterMode webglShadowMode = MapShadowCasterMode.Directional;
        [Tooltip("影を有効化する際のシャドウ種別。")]
        [SerializeField] private LightShadows shadowQuality = LightShadows.Soft;
        [Tooltip("部屋の主光を明示指定する場合に設定（未設定なら最も明るい Spot/Point を自動採用）。")]
        [SerializeField] private Light roomMainLightOverride;
        [Tooltip("方向光を明示指定する場合に設定（未設定なら最も明るい Directional を自動採用）。")]
        [SerializeField] private Light directionalLightOverride;

        public CameraPathSequenceAuthoringMB CameraPath => cameraPath;
        [System.Obsolete("Use CameraPath instead.")]
        public CameraPathSequenceAuthoringMB IntroCameraPath => cameraPath;
        public BreakableGateObjectMB GoalGate => goalGate;
        public GoalData GoalData => goalGate != null && goalGate.GoalData != null ? goalGate.GoalData : goalData;
        public IReadOnlyList<BombMB> Bombs => bombs;
        public IReadOnlyList<PlayerSpawnPointMB> SpawnPoints => spawnPoints;
        public IReadOnlyList<GodHandObjectMB> GodHandObjects => godHandObjects;
        public BonusObjectMB BonusObject => bonusObject;
        public TutorialStageAuthoringMB TutorialStage => tutorialStage;
        public string EntityMaterialDatasetKind => entityMaterialDatasetKind;
        private void Awake()
        {
            // Prefab の保存状態に依存せず、実行時は Root から必ず再収集する。
            AutoCollect();

            if (applyShadowPolicy)
            {
                ApplyShadowPolicy(ResolveActiveShadowMode());
            }
        }

        // ビルドターゲットに応じた影キャスターモードを返す。
        private MapShadowCasterMode ResolveActiveShadowMode()
        {
#if UNITY_WEBGL
            return webglShadowMode;
#else
            return Application.isMobilePlatform ? mobileShadowMode : pcShadowMode;
#endif
        }

        // 指定モードに従い、部屋の主光と方向光の Light.shadows を切り替える。
        // 注意: 追加光(Spot/Point)の影は URP の Additional Light Shadows が有効なプラットフォームでのみ実際に描画される
        // （モバイル/WebGL では無効な構成のため、これらは方向光を既定にしている）。
        private void ApplyShadowPolicy(MapShadowCasterMode mode)
        {
            Light roomMainLight = ResolveRoomMainLight();
            Light directionalLight = ResolveDirectionalLight();

            bool roomCasts = mode == MapShadowCasterMode.RoomMainLight || mode == MapShadowCasterMode.Both;
            bool directionalCasts = mode == MapShadowCasterMode.Directional || mode == MapShadowCasterMode.Both;

            if (roomMainLight != null)
            {
                roomMainLight.shadows = roomCasts ? shadowQuality : LightShadows.None;
            }

            if (directionalLight != null)
            {
                directionalLight.shadows = directionalCasts ? shadowQuality : LightShadows.None;
            }
        }

        // 最も明るい Spot/Point ライトを部屋の主光として解決する（override 優先）。
        private Light ResolveRoomMainLight()
        {
            if (roomMainLightOverride != null)
            {
                return roomMainLightOverride;
            }

            Light best = null;
            Light[] lights = GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light candidate = lights[i];
                if (candidate == null || (candidate.type != LightType.Spot && candidate.type != LightType.Point))
                {
                    continue;
                }

                if (best == null || candidate.intensity > best.intensity)
                {
                    best = candidate;
                }
            }

            return best;
        }

        // 最も明るい Directional ライトを方向光として解決する（override 優先）。
        private Light ResolveDirectionalLight()
        {
            if (directionalLightOverride != null)
            {
                return directionalLightOverride;
            }

            Light best = null;
            Light[] lights = GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light candidate = lights[i];
                if (candidate == null || candidate.type != LightType.Directional)
                {
                    continue;
                }

                if (best == null || candidate.intensity > best.intensity)
                {
                    best = candidate;
                }
            }

            return best;
        }

        // Scene ビューで見た目を確認するための、PC モード適用ボタン（エディタ専用）。
        [Button("Apply Shadow Policy (PC Preview)")]
        [ContextMenu("Apply Shadow Policy (PC Preview)")]
        private void EditorPreviewShadowPolicy()
        {
            ApplyShadowPolicy(pcShadowMode);
#if UNITY_EDITOR
            Light roomMainLight = ResolveRoomMainLight();
            Light directionalLight = ResolveDirectionalLight();
            if (roomMainLight != null)
            {
                UnityEditor.EditorUtility.SetDirty(roomMainLight);
            }

            if (directionalLight != null)
            {
                UnityEditor.EditorUtility.SetDirty(directionalLight);
            }

            UnityEditor.SceneView.RepaintAll();
#endif
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
            tutorialStage = GetComponentInChildren<TutorialStageAuthoringMB>(true);

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
