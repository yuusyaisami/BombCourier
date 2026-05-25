using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class MovingPlatformMB : MonoBehaviour, IMovingPlatformMotionSource, ISupportMotionSource
    {
        private static readonly Regex GeneratedNodeSuffixRegex = new($"^{MovingPlatformRailNode.DefaultNodeLabelPrefix}(\\d+)$", RegexOptions.Compiled);

        [Header("Shared Rails")]
        [Tooltip("共有レールのノード一覧です。各レイヤーのルートはこのノードを共通参照します。")]
        [SerializeField]
        private MovingPlatformRailNode[] railNodes = Array.Empty<MovingPlatformRailNode>();

        [Tooltip("共有レール上の接続一覧です。停止中の最短経路探索や、接続点でのレイヤー切り替えに使います。")]
        [SerializeField]
        private MovingPlatformRailConnection[] railConnections = Array.Empty<MovingPlatformRailConnection>();

        [Header("Layers")]
        [LabelText("Layers")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, ListElementLabelName = "LayerName")]
        [Tooltip("足場の挙動レイヤー一覧です。優先度が最も高い有効レイヤーが再生されます。")]
        [SerializeField]
        private MovingPlatformLayer[] layers = Array.Empty<MovingPlatformLayer>();

        [Header("Motion")]
        [Tooltip("実際に移動させる Rigidbody 群です。先頭要素を基準に、残りは初期相対オフセットを保って同時に移動します。")]
        [SerializeField] private Rigidbody[] motionTargets = Array.Empty<Rigidbody>();
        [Tooltip("motionTargets が空の時、子階層の Rigidbody を自動収集します。")]
        [SerializeField] private bool autoCollectChildRigidbodies = true;

        [Header("Signals")]
        [Tooltip("レイヤー切り替えやシーケンス完了時に Kernel Signal を送るかを指定します。")]
        [SerializeField] private bool publishLayerSignals = true;

        [ShowIf(nameof(publishLayerSignals))]
        [Tooltip("レイヤーが有効化された時に送る Signal です。")]
        [SerializeField, SignalDropdown("Gimmick.MovingPlatform")]
        private KernelSignalReference layerEnabledSignal =
            KernelSignalReference.From(Signals.Gimmick.MovingPlatform.LayerEnabled);

        [ShowIf(nameof(publishLayerSignals))]
        [Tooltip("レイヤーが無効化された時に送る Signal です。")]
        [SerializeField, SignalDropdown("Gimmick.MovingPlatform")]
        private KernelSignalReference layerDisabledSignal =
            KernelSignalReference.From(Signals.Gimmick.MovingPlatform.LayerDisabled);

        [ShowIf(nameof(publishLayerSignals))]
        [Tooltip("選択中レイヤーの再生が完了した時に送る Signal です。")]
        [SerializeField, SignalDropdown("Gimmick.MovingPlatform")]
        private KernelSignalReference sequenceCompletedSignal =
            KernelSignalReference.From(Signals.Gimmick.MovingPlatform.SequenceCompleted);

        [Header("Path Visualization")]
        [Tooltip("Scene ビュー上で移動経路を Gizmo として表示するかを指定します。")]
        [SerializeField] private bool showPathInEditor = true;
        [Tooltip("ゲーム再生中にも現在の移動経路を表示するかを指定します。")]
        [SerializeField] private bool showPathInGame;
        [Tooltip("経路の補助ポイント半径です。Editor 上の終点の見やすさに使います。")]
        [SerializeField, Min(0.01f)] private float pathVisualizationPointRadius = 0.08f;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("ゲーム再生中に表示するラインの太さです。")]
        [SerializeField, Min(0.005f)] private float runtimePathLineWidth = 0.05f;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("ゲーム再生中に表示する経路ラインと接続点の不透明度です。色相は Layer 側の表示色を使います。")]
        [SerializeField] private Color runtimePathColor = new(1.0f, 1.0f, 1.0f, 0.9f);
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("LineのMaterial")]
        [SerializeField] private Material runtimePathMaterial;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("runtime path line の発光制御を有効にします。")]
        [SerializeField] private bool enableRuntimePathEmission = true;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("レイヤー表示色が使えない場合に使う発光色フォールバックです。通常は Layer の VisualizationColor が発光色として使われます。")]
        [SerializeField] private Color runtimePathEmissionColor = Color.white;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("有効状態の発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathActiveEmissionStrength = 2.0f;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("無効状態の発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathInactiveEmissionStrength;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("EnvironmentStylizedLit の SimpleBoost 発光も同期します。")]
        [SerializeField] private bool runtimePathSyncSimpleBoost = true;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("有効状態の SimpleBoost 発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathActiveSimpleBoostIntensity = 4.0f;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("無効状態の SimpleBoost 発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathInactiveSimpleBoostIntensity;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("無効状態のラインを薄く表示します。")]
        [SerializeField] private bool dimInactiveRuntimePath = true;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("無効状態で乗算するラインのアルファ値です。")]
        [SerializeField, Range(0.0f, 1.0f)] private float runtimePathInactiveAlphaMultiplier = 0.35f;
        [Header("Debug")]
        [Tooltip("Layer 選択と有効判定の診断ログを出力します。停止原因の切り分け用です。")]
        [SerializeField] private bool enableLayerDebugLog;
        [ShowIf(nameof(enableLayerDebugLog))]
        [Tooltip("診断ログの最小出力間隔です。")]
        [SerializeField, Min(0.2f)] private float layerDebugLogInterval = 1.0f;
        private MovingPlatformLayerRuntime[] runtimes;
        private MovingPlatformRailLayerRoute[] railRoutes;
        private MotionTargetBinding[] motionTargetBindings = Array.Empty<MotionTargetBinding>();
        private MovingPlatformBasePose basePose;
        private SceneKernel sceneKernel;
        private EntityMB selfEntityMB;
        private EventSubscription signalSubscription;
        private int selectedLayerIndex = -1;
        private MovingPlatformRailGraph railGraph;
        private MovingPlatformRailController railController;
        private bool useRailRouting;

        private Vector3 accumulatedPositionDelta;
        private Quaternion accumulatedRotationDelta = Quaternion.identity;
        private Vector3 accumulatedMotionOrigin;
        private bool hasAccumulatedMotion;
        private readonly List<Vector3> pathVisualizationPoints = new();
        private readonly List<MovingPlatformRailTransitionEvent> railTransitionEvents = new();
        private MovingPlatformRuntimePathVisualizerRegistry runtimePathVisualizerRegistry;
        private float nextLayerDebugLogTime;
        private int lastLoggedLayerSelection = int.MinValue;

        private Transform MotionTransform => motionTargetBindings.Length > 0 && motionTargetBindings[0].Rigidbody != null
            ? motionTargetBindings[0].Rigidbody.transform
            : transform;

        private readonly struct MotionTargetBinding
        {
            public readonly Rigidbody Rigidbody;
            public readonly Vector3 LocalOffsetFromReference;
            public readonly Quaternion LocalRotationFromReference;
            public readonly Vector3 InitialLocalScale;

            public MotionTargetBinding(
                Rigidbody rigidbody,
                Vector3 localOffsetFromReference,
                Quaternion localRotationFromReference,
                Vector3 initialLocalScale)
            {
                Rigidbody = rigidbody;
                LocalOffsetFromReference = localOffsetFromReference;
                LocalRotationFromReference = localRotationFromReference;
                InitialLocalScale = initialLocalScale;
            }
        }

        private void OnValidate()
        {
            NormalizeRailNodeAuthoring();

            if (!autoCollectChildRigidbodies || (motionTargets != null && motionTargets.Length > 0))
                return;

            motionTargets = GetComponentsInChildren<Rigidbody>(true);
        }

        private void Start()
        {
            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();
            if (kernelMB == null || kernelMB.Kernel == null)
            {
                Debug.LogError($"{nameof(MovingPlatformMB)}: SceneKernelMB was not found.", this);
                enabled = false;
                return;
            }

            sceneKernel = kernelMB.Kernel;
            selfEntityMB = GetComponentInParent<EntityMB>();
            BuildMotionTargetBindings();
            if (motionTargetBindings.Length == 0)
            {
                Debug.LogError($"{nameof(MovingPlatformMB)}[{name}]: 有効な motionTargets がありません。子の Rigidbody を設定してください。", this);
                enabled = false;
                return;
            }

            Transform motion = MotionTransform;
            basePose = new MovingPlatformBasePose(motion.position, motion.rotation, motion.localScale);
            BuildLayerRuntimes();
            BuildRailRouting();
            WarnAuthoringSetup();
            signalSubscription = sceneKernel.KernelEvents.Subscribe<KernelSignalRaisedEvent>(OnKernelSignalRaised);
        }

        private void BuildMotionTargetBindings()
        {
            var uniqueTargets = new List<Rigidbody>();
            if (motionTargets != null)
            {
                for (int i = 0; i < motionTargets.Length; i++)
                {
                    Rigidbody target = motionTargets[i];
                    if (target == null || uniqueTargets.Contains(target))
                        continue;

                    uniqueTargets.Add(target);
                }
            }

            if (uniqueTargets.Count == 0 && autoCollectChildRigidbodies)
            {
                Rigidbody[] discovered = GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < discovered.Length; i++)
                {
                    Rigidbody candidate = discovered[i];
                    if (candidate == null || uniqueTargets.Contains(candidate))
                        continue;

                    uniqueTargets.Add(candidate);
                }

                motionTargets = discovered;
            }

            if (uniqueTargets.Count == 0)
            {
                motionTargetBindings = Array.Empty<MotionTargetBinding>();
                return;
            }

            Transform reference = uniqueTargets[0].transform;
            Vector3 referencePosition = reference.position;
            Quaternion referenceRotation = reference.rotation;

            var bindings = new MotionTargetBinding[uniqueTargets.Count];
            for (int i = 0; i < uniqueTargets.Count; i++)
            {
                Rigidbody target = uniqueTargets[i];
                Vector3 localOffset = Quaternion.Inverse(referenceRotation) * (target.position - referencePosition);
                Quaternion localRotation = Quaternion.Inverse(referenceRotation) * target.rotation;
                bindings[i] = new MotionTargetBinding(target, localOffset, localRotation, target.transform.localScale);
            }

            motionTargetBindings = bindings;
        }

        private void WarnAuthoringSetup()
        {
            if (motionTargetBindings == null || motionTargetBindings.Length == 0)
            {
                Debug.LogWarning($"{nameof(MovingPlatformMB)}[{name}]: motionTargets が未設定です。子階層 Rigidbody を最低1つ指定してください。", this);
                return;
            }

            for (int i = 0; i < motionTargetBindings.Length; i++)
            {
                Rigidbody target = motionTargetBindings[i].Rigidbody;
                if (target == null)
                    continue;

                if (!target.transform.IsChildOf(transform))
                {
                    Debug.LogWarning(
                        $"{nameof(MovingPlatformMB)}[{name}]: motionTargets[{i}] '{target.name}' が {name} の子階層外です。相対移動が想定どおりにならない可能性があります。",
                        this);
                }

                if (!target.isKinematic)
                {
                    Debug.LogWarning(
                        $"{nameof(MovingPlatformMB)}[{name}]: motionTargets[{i}] '{target.name}' は Dynamic Rigidbody です。Concave MeshCollider 併用で不安定になりやすいため、基本は Kinematic を推奨します。",
                        this);
                }

                bool hasCollider = target.GetComponent<Collider>() != null || HasChildComponentExcludingRoot<Collider>(target.transform);
                if (!hasCollider)
                {
                    Debug.LogWarning(
                        $"{nameof(MovingPlatformMB)}[{name}]: motionTargets[{i}] '{target.name}' 配下に Collider がありません。接地/運搬判定が必要な場合は Collider を追加してください。",
                        this);
                }
            }

            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                MovingPlatformLayer layer = layers[i];
                if (layer == null || !layer.UseReactiveCondition)
                    continue;
            }
        }

        private static bool HasChildComponentExcludingRoot<T>(Transform root) where T : Component
        {
            if (root == null)
                return false;

            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null || component.transform == root)
                    continue;

                return true;
            }

            return false;
        }

        private void OnDestroy()
        {
            signalSubscription?.Dispose();
            signalSubscription = null;
            DisposeLayerRuntimes();
            DisposeRuntimePathVisualization();
        }

        private void OnDisable()
        {
            HideRuntimePathVisualization();
        }

        private void FixedUpdate()
        {
            accumulatedPositionDelta = Vector3.zero;
            accumulatedRotationDelta = Quaternion.identity;
            hasAccumulatedMotion = false;

            if (runtimes == null || runtimes.Length == 0)
                return;

            TickLayers(Time.fixedDeltaTime);
            SyncRuntimePathVisualization();
        }

        public bool TryCollectEditorGizmoData(
            List<MovingPlatformEditorLayerPathData> layerPaths,
            List<MovingPlatformEditorRailConnectionData> railConnectionsData,
            List<MovingPlatformEditorRailNodeData> railNodesData)
        {
            layerPaths?.Clear();
            railConnectionsData?.Clear();
            railNodesData?.Clear();

            if (!showPathInEditor || layers == null || layers.Length == 0)
                return false;

            MovingPlatformBasePose visualizationBasePose = GetVisualizationBasePose();
            MovingPlatformRailGraph visualizationRailGraph = GetVisualizationRailGraph();
            if (visualizationRailGraph == null)
                return false;

            CollectSharedRailGraphGizmoData(visualizationBasePose, visualizationRailGraph, railConnectionsData, railNodesData);

            for (int i = 0; i < layers.Length; i++)
            {
                if (!TryBuildLayerPreviewPoints(i, visualizationRailGraph, visualizationBasePose, pathVisualizationPoints))
                    continue;

                layerPaths?.Add(new MovingPlatformEditorLayerPathData(
                    ResolveLayerVisualizationColor(layers[i], i),
                    CopyPoints(pathVisualizationPoints),
                    Mathf.Max(0.01f, pathVisualizationPointRadius)));
            }

            return (layerPaths != null && layerPaths.Count > 0) ||
                   (railConnectionsData != null && railConnectionsData.Count > 0) ||
                   (railNodesData != null && railNodesData.Count > 0);
        }

        public bool TryCollectEditorRailNodeHandleData(List<MovingPlatformEditorRailNodeHandleData> handleNodes)
        {
            if (handleNodes == null)
                return false;

            handleNodes.Clear();
            if (railNodes == null || railNodes.Length == 0)
                return false;

            MovingPlatformBasePose visualizationBasePose = GetVisualizationBasePose();
            MovingPlatformRailGraph visualizationRailGraph = GetVisualizationRailGraph();
            if (visualizationRailGraph == null)
                return false;

            for (int i = 0; i < railNodes.Length; i++)
            {
                MovingPlatformRailNode railNode = railNodes[i];
                if (railNode == null)
                    continue;

                if (!visualizationRailGraph.TryGetNodeIndex(railNode.NodePath, out int nodeIndex))
                    continue;

                bool isLiteralPosition = TryResolveLiteralLocalPosition(railNode, out Vector3 localPosition);
                Vector3 worldPosition = visualizationRailGraph.GetWorldPosition(visualizationBasePose, nodeIndex);
                handleNodes.Add(new MovingPlatformEditorRailNodeHandleData(
                    i,
                    railNode.NodePath,
                    localPosition,
                    worldPosition,
                    isLiteralPosition));
            }

            return handleNodes.Count > 0;
        }

        public bool TryGetPassengerMotion(Vector3 passengerWorldPosition, float deltaTime, out MovingPlatformPassengerMotion motion)
        {
            if (!TryGetSupportMotion(passengerWorldPosition, deltaTime, out SupportMotionSnapshot supportMotion))
            {
                motion = new MovingPlatformPassengerMotion(Vector3.zero, Vector3.zero);
                return false;
            }

            motion = new MovingPlatformPassengerMotion(supportMotion.PassengerDelta, supportMotion.PassengerVelocity);
            return true;
        }

        public bool TryGetPrimaryMotionTransform(out Transform motionTransform)
        {
            motionTransform = MotionTransform;
            return motionTransform != null;
        }

        public bool TryGetSupportMotion(Vector3 passengerWorldPosition, float deltaTime, out SupportMotionSnapshot motion)
        {
            if (!hasAccumulatedMotion || deltaTime <= 0.0f)
            {
                motion = SupportMotionSnapshot.None;
                return false;
            }

            motion = SupportMotionUtility.FromDelta(
                MotionTransform,
                null,
                accumulatedMotionOrigin,
                passengerWorldPosition,
                accumulatedPositionDelta,
                accumulatedRotationDelta,
                deltaTime);
            return true;
        }

        private void BuildLayerRuntimes()
        {
            if (layers == null || layers.Length == 0)
            {
                runtimes = Array.Empty<MovingPlatformLayerRuntime>();
                return;
            }

            EntityRef actorEntity = ResolveSelfEntity();
            runtimes = new MovingPlatformLayerRuntime[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                runtimes[i] = new MovingPlatformLayerRuntime(layers[i]);
                runtimes[i].Initialize(sceneKernel, actorEntity);
            }
        }

        private void DisposeLayerRuntimes()
        {
            if (runtimes == null)
                return;

            for (int i = 0; i < runtimes.Length; i++)
                runtimes[i]?.Dispose();
        }

        private void BuildRailRouting()
        {
            railGraph = MovingPlatformRailGraph.Build(
                transform,
                railNodes,
                railConnections,
                sceneKernel?.ReactiveValues,
                BuildReactiveEvalContext());

            if (layers == null || layers.Length == 0)
            {
                railRoutes = Array.Empty<MovingPlatformRailLayerRoute>();
            }
            else
            {
                railRoutes = new MovingPlatformRailLayerRoute[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                    railRoutes[i] = new MovingPlatformRailLayerRoute(layers[i], railGraph);
            }

            railController = railGraph != null ? new MovingPlatformRailController(railGraph, railRoutes) : null;
            useRailRouting = railController != null && railController.IsValid;
            if (useRailRouting)
                railController.Reset(basePose, MotionTransform.position);
        }

        private MovingPlatformBasePose GetVisualizationBasePose()
        {
            if (Application.isPlaying)
                return basePose;

            Transform motion = MotionTransform;
            return new MovingPlatformBasePose(motion.position, motion.rotation, motion.localScale);
        }

        private ReactiveEvalContext BuildReactiveEvalContext()
        {
            return new ReactiveEvalContext(sceneKernel, ResolveSelfEntity(), default);
        }

        private EntityRef ResolveSelfEntity()
        {
            return selfEntityMB != null && selfEntityMB.HasEntity
                ? selfEntityMB.Entity
                : default;
        }

        private void NormalizeRailNodeAuthoring()
        {
            if (railNodes == null || railNodes.Length == 0)
                return;

            var usedNodePaths = new HashSet<string>(StringComparer.Ordinal);
            int nextGeneratedIndex = 1;

            for (int i = 0; i < railNodes.Length; i++)
            {
                MovingPlatformRailNode railNode = railNodes[i];
                if (railNode == null)
                    continue;

                string nodePath = railNode.NodePath;
                bool needsGeneratedPath = railNode.NeedsGeneratedNodePath() ||
                    string.IsNullOrWhiteSpace(nodePath) ||
                    !usedNodePaths.Add(nodePath);

                if (needsGeneratedPath)
                {
                    string generatedPath = GenerateNextNodePath(usedNodePaths, ref nextGeneratedIndex);
                    railNode.SetNodePath(generatedPath);
                    continue;
                }

                nextGeneratedIndex = Mathf.Max(nextGeneratedIndex, ExtractGeneratedNodeIndex(nodePath) + 1);
            }
        }

        private static string GenerateNextNodePath(HashSet<string> usedNodePaths, ref int nextGeneratedIndex)
        {
            string generatedPath;

            do
            {
                generatedPath = $"{MovingPlatformRailNode.DefaultNodeLabelPrefix}{nextGeneratedIndex}";
                nextGeneratedIndex++;
            }
            while (!usedNodePaths.Add(generatedPath));

            return generatedPath;
        }

        private static int ExtractGeneratedNodeIndex(string nodePath)
        {
            if (string.IsNullOrWhiteSpace(nodePath))
                return 0;

            Match match = GeneratedNodeSuffixRegex.Match(nodePath);
            return match.Success && int.TryParse(match.Groups[1].Value, out int index) ? index : 0;
        }

        private bool TryBuildLayerPathPoints(
            int layerIndex,
            MovingPlatformRailGraph visualizationRailGraph,
            in MovingPlatformBasePose visualizationBasePose,
            List<Vector3> points)
        {
            points.Clear();
            if (!TryGetVisualizationRoute(layerIndex, visualizationRailGraph, out MovingPlatformRailLayerRoute route))
                return false;

            for (int i = 0; i < route.NodeCount; i++)
                AddUniquePoint(points, visualizationRailGraph.GetWorldPosition(visualizationBasePose, route.GetNodeIndexAt(i)));

            return points.Count > 0;
        }

        private bool TryBuildLayerPreviewPoints(
            int layerIndex,
            MovingPlatformRailGraph visualizationRailGraph,
            in MovingPlatformBasePose visualizationBasePose,
            List<Vector3> points)
        {
            points.Clear();
            if (layers == null || layerIndex < 0 || layerIndex >= layers.Length || visualizationRailGraph == null)
                return false;

            MovingPlatformLayer layer = layers[layerIndex];
            if (layer == null || !layer.UsesRailRoute || !visualizationRailGraph.TryGetNodeIndex(layer.StartNodePath, out int startNodeIndex))
                return false;

            AddUniquePoint(points, visualizationRailGraph.GetWorldPosition(visualizationBasePose, startNodeIndex));

            // Editor 上では接続の配線がまだ終わっていなくても、authoring した node 列そのものを先に確認できるようにする。
            for (int i = 0; i < layer.RouteSegmentCount; i++)
            {
                if (!layer.TryGetRouteSegment(i, out MovingPlatformRailRouteSegment segment) ||
                    string.IsNullOrWhiteSpace(segment.TargetNodePath) ||
                    !visualizationRailGraph.TryGetNodeIndex(segment.TargetNodePath, out int targetNodeIndex))
                {
                    break;
                }

                AddUniquePoint(points, visualizationRailGraph.GetWorldPosition(visualizationBasePose, targetNodeIndex));
            }

            return points.Count > 1;
        }

        private static Vector3[] CopyPoints(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return Array.Empty<Vector3>();

            Vector3[] copied = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
                copied[i] = points[i];

            return copied;
        }

        private static bool TryResolveLiteralLocalPosition(MovingPlatformRailNode railNode, out Vector3 localPosition)
        {
            localPosition = default;
            if (railNode == null)
                return false;

            ReactiveVector3 reactiveLocalPosition = railNode.LocalPosition;
            if (reactiveLocalPosition.SourceKind != ReactiveVector3SourceKind.Literal)
                return false;

            localPosition = reactiveLocalPosition.Literal;
            return true;
        }

        private static Color GetAutoLayerColor(int layerIndex)
        {
            float hue = Mathf.Repeat(0.12f + layerIndex * 0.19f, 1.0f);
            Color color = Color.HSVToRGB(hue, 0.78f, 1.0f);
            color.a = 0.95f;
            return color;
        }

        private static void AddUniquePoint(List<Vector3> points, Vector3 point)
        {
            if (points == null)
                return;

            if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > 0.000001f)
                points.Add(point);
        }

        private Color ResolveLayerVisualizationColor(MovingPlatformLayer layer, int layerIndex)
        {
            if (layer != null && layer.VisualizationColor.a > 0.001f)
            {
                Color color = layer.VisualizationColor;
                color.a = Mathf.Clamp01(color.a);
                return color;
            }

            return GetAutoLayerColor(layerIndex);
        }

        private Color ComposeLayerColors(int contributionCount, Color accumulatedColor, Color fallbackColor)
        {
            if (contributionCount <= 0)
                return fallbackColor;

            float maxChannel = Mathf.Max(1.0f, accumulatedColor.r, accumulatedColor.g, accumulatedColor.b);
            accumulatedColor.r /= maxChannel;
            accumulatedColor.g /= maxChannel;
            accumulatedColor.b /= maxChannel;
            accumulatedColor.a = Mathf.Clamp01(Mathf.Max(fallbackColor.a, accumulatedColor.a / contributionCount));
            return accumulatedColor;
        }

        private Color GetRailConnectionVisualizationColor(
            MovingPlatformRailGraph visualizationRailGraph,
            int fromNodeIndex,
            int toNodeIndex)
        {
            Color accumulatedColor = Color.clear;
            int contributionCount = 0;

            for (int i = 0; i < layers.Length; i++)
            {
                if (!LayerUsesRailConnection(i, visualizationRailGraph, fromNodeIndex, toNodeIndex))
                    continue;

                accumulatedColor += ResolveLayerVisualizationColor(layers[i], i);
                contributionCount++;
            }

            return ComposeLayerColors(contributionCount, accumulatedColor, new Color(0.24f, 0.28f, 0.34f, 0.55f));
        }

        private Color GetRailNodeVisualizationColor(MovingPlatformRailGraph visualizationRailGraph, int nodeIndex)
        {
            Color accumulatedColor = Color.clear;
            int contributionCount = 0;

            for (int i = 0; i < layers.Length; i++)
            {
                if (!LayerUsesRailNode(i, visualizationRailGraph, nodeIndex))
                    continue;

                accumulatedColor += ResolveLayerVisualizationColor(layers[i], i);
                contributionCount++;
            }

            return ComposeLayerColors(contributionCount, accumulatedColor, new Color(0.75f, 0.82f, 0.92f, 0.75f));
        }

        private bool LayerUsesRailConnection(
            int layerIndex,
            MovingPlatformRailGraph visualizationRailGraph,
            int fromNodeIndex,
            int toNodeIndex)
        {
            if (!TryGetVisualizationRoute(layerIndex, visualizationRailGraph, out MovingPlatformRailLayerRoute route))
                return false;

            for (int i = 0; i < route.NodeCount - 1; i++)
            {
                int routeFromIndex = route.GetNodeIndexAt(i);
                int routeToIndex = route.GetNodeIndexAt(i + 1);
                if ((routeFromIndex == fromNodeIndex && routeToIndex == toNodeIndex) ||
                    (routeFromIndex == toNodeIndex && routeToIndex == fromNodeIndex))
                {
                    return true;
                }
            }

            return false;
        }

        private bool LayerUsesRailNode(int layerIndex, MovingPlatformRailGraph visualizationRailGraph, int nodeIndex)
        {
            if (!TryGetVisualizationRoute(layerIndex, visualizationRailGraph, out MovingPlatformRailLayerRoute route))
                return false;

            return route.ContainsNode(nodeIndex);
        }

        private MovingPlatformRailGraph GetVisualizationRailGraph()
        {
            if (Application.isPlaying)
            {
                if (railGraph != null)
                    return railGraph;

                return sceneKernel != null
                    ? MovingPlatformRailGraph.Build(transform, railNodes, railConnections, sceneKernel.ReactiveValues, BuildReactiveEvalContext())
                    : MovingPlatformRailGraph.Build(transform, railNodes, railConnections);
            }

            return MovingPlatformRailGraph.Build(transform, railNodes, railConnections);
        }

        private bool TryGetVisualizationRoute(
            int layerIndex,
            MovingPlatformRailGraph visualizationRailGraph,
            out MovingPlatformRailLayerRoute route)
        {
            route = null;
            if (layers == null || layerIndex < 0 || layerIndex >= layers.Length || layers[layerIndex] == null || visualizationRailGraph == null)
                return false;

            if (Application.isPlaying && railRoutes != null && layerIndex < railRoutes.Length && railRoutes[layerIndex] != null)
            {
                route = railRoutes[layerIndex];
                return route.IsValid;
            }

            route = new MovingPlatformRailLayerRoute(layers[layerIndex], visualizationRailGraph);
            return route.IsValid;
        }

        private void CollectSharedRailGraphGizmoData(
            in MovingPlatformBasePose visualizationBasePose,
            MovingPlatformRailGraph visualizationRailGraph,
            List<MovingPlatformEditorRailConnectionData> railConnectionsData,
            List<MovingPlatformEditorRailNodeData> railNodesData)
        {
            if (visualizationRailGraph == null)
                return;

            float pointRadius = Mathf.Max(0.01f, pathVisualizationPointRadius) * 0.65f;

            if (railConnections != null)
            {
                for (int i = 0; i < railConnections.Length; i++)
                {
                    MovingPlatformRailConnection connection = railConnections[i];
                    if (connection == null ||
                        !visualizationRailGraph.TryGetNodeIndex(connection.FromNodePath, out int fromNodeIndex) ||
                        !visualizationRailGraph.TryGetNodeIndex(connection.ToNodePath, out int toNodeIndex))
                    {
                        continue;
                    }

                    Vector3 fromWorld = visualizationRailGraph.GetWorldPosition(visualizationBasePose, fromNodeIndex);
                    Vector3 toWorld = visualizationRailGraph.GetWorldPosition(visualizationBasePose, toNodeIndex);

                    railConnectionsData?.Add(new MovingPlatformEditorRailConnectionData(
                        fromWorld,
                        toWorld,
                        GetRailConnectionVisualizationColor(visualizationRailGraph, fromNodeIndex, toNodeIndex)));
                }
            }

            if (railNodes != null)
            {
                for (int i = 0; i < railNodes.Length; i++)
                {
                    MovingPlatformRailNode railNode = railNodes[i];
                    if (railNode == null || !visualizationRailGraph.TryGetNodeIndex(railNode.NodePath, out int nodeIndex))
                        continue;

                    Color fillColor = GetRailNodeVisualizationColor(visualizationRailGraph, nodeIndex);
                    Color wireColor = Color.Lerp(fillColor, Color.white, 0.55f);
                    wireColor.a = 1.0f;

                    railNodesData?.Add(new MovingPlatformEditorRailNodeData(
                        visualizationRailGraph.GetWorldPosition(visualizationBasePose, nodeIndex),
                        fillColor,
                        wireColor,
                        pointRadius));
                }
            }
        }

        private void SyncRuntimePathVisualization()
        {
            if (!Application.isPlaying || !showPathInGame)
            {
                HideRuntimePathVisualization();
                return;
            }

            if (layers == null || layers.Length == 0 || railGraph == null)
            {
                HideRuntimePathVisualization();
                return;
            }

            EnsureRuntimePathRegistry();
            if (runtimePathVisualizerRegistry == null)
                return;

            runtimePathVisualizerRegistry.EnsureLayerCount(layers.Length);
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                Color lineColor = ResolveLayerVisualizationColor(layers[layerIndex], layerIndex);
                lineColor.a = Mathf.Clamp01(runtimePathColor.a);

                RuntimePathEmissionSettings emissionSettings = ResolveRuntimePathEmissionSettings(layers[layerIndex], lineColor);
                if (!TryBuildLayerPathPoints(layerIndex, railGraph, basePose, pathVisualizationPoints))
                {
                    runtimePathVisualizerRegistry.ApplyLayer(
                        layerIndex,
                        runtimePathMaterial,
                        RuntimePathVisualizationData.Hidden(emissionSettings));
                    continue;
                }

                RuntimePathVisualizationData data = new(
                    isVisible: true,
                    isActiveLayer: layerIndex == selectedLayerIndex,
                    lineWidth: runtimePathLineWidth,
                    lineColor: lineColor,
                    points: pathVisualizationPoints,
                    emissionSettings: emissionSettings);

                runtimePathVisualizerRegistry.ApplyLayer(layerIndex, runtimePathMaterial, data);
            }
        }

        private void EnsureRuntimePathRegistry()
        {
            runtimePathVisualizerRegistry ??= new MovingPlatformRuntimePathVisualizerRegistry(transform);
        }

        private RuntimePathEmissionSettings ResolveRuntimePathEmissionSettings(MovingPlatformLayer layer, Color lineColor)
        {
            Color visualizationEmissionColor = ResolveVisualizationEmissionColor(lineColor);

            RuntimePathEmissionSettings defaultSettings = new(
                enableRuntimePathEmission,
                visualizationEmissionColor,
                runtimePathActiveEmissionStrength,
                runtimePathInactiveEmissionStrength,
                runtimePathSyncSimpleBoost,
                runtimePathActiveSimpleBoostIntensity,
                runtimePathInactiveSimpleBoostIntensity,
                dimInactiveRuntimePath,
                runtimePathInactiveAlphaMultiplier);

            if (layer == null || !layer.OverrideRuntimePathEmission)
                return defaultSettings;

            Color overrideEmissionColor = layer.UseVisualizationColorForRuntimePathEmission
                ? visualizationEmissionColor
                : ResolveExplicitEmissionColor(layer.RuntimePathEmissionColor, visualizationEmissionColor);

            return new RuntimePathEmissionSettings(
                enableEmission: true,
                emissionColor: overrideEmissionColor,
                activeEmissionStrength: layer.RuntimePathActiveEmissionStrength,
                inactiveEmissionStrength: layer.RuntimePathInactiveEmissionStrength,
                syncSimpleBoost: layer.SyncRuntimePathSimpleBoost,
                activeSimpleBoostIntensity: layer.RuntimePathActiveSimpleBoostIntensity,
                inactiveSimpleBoostIntensity: layer.RuntimePathInactiveSimpleBoostIntensity,
                dimInactive: layer.DimRuntimePathWhenInactive,
                inactiveAlphaMultiplier: layer.RuntimePathInactiveAlphaMultiplier);
        }

        private Color ResolveVisualizationEmissionColor(Color lineColor)
        {
            Color emissionColor = new(lineColor.r, lineColor.g, lineColor.b, 1.0f);
            if (emissionColor.maxColorComponent > 0.0001f)
                return emissionColor;

            return ResolveExplicitEmissionColor(runtimePathEmissionColor, Color.white);
        }

        private static Color ResolveExplicitEmissionColor(Color configuredColor, Color fallbackColor)
        {
            Color emissionColor = configuredColor.a > 0.001f ? configuredColor : fallbackColor;
            emissionColor.a = 1.0f;
            return emissionColor;
        }

        private void HideRuntimePathVisualization()
        {
            runtimePathVisualizerRegistry?.HideAll();
        }

        private void DisposeRuntimePathVisualization()
        {
            runtimePathVisualizerRegistry?.DisposeAll();
            runtimePathVisualizerRegistry = null;
        }

        private void TickLayers(float deltaTime)
        {
            int nextSelectedLayerIndex = SelectActiveLayerIndex();
            LogLayerDiagnostics(nextSelectedLayerIndex);
            WiringActionContext wiringContext = BuildWiringActionContext();
            if (nextSelectedLayerIndex != selectedLayerIndex)
                ChangeSelectedLayer(nextSelectedLayerIndex);

            if (selectedLayerIndex < 0 || !useRailRouting || railController == null)
                return;

            bool sequenceCompleted = false;
            Transform motion = MotionTransform;
            MovingPlatformPose pose = railController.Tick(
                deltaTime,
                selectedLayerIndex,
                basePose,
                motion.position,
                motion.rotation,
                motion.localScale,
                out sequenceCompleted);
            ApplyPose(pose);
            railController.DrainTransitionEvents(railTransitionEvents);
            ExecuteRailTransitionEvents(wiringContext);

            if (sequenceCompleted && publishLayerSignals)
                sceneKernel.KernelEvents.RaiseSignal(sequenceCompletedSignal);
        }

        private int SelectActiveLayerIndex()
        {
            int bestIndex = -1;
            int bestPriority = int.MinValue;

            for (int i = 0; i < runtimes.Length; i++)
            {
                MovingPlatformLayerRuntime runtime = runtimes[i];
                if (runtime == null || !runtime.RefreshActive())
                    continue;

                if (!IsLayerPlayable(i))
                    continue;

                if (runtime.Priority <= bestPriority)
                    continue;

                bestIndex = i;
                bestPriority = runtime.Priority;
            }

            return bestIndex;
        }

        private bool IsLayerPlayable(int layerIndex)
        {
            if (layerIndex < 0 || layers == null || layerIndex >= layers.Length || layers[layerIndex] == null)
                return false;

            bool routeValid = railRoutes != null &&
                              layerIndex < railRoutes.Length &&
                              railRoutes[layerIndex] != null &&
                              railRoutes[layerIndex].IsValid;

            // Rail-only 実装なので、Layer がルートを使う場合は route 有効化を必須にする。
            if (layers[layerIndex].UsesRailRoute)
                return routeValid;

            if (!useRailRouting)
                return true;

            return routeValid;
        }

        private void LogLayerDiagnostics(int nextSelectedLayerIndex)
        {
            if (!enableLayerDebugLog || runtimes == null || runtimes.Length == 0)
                return;

            float now = Time.unscaledTime;
            bool selectionChanged = nextSelectedLayerIndex != lastLoggedLayerSelection;
            bool intervalElapsed = now >= nextLayerDebugLogTime;
            if (!selectionChanged && !intervalElapsed)
                return;

            lastLoggedLayerSelection = nextSelectedLayerIndex;
            nextLayerDebugLogTime = now + Mathf.Max(0.2f, layerDebugLogInterval);

            string selectedLayerName = nextSelectedLayerIndex >= 0 && layers != null && nextSelectedLayerIndex < layers.Length && layers[nextSelectedLayerIndex] != null
                ? layers[nextSelectedLayerIndex].LayerName
                : "None";

            Debug.Log(
                $"{nameof(MovingPlatformMB)}[{name}] LayerDiagnostics: selected={nextSelectedLayerIndex}({selectedLayerName})",
                this);


            for (int i = 0; i < runtimes.Length; i++)
            {
                MovingPlatformLayerRuntime runtime = runtimes[i];
                if (runtime == null)
                    continue;

                bool playable = IsLayerPlayable(i);
                bool active = runtime.RefreshActive();
                bool routeValid = railRoutes != null && i < railRoutes.Length && railRoutes[i] != null && railRoutes[i].IsValid;
                string routeIssue = ResolveLayerRouteIssue(i);
                float routeDistance = ResolveLayerApproxRouteDistance(i);
                string conditionCurrentValueText = runtime.TryGetConditionCurrentValue(out bool conditionCurrentValue)
                    ? conditionCurrentValue.ToString()
                    : "n/a";

                string reason = active && playable
                    ? "selected-candidate"
                    : BuildInactiveReason(runtime, playable, routeValid);

                Debug.Log(
                    $"{nameof(MovingPlatformMB)}[{name}] Layer[{i}] '{runtime.LayerName}': active={active}, playable={playable}, reason={reason}, routeValid={routeValid}, routeIssue={routeIssue}, routeDistance={routeDistance:0.###}, conditionValue={conditionCurrentValueText}, signalGate={(runtime.UseSignalGate ? runtime.SignalGateActive : true)}",
                    this);
            }
        }

        private float ResolveLayerApproxRouteDistance(int layerIndex)
        {
            if (railGraph == null || railRoutes == null || layerIndex < 0 || layerIndex >= railRoutes.Length)
                return 0f;

            MovingPlatformRailLayerRoute route = railRoutes[layerIndex];
            if (route == null || !route.IsValid || route.NodeCount < 2)
                return 0f;

            float distance = 0f;
            for (int i = 0; i < route.NodeCount - 1; i++)
            {
                int fromNode = route.GetNodeIndexAt(i);
                int toNode = route.GetNodeIndexAt(i + 1);
                distance += railGraph.GetApproxDistance(fromNode, toNode);
            }

            return distance;
        }

        private string ResolveLayerRouteIssue(int layerIndex)
        {
            if (layers == null || layerIndex < 0 || layerIndex >= layers.Length)
                return "layer-out-of-range";

            MovingPlatformLayer layer = layers[layerIndex];
            if (layer == null)
                return "layer-null";

            if (!layer.UsesRailRoute)
                return "uses-rail-route=false";

            if (railGraph == null)
                return "rail-graph-null";

            if (string.IsNullOrWhiteSpace(layer.StartNodePath))
                return "start-node-empty";

            if (!railGraph.TryGetNodeIndex(layer.StartNodePath, out int currentNodeIndex))
                return $"start-node-missing:{layer.StartNodePath}";

            IReadOnlyList<MovingPlatformLayerSegment> segments = layer.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                if (!layer.TryGetSegment(i, out MovingPlatformLayerSegment segment) || segment == null)
                    return $"segment-null:{i}";

                if (segment is MovingPlatformRailRouteSegment moveSegment)
                {
                    if (string.IsNullOrWhiteSpace(moveSegment.TargetNodePath))
                        return $"segment-target-empty:{i}";

                    if (!railGraph.TryGetNodeIndex(moveSegment.TargetNodePath, out int targetNodeIndex))
                        return $"segment-target-missing:{i}:{moveSegment.TargetNodePath}";

                    currentNodeIndex = targetNodeIndex;
                    continue;
                }

                if (segment is MovingPlatformWaitSegment waitSegment &&
                    waitSegment.WaitMode == MovingPlatformWaitMode.Signal &&
                    !waitSegment.Signal.TryResolve(out _))
                {
                    return $"wait-signal-unresolved:{i}";
                }
            }

            return "none";
        }

        private static string BuildInactiveReason(MovingPlatformLayerRuntime runtime, bool playable, bool routeValid)
        {
            if (!routeValid)
                return "inactive(route-invalid)";

            if (!playable)
                return "inactive(not-playable)";

            if (runtime.UseSignalGate && !runtime.SignalGateActive)
                return "inactive(signal-gate-off)";

            if (runtime.UseReactiveCondition && !runtime.ConditionBindingReady)
                return "inactive(condition-binding-missing)";

            if (runtime.UseReactiveCondition && runtime.ConditionBindingReady && !runtime.ConditionReadSucceeded)
                return "inactive(condition-read-failed)";

            if (runtime.UseReactiveCondition && runtime.ConditionReadSucceeded && !runtime.IsConditionSatisfied)
                return "inactive(condition-false)";

            return "inactive(priority-lost)";
        }

        private void ChangeSelectedLayer(int nextSelectedLayerIndex)
        {
            if (publishLayerSignals && selectedLayerIndex >= 0)
                sceneKernel.KernelEvents.RaiseSignal(layerDisabledSignal);

            selectedLayerIndex = nextSelectedLayerIndex;
            if (selectedLayerIndex < 0)
                return;

            if (useRailRouting && railController != null &&
                layers != null &&
                selectedLayerIndex < layers.Length &&
                layers[selectedLayerIndex] != null &&
                layers[selectedLayerIndex].ResetWhenSelected)
            {
                railController.SnapToRouteStart(selectedLayerIndex, basePose);

                if (TryBuildRouteStartPose(selectedLayerIndex, out MovingPlatformPose routeStartPose))
                    ApplyPose(routeStartPose);
            }

            if (publishLayerSignals)
                sceneKernel.KernelEvents.RaiseSignal(layerEnabledSignal);
        }

        private void ExecuteRailTransitionEvents(in WiringActionContext wiringContext)
        {
            if (railTransitionEvents.Count <= 0 || railRoutes == null)
                return;

            for (int i = 0; i < railTransitionEvents.Count; i++)
            {
                MovingPlatformRailTransitionEvent transitionEvent = railTransitionEvents[i];
                if (transitionEvent.LayerIndex < 0 || transitionEvent.LayerIndex >= railRoutes.Length)
                    continue;

                MovingPlatformRailLayerRoute route = railRoutes[transitionEvent.LayerIndex];
                if (route == null)
                    continue;

                if (transitionEvent.Kind == MovingPlatformRailTransitionEventKind.SegmentEnter)
                    route.ExecuteSegmentEnter(transitionEvent.SegmentIndex, wiringContext);
                else
                    route.ExecuteSegmentExit(transitionEvent.SegmentIndex, wiringContext);
            }

            railTransitionEvents.Clear();
        }

        private void ApplyPose(in MovingPlatformPose pose)
        {
            const float DynamicSnapDistanceSqr = 1.5f * 1.5f;
            const float DynamicMaxAngularSpeed = 10.0f;
            const float DeltaFallbackThresholdSqr = 0.000001f;
            const float RotationFallbackAngleThreshold = 0.01f;

            if (motionTargetBindings == null || motionTargetBindings.Length == 0)
                return;

            Transform referenceTransform = MotionTransform;
            Vector3 previousPosition = referenceTransform.position;
            Quaternion previousRotation = referenceTransform.rotation;

            Vector3 baseScale = SanitizeScale(basePose.LocalScale);
            Vector3 poseScale = SanitizeScale(pose.LocalScale);
            Vector3 scaleRatio = new Vector3(
                poseScale.x / baseScale.x,
                poseScale.y / baseScale.y,
                poseScale.z / baseScale.z);

            float deltaTime = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            Vector3 referenceCommandedPosition = previousPosition;
            Quaternion referenceCommandedRotation = previousRotation;
            bool hasReferenceCommand = false;

            for (int i = 0; i < motionTargetBindings.Length; i++)
            {
                MotionTargetBinding binding = motionTargetBindings[i];
                Rigidbody target = binding.Rigidbody;
                if (target == null)
                    continue;

                Transform targetTransform = target.transform;
                Vector3 targetPosition = pose.Position + pose.Rotation * Vector3.Scale(binding.LocalOffsetFromReference, scaleRatio);
                Quaternion targetRotation = pose.Rotation * binding.LocalRotationFromReference;
                Vector3 targetLocalScale = Vector3.Scale(binding.InitialLocalScale, scaleRatio);

                if (i == 0)
                {
                    referenceCommandedPosition = targetPosition;
                    referenceCommandedRotation = targetRotation;
                    hasReferenceCommand = true;
                }

                if (target.isKinematic)
                {
                    target.MovePosition(targetPosition);
                    target.MoveRotation(targetRotation);
                }
                else
                {
                    // Dynamic Rigidbody を毎フレーム直接テレポートすると接触解決で押しのけが発生しやすいため、
                    // 基本は速度追従にし、大きな乖離時だけスナップで復帰させます。
                    Vector3 currentPosition = target.position;
                    Quaternion currentRotation = target.rotation;
                    Vector3 positionError = targetPosition - currentPosition;
                    Quaternion rotationError = targetRotation * Quaternion.Inverse(currentRotation);

                    if (positionError.sqrMagnitude > DynamicSnapDistanceSqr)
                    {
                        target.position = targetPosition;
                        target.rotation = targetRotation;
                        target.linearVelocity = Vector3.zero;
                        target.angularVelocity = Vector3.zero;
                    }
                    else
                    {
                        target.linearVelocity = positionError / deltaTime;
                        Vector3 targetAngularVelocity = SupportMotionUtility.CalculateAngularVelocity(rotationError, deltaTime);
                        target.angularVelocity = Vector3.ClampMagnitude(targetAngularVelocity, DynamicMaxAngularSpeed);
                    }
                }

                targetTransform.localScale = targetLocalScale;
            }

            Vector3 observedPositionDelta = referenceTransform.position - previousPosition;
            Quaternion observedRotationDelta = referenceTransform.rotation * Quaternion.Inverse(previousRotation);

            // 物理同期タイミングで observed delta が 0 になるフレームだけ、
            // この tick で指示した参照ターゲット差分へフォールバックして運搬量を維持します。
            Vector3 positionDelta = observedPositionDelta;
            Quaternion rotationDelta = observedRotationDelta;

            if (hasReferenceCommand)
            {
                Vector3 commandedPositionDelta = referenceCommandedPosition - previousPosition;
                Quaternion commandedRotationDelta = referenceCommandedRotation * Quaternion.Inverse(previousRotation);

                if (positionDelta.sqrMagnitude <= DeltaFallbackThresholdSqr &&
                    commandedPositionDelta.sqrMagnitude > DeltaFallbackThresholdSqr)
                {
                    positionDelta = commandedPositionDelta;
                }

                if (Quaternion.Angle(rotationDelta, Quaternion.identity) <= RotationFallbackAngleThreshold &&
                    Quaternion.Angle(commandedRotationDelta, Quaternion.identity) > RotationFallbackAngleThreshold)
                {
                    rotationDelta = commandedRotationDelta;
                }
            }

            if (!hasAccumulatedMotion)
                accumulatedMotionOrigin = previousPosition;

            accumulatedPositionDelta += positionDelta;
            accumulatedRotationDelta = rotationDelta * accumulatedRotationDelta;

            if (positionDelta.sqrMagnitude > 0.0f || Quaternion.Angle(rotationDelta, Quaternion.identity) > 0.001f)
                hasAccumulatedMotion = true;
        }

        private bool TryBuildRouteStartPose(int layerIndex, out MovingPlatformPose pose)
        {
            pose = default;
            if (railGraph == null || railController == null || !railController.TryGetRoute(layerIndex, out MovingPlatformRailLayerRoute route) ||
                route == null || !route.IsValid || route.NodeCount <= 0)
            {
                return false;
            }

            int startNodeIndex = route.GetNodeIndexAt(0);
            Vector3 startWorldPosition = railGraph.GetWorldPosition(basePose, startNodeIndex);
            Transform motion = MotionTransform;
            pose = new MovingPlatformPose(startWorldPosition, motion.rotation, motion.localScale);
            return true;
        }

        private static Vector3 SanitizeScale(Vector3 value)
        {
            const float MinScale = 0.0001f;
            return new Vector3(
                Mathf.Abs(value.x) < MinScale ? 1.0f : value.x,
                Mathf.Abs(value.y) < MinScale ? 1.0f : value.y,
                Mathf.Abs(value.z) < MinScale ? 1.0f : value.z);
        }

        private void OnKernelSignalRaised(KernelSignalRaisedEvent signalEvent)
        {
            railController?.NotifySignal(signalEvent.Signal);

            if (runtimes == null)
                return;

            for (int i = 0; i < runtimes.Length; i++)
                runtimes[i]?.HandleSignal(signalEvent.Signal);
        }

        private WiringActionContext BuildWiringActionContext()
        {
            EntityRef selfEntity = default;
            EntityTagId selfTag = default;

            if (selfEntityMB != null)
            {
                if (selfEntityMB.HasEntity)
                    selfEntity = selfEntityMB.Entity;

                selfTag = selfEntityMB.Tag;
            }

            return new WiringActionContext(
                sceneKernel,
                gameObject,
                transform,
                selfEntity,
                selfTag,
                null,
                null,
                default,
                default);
        }

        private sealed class MovingPlatformLayerRuntime : IDisposable
        {
            private readonly MovingPlatformLayer layer;
            private ReactiveWatchedBoolBinding activeConditionBinding;
            private bool signalGateActive;
            private bool conditionReadSucceeded;
            private bool isConditionSatisfied = true;

            public int Priority => layer != null ? layer.Priority : int.MinValue;
            public string LayerName => layer != null ? layer.LayerName : "Layer";
            public bool ActiveOnStart => layer != null && layer.ActiveOnStart;
            public bool UseSignalGate => layer != null && layer.UseSignalGate;
            public bool SignalGateActive => signalGateActive;
            public bool UseReactiveCondition => layer != null && layer.UseReactiveCondition;
            public bool ConditionBindingReady => activeConditionBinding != null;
            public bool ConditionReadSucceeded => conditionReadSucceeded;
            public bool IsConditionSatisfied => !UseReactiveCondition || isConditionSatisfied;

            public MovingPlatformLayerRuntime(MovingPlatformLayer layer)
            {
                this.layer = layer;
            }

            public void Initialize(SceneKernel sceneKernel, EntityRef actorEntity)
            {
                signalGateActive = layer != null && layer.ActiveOnStart;
                conditionReadSucceeded = !UseReactiveCondition;
                isConditionSatisfied = true;

                if (layer == null || !layer.UseReactiveCondition || sceneKernel?.ReactiveValues == null)
                    return;

                activeConditionBinding = new ReactiveWatchedBoolBinding(
                    sceneKernel.ReactiveValues,
                    new ReactiveEvalContext(sceneKernel, actorEntity, default),
                    layer.ActiveCondition);
            }

            public bool RefreshActive()
            {
                if (layer == null)
                    return false;

                if (layer.UseSignalGate && !signalGateActive)
                    return false;

                if (layer.UseReactiveCondition)
                {
                    if (activeConditionBinding == null)
                    {
                        conditionReadSucceeded = false;
                        isConditionSatisfied = false;
                        return false;
                    }

                    ReactiveResult<bool> result = activeConditionBinding.Read();
                    conditionReadSucceeded = result.Success;
                    if (!result.Success)
                    {
                        isConditionSatisfied = false;
                        return false;
                    }

                    isConditionSatisfied = result.Value;
                    if (!isConditionSatisfied)
                        return false;
                }
                else
                {
                    conditionReadSucceeded = true;
                    isConditionSatisfied = true;
                }

                return true;
            }

            public bool TryGetConditionCurrentValue(out bool value)
            {
                if (!UseReactiveCondition)
                {
                    value = true;
                    return true;
                }

                if (!conditionReadSucceeded)
                {
                    value = default;
                    return false;
                }

                value = isConditionSatisfied;
                return true;
            }

            public void Dispose()
            {
                activeConditionBinding?.Dispose();
                activeConditionBinding = null;
            }

            public void HandleSignal(SignalId signalId)
            {
                if (layer == null || !layer.UseSignalGate)
                    return;

                if (layer.MatchesActivateSignal(signalId))
                    signalGateActive = true;

                if (layer.MatchesDeactivateSignal(signalId))
                    signalGateActive = false;
            }
        }
    }
}
