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
        private MovingPlatformLayerRuntime[] runtimes;
        private MovingPlatformRailLayerRoute[] railRoutes;
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
        private bool hasAccumulatedMotion;
        private readonly List<Vector3> pathVisualizationPoints = new();
        private readonly List<MovingPlatformRailTransitionEvent> railTransitionEvents = new();
        private LineRenderer runtimePathRenderer;
        private Material ownedRuntimePathMaterial;
        private int visualizedRuntimeLayerIndex = -2;
        private int visualizedRuntimePointCount = -1;
        private float visualizedRuntimeLineWidth = -1.0f;
        private Color visualizedRuntimeColor = default;

        private void OnValidate()
        {
            NormalizeRailNodeAuthoring();
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
            basePose = new MovingPlatformBasePose(transform.position, transform.rotation, transform.localScale);
            BuildLayerRuntimes();
            BuildRailRouting();
            signalSubscription = sceneKernel.KernelEvents.Subscribe<KernelSignalRaisedEvent>(OnKernelSignalRaised);
        }

        private void OnDestroy()
        {
            signalSubscription?.Dispose();
            signalSubscription = null;
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

        private void OnDrawGizmos()
        {
            if (!showPathInEditor || layers == null || layers.Length == 0)
                return;

            MovingPlatformBasePose visualizationBasePose = GetVisualizationBasePose();
            MovingPlatformRailGraph visualizationRailGraph = GetVisualizationRailGraph();
            if (visualizationRailGraph == null)
                return;

            DrawSharedRailGraphGizmos(visualizationBasePose, visualizationRailGraph);

            for (int i = 0; i < layers.Length; i++)
            {
                if (!TryBuildLayerPreviewPoints(i, visualizationRailGraph, visualizationBasePose, pathVisualizationPoints))
                    continue;

                DrawLayerPathGizmos(
                    pathVisualizationPoints,
                    ResolveLayerVisualizationColor(layers[i], i));
            }
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

        public bool TryGetSupportMotion(Vector3 passengerWorldPosition, float deltaTime, out SupportMotionSnapshot motion)
        {
            if (!hasAccumulatedMotion || deltaTime <= 0.0f)
            {
                motion = SupportMotionSnapshot.None;
                return false;
            }

            motion = SupportMotionUtility.FromDelta(
                transform,
                null,
                transform.position,
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

            runtimes = new MovingPlatformLayerRuntime[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                runtimes[i] = new MovingPlatformLayerRuntime(layers[i]);
                runtimes[i].Initialize(sceneKernel);
            }
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
                railController.Reset(basePose, transform.position);
        }

        private MovingPlatformBasePose GetVisualizationBasePose()
        {
            if (Application.isPlaying)
                return basePose;

            return new MovingPlatformBasePose(transform.position, transform.rotation, transform.localScale);
        }

        private ReactiveEvalContext BuildReactiveEvalContext()
        {
            EntityRef selfEntity = default;
            if (selfEntityMB != null && selfEntityMB.HasEntity)
                selfEntity = selfEntityMB.Entity;

            return new ReactiveEvalContext(sceneKernel, selfEntity, default);
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

                string previousRawPath = railNode.RawNodePath;
                string previousRawName = railNode.RawNodeName;
                string nodePath = railNode.NodePath;
                bool needsGeneratedPath = railNode.NeedsGeneratedNodePath() ||
                    string.IsNullOrWhiteSpace(nodePath) ||
                    !usedNodePaths.Add(nodePath);

                if (needsGeneratedPath)
                {
                    string generatedPath = GenerateNextNodePath(usedNodePaths, ref nextGeneratedIndex);
                    railNode.SetNodePath(generatedPath);

                    if (railNode.NeedsGeneratedNodeName() || string.Equals(previousRawName, previousRawPath, StringComparison.Ordinal))
                        railNode.SetNodeName(generatedPath);

                    continue;
                }

                nextGeneratedIndex = Mathf.Max(nextGeneratedIndex, ExtractGeneratedNodeIndex(nodePath) + 1);

                if (railNode.NeedsGeneratedNodeName())
                    railNode.SetNodeName(nodePath);
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

        private void DrawLayerPathGizmos(
            List<Vector3> points,
            Color color)
        {
            if (points.Count <= 0)
                return;

            Gizmos.color = color;
            for (int i = 0; i < points.Count - 1; i++)
                Gizmos.DrawLine(points[i], points[i + 1]);

            float pointRadius = Mathf.Max(0.01f, pathVisualizationPointRadius);
            Gizmos.DrawSphere(points[0], pointRadius);
            DrawLayerAnchorGizmos(points, pointRadius, color);
            Gizmos.DrawSphere(points[points.Count - 1], pointRadius * 0.75f);
        }

        private void DrawLayerAnchorGizmos(
            List<Vector3> points,
            float pointRadius,
            Color color)
        {
            if (points == null || points.Count <= 0)
                return;

            Gizmos.color = Color.Lerp(color, Color.white, 0.35f);
            for (int i = 0; i < points.Count; i++)
            {
                Gizmos.DrawWireSphere(points[i], pointRadius * 0.55f);
            }
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

        private static void DrawCompositeRailNodeGizmo(Vector3 worldPosition, float pointRadius, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(worldPosition, pointRadius);

            Color wireColor = Color.Lerp(color, Color.white, 0.55f);
            wireColor.a = 1.0f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(worldPosition, pointRadius * 1.15f);
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

        private void DrawSharedRailGraphGizmos(in MovingPlatformBasePose visualizationBasePose, MovingPlatformRailGraph visualizationRailGraph)
        {
            if (visualizationRailGraph == null)
                return;

            Color previousColor = Gizmos.color;
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

                    Gizmos.color = new Color(0.16f, 0.19f, 0.24f, 0.55f);
                    Gizmos.DrawLine(fromWorld, toWorld);

                    Gizmos.color = GetRailConnectionVisualizationColor(visualizationRailGraph, fromNodeIndex, toNodeIndex);
                    Gizmos.DrawLine(fromWorld, toWorld);
                }
            }

            if (railNodes != null)
            {
                for (int i = 0; i < railNodes.Length; i++)
                {
                    MovingPlatformRailNode railNode = railNodes[i];
                    if (railNode == null || !visualizationRailGraph.TryGetNodeIndex(railNode.NodePath, out int nodeIndex))
                        continue;

                    DrawCompositeRailNodeGizmo(
                        visualizationRailGraph.GetWorldPosition(visualizationBasePose, nodeIndex),
                        pointRadius,
                        GetRailNodeVisualizationColor(visualizationRailGraph, nodeIndex));
                }
            }

            Gizmos.color = previousColor;
        }

        private void SyncRuntimePathVisualization()
        {
            if (!Application.isPlaying || !showPathInGame)
            {
                HideRuntimePathVisualization();
                return;
            }

            if (selectedLayerIndex < 0 || selectedLayerIndex >= layers.Length || railGraph == null)
            {
                HideRuntimePathVisualization();
                return;
            }

            int runtimeLayerIndex = selectedLayerIndex;
            if (!TryBuildLayerPathPoints(runtimeLayerIndex, railGraph, basePose, pathVisualizationPoints))
            {
                HideRuntimePathVisualization();
                return;
            }

            EnsureRuntimePathRenderer();
            if (runtimePathRenderer == null)
                return;

            runtimePathRenderer.enabled = true;
            Color lineColor = ResolveLayerVisualizationColor(layers[runtimeLayerIndex], runtimeLayerIndex);
            lineColor.a = Mathf.Clamp01(runtimePathColor.a);

            bool needsRefresh = visualizedRuntimeLayerIndex != runtimeLayerIndex ||
                                visualizedRuntimePointCount != pathVisualizationPoints.Count ||
                                !Mathf.Approximately(visualizedRuntimeLineWidth, runtimePathLineWidth) ||
                                visualizedRuntimeColor != lineColor ||
                                runtimePathRenderer.positionCount != pathVisualizationPoints.Count;

            runtimePathRenderer.material = runtimePathMaterial;

            runtimePathRenderer.startWidth = runtimePathLineWidth;
            runtimePathRenderer.endWidth = runtimePathLineWidth;
            runtimePathRenderer.startColor = lineColor;
            runtimePathRenderer.endColor = lineColor;

            if (needsRefresh)
            {
                runtimePathRenderer.positionCount = pathVisualizationPoints.Count;
                for (int i = 0; i < pathVisualizationPoints.Count; i++)
                    runtimePathRenderer.SetPosition(i, pathVisualizationPoints[i]);

                visualizedRuntimeLayerIndex = runtimeLayerIndex;
                visualizedRuntimePointCount = pathVisualizationPoints.Count;
                visualizedRuntimeLineWidth = runtimePathLineWidth;
                visualizedRuntimeColor = lineColor;
            }
        }

        private void EnsureRuntimePathRenderer()
        {
            if (runtimePathRenderer == null)
            {
                GameObject rendererObject = new GameObject("MovingPlatform Path Preview");
                rendererObject.transform.SetParent(transform, false);
                runtimePathRenderer = rendererObject.AddComponent<LineRenderer>();
                runtimePathRenderer.useWorldSpace = true;
                runtimePathRenderer.numCapVertices = 4;
                runtimePathRenderer.numCornerVertices = 4;
                runtimePathRenderer.loop = false;
            }

            if (runtimePathRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Unlit");

                if (shader != null)
                {
                    ownedRuntimePathMaterial = new Material(shader);
                    runtimePathRenderer.sharedMaterial = ownedRuntimePathMaterial;
                }
            }
        }

        private void HideRuntimePathVisualization()
        {
            if (runtimePathRenderer == null)
                return;

            runtimePathRenderer.enabled = false;
            runtimePathRenderer.positionCount = 0;
            visualizedRuntimeLayerIndex = -2;
            visualizedRuntimePointCount = -1;
        }

        private void DisposeRuntimePathVisualization()
        {
            if (runtimePathRenderer != null)
            {
                GameObject rendererObject = runtimePathRenderer.gameObject;
                if (Application.isPlaying)
                    Destroy(rendererObject);
                else
                    DestroyImmediate(rendererObject);

                runtimePathRenderer = null;
            }

            if (ownedRuntimePathMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(ownedRuntimePathMaterial);
                else
                    DestroyImmediate(ownedRuntimePathMaterial);

                ownedRuntimePathMaterial = null;
            }
        }

        private void TickLayers(float deltaTime)
        {
            int nextSelectedLayerIndex = SelectActiveLayerIndex();
            WiringActionContext wiringContext = BuildWiringActionContext();
            if (nextSelectedLayerIndex != selectedLayerIndex)
                ChangeSelectedLayer(nextSelectedLayerIndex);

            if (selectedLayerIndex < 0 || !useRailRouting || railController == null)
                return;

            bool sequenceCompleted = false;
            MovingPlatformPose pose = railController.Tick(deltaTime, selectedLayerIndex, basePose, transform.position, out sequenceCompleted);
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

                if (runtime.Priority <= bestPriority)
                    continue;

                bestIndex = i;
                bestPriority = runtime.Priority;
            }

            return bestIndex;
        }

        private void ChangeSelectedLayer(int nextSelectedLayerIndex)
        {
            if (publishLayerSignals && selectedLayerIndex >= 0)
                sceneKernel.KernelEvents.RaiseSignal(layerDisabledSignal);

            selectedLayerIndex = nextSelectedLayerIndex;
            if (selectedLayerIndex < 0)
                return;

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
            Vector3 previousPosition = transform.position;
            Quaternion previousRotation = transform.rotation;

            transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            transform.localScale = pose.LocalScale;

            Vector3 positionDelta = transform.position - previousPosition;
            Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(previousRotation);
            accumulatedPositionDelta += positionDelta;
            accumulatedRotationDelta = rotationDelta * accumulatedRotationDelta;

            if (positionDelta.sqrMagnitude > 0.0f || Quaternion.Angle(rotationDelta, Quaternion.identity) > 0.001f)
                hasAccumulatedMotion = true;
        }

        private void OnKernelSignalRaised(KernelSignalRaisedEvent signalEvent)
        {
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

        private sealed class MovingPlatformLayerRuntime
        {
            private readonly MovingPlatformLayer layer;
            private ValueWatchHandle<bool> activeValueHandle;
            private bool signalGateActive;

            public int Priority => layer != null ? layer.Priority : int.MinValue;

            public MovingPlatformLayerRuntime(MovingPlatformLayer layer)
            {
                this.layer = layer;
            }

            public void Initialize(SceneKernel sceneKernel)
            {
                signalGateActive = layer != null && layer.ActiveOnStart;
                if (layer == null || !layer.UseKernelBoolCondition || sceneKernel?.KernelValueStore == null)
                    return;

                if (layer.KernelActiveKey.TryResolve(out ValueKey<bool> key))
                    activeValueHandle = sceneKernel.KernelValueStore.GetHandle(key);
            }

            public bool RefreshActive()
            {
                if (layer == null)
                    return false;

                if (layer.UseSignalGate && !signalGateActive)
                    return false;

                if (layer.UseKernelBoolCondition)
                {
                    if (activeValueHandle == null)
                        return false;

                    if (activeValueHandle.CurrentValue != layer.ActiveWhenValue)
                        return false;
                }

                return true;
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
