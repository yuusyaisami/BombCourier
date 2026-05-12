using System.Collections.Generic;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class MovingPlatformMB : MonoBehaviour, IMovingPlatformMotionSource
    {
        [Header("Layers")]
        [LabelText("Layers")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, ListElementLabelName = "LayerName")]
        [Tooltip("足場の挙動レイヤー一覧です。優先度が最も高い有効レイヤーが再生されます。")]
        [SerializeField]
        private MovingPlatformLayer[] layers = System.Array.Empty<MovingPlatformLayer>();

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
        [Tooltip("1 Step あたりの経路サンプリング数です。増やすほど曲線表示が滑らかになります。")]
        [SerializeField, Min(2)] private int pathVisualizationSamplesPerStep = 16;
        [Tooltip("経路の補助ポイント半径です。Editor 上の終点やゲーム中ラインの見やすさに使います。")]
        [SerializeField, Min(0.01f)] private float pathVisualizationPointRadius = 0.08f;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("ゲーム再生中に表示するラインの太さです。")]
        [SerializeField, Min(0.005f)] private float runtimePathLineWidth = 0.05f;
        [ShowIf(nameof(showPathInGame))]
        [Tooltip("ゲーム再生中に表示する経路ラインの色です。")]
        [SerializeField] private Color runtimePathColor = new(1.0f, 0.65f, 0.15f, 0.9f);

        private MovingPlatformLayerRuntime[] runtimes;
        private MovingPlatformBasePose basePose;
        private SceneKernel sceneKernel;
        private EntityMB selfEntityMB;
        private EventSubscription signalSubscription;
        private int selectedLayerIndex = -1;

        private Vector3 accumulatedPositionDelta;
        private Quaternion accumulatedRotationDelta = Quaternion.identity;
        private bool hasAccumulatedMotion;
        private readonly List<Vector3> pathVisualizationPoints = new();
        private readonly List<Vector3> pathVisualizationAnchors = new();
        private LineRenderer runtimePathRenderer;
        private Material ownedRuntimePathMaterial;
        private int visualizedRuntimeLayerIndex = -2;
        private int visualizedRuntimeSampleCount = -1;
        private float visualizedRuntimeLineWidth = -1.0f;
        private Color visualizedRuntimeColor = default;

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

            for (int i = 0; i < layers.Length; i++)
            {
                MovingPlatformLayer layer = layers[i];

                if (!TryBuildLayerPathPoints(layer, visualizationBasePose, pathVisualizationPoints))
                    continue;

                DrawLayerPathGizmos(layer, visualizationBasePose, pathVisualizationPoints, GetEditorLayerColor(i));
            }
        }

        public bool TryGetPassengerMotion(Vector3 passengerWorldPosition, float deltaTime, out MovingPlatformPassengerMotion motion)
        {
            if (!hasAccumulatedMotion || deltaTime <= 0.0f)
            {
                motion = new MovingPlatformPassengerMotion(Vector3.zero, Vector3.zero);
                return false;
            }

            Vector3 offset = passengerWorldPosition - transform.position;
            Vector3 rotationDelta = accumulatedRotationDelta * offset - offset;
            Vector3 delta = accumulatedPositionDelta + rotationDelta;
            motion = new MovingPlatformPassengerMotion(delta, delta / deltaTime);
            return true;
        }

        private void BuildLayerRuntimes()
        {
            if (layers == null || layers.Length == 0)
            {
                runtimes = new MovingPlatformLayerRuntime[0];
                return;
            }

            runtimes = new MovingPlatformLayerRuntime[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                runtimes[i] = new MovingPlatformLayerRuntime(layers[i]);
                runtimes[i].Initialize(sceneKernel);
            }
        }

        private MovingPlatformBasePose GetVisualizationBasePose()
        {
            if (Application.isPlaying)
                return basePose;

            return new MovingPlatformBasePose(transform.position, transform.rotation, transform.localScale);
        }

        private bool TryBuildLayerPathPoints(
            MovingPlatformLayer layer,
            in MovingPlatformBasePose visualizationBasePose,
            List<Vector3> points)
        {
            points.Clear();

            if (layer == null || layer.StepCount <= 0)
                return false;

            int samplesPerStep = Mathf.Max(2, pathVisualizationSamplesPerStep);
            points.Add(visualizationBasePose.Position);

            for (int stepIndex = 0; stepIndex < layer.StepCount; stepIndex++)
            {
                for (int sampleIndex = 1; sampleIndex <= samplesPerStep; sampleIndex++)
                {
                    float normalizedTime = sampleIndex / (float)samplesPerStep;
                    Vector3 point = layer.EvaluatePose(visualizationBasePose, stepIndex, normalizedTime).Position;

                    if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > 0.000001f)
                        points.Add(point);
                }
            }

            return points.Count > 0;
        }

        private void DrawLayerPathGizmos(
            MovingPlatformLayer layer,
            in MovingPlatformBasePose visualizationBasePose,
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
            DrawLayerAnchorGizmos(layer, visualizationBasePose, pointRadius, color);

            for (int stepIndex = 0; stepIndex < layer.StepCount; stepIndex++)
            {
                Vector3 stepEnd = layer.EvaluatePose(visualizationBasePose, stepIndex, 1.0f).Position;
                Gizmos.DrawWireSphere(stepEnd, pointRadius * 0.9f);
            }

            Gizmos.DrawSphere(points[points.Count - 1], pointRadius * 0.75f);
        }

        private void DrawLayerAnchorGizmos(
            MovingPlatformLayer layer,
            in MovingPlatformBasePose visualizationBasePose,
            float pointRadius,
            Color color)
        {
            if (layer == null || layer.StepCount <= 0)
                return;

            MovingPlatformPose stepStartPose = visualizationBasePose.ToPose();
            Gizmos.color = Color.Lerp(color, Color.white, 0.35f);

            for (int stepIndex = 0; stepIndex < layer.StepCount; stepIndex++)
            {
                if (!layer.TryGetStep(stepIndex, out MovingPlatformMotionStep step))
                    continue;

                pathVisualizationAnchors.Clear();
                step.AppendVisualizationAnchors(visualizationBasePose, stepStartPose, pathVisualizationAnchors);

                for (int i = 0; i < pathVisualizationAnchors.Count; i++)
                    Gizmos.DrawWireSphere(pathVisualizationAnchors[i], pointRadius * 0.55f);

                stepStartPose = step.EvaluatePose(visualizationBasePose, stepStartPose, 1.0f);
            }
        }

        private static Color GetEditorLayerColor(int layerIndex)
        {
            float hue = Mathf.Repeat(0.12f + layerIndex * 0.19f, 1.0f);
            Color color = Color.HSVToRGB(hue, 0.78f, 1.0f);
            color.a = 0.95f;
            return color;
        }

        private void SyncRuntimePathVisualization()
        {
            if (!Application.isPlaying || !showPathInGame)
            {
                HideRuntimePathVisualization();
                return;
            }

            if (selectedLayerIndex < 0 || selectedLayerIndex >= layers.Length)
            {
                HideRuntimePathVisualization();
                return;
            }

            int runtimeLayerIndex = selectedLayerIndex;
            MovingPlatformLayer layer = layers[runtimeLayerIndex];

            if (!TryBuildLayerPathPoints(layer, basePose, pathVisualizationPoints))
            {
                HideRuntimePathVisualization();
                return;
            }

            EnsureRuntimePathRenderer();

            if (runtimePathRenderer == null)
                return;

            runtimePathRenderer.enabled = true;
            Color lineColor = Color.Lerp(runtimePathColor, GetEditorLayerColor(runtimeLayerIndex), 0.35f);
            lineColor.a = runtimePathColor.a;

            bool needsRefresh = visualizedRuntimeLayerIndex != runtimeLayerIndex ||
                                visualizedRuntimeSampleCount != pathVisualizationSamplesPerStep ||
                                !Mathf.Approximately(visualizedRuntimeLineWidth, runtimePathLineWidth) ||
                                visualizedRuntimeColor != lineColor ||
                                runtimePathRenderer.positionCount != pathVisualizationPoints.Count;

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
                visualizedRuntimeSampleCount = pathVisualizationSamplesPerStep;
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
                ChangeSelectedLayer(nextSelectedLayerIndex, wiringContext);

            if (selectedLayerIndex < 0)
                return;

            MovingPlatformLayerRuntime runtime = runtimes[selectedLayerIndex];
            bool completed = runtime.Tick(deltaTime, wiringContext);
            ApplyPose(runtime.EvaluatePose(basePose));

            if (completed && publishLayerSignals)
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

        private void ChangeSelectedLayer(int nextSelectedLayerIndex, in WiringActionContext wiringContext)
        {
            if (publishLayerSignals && selectedLayerIndex >= 0)
                sceneKernel.KernelEvents.RaiseSignal(layerDisabledSignal);

            selectedLayerIndex = nextSelectedLayerIndex;

            if (selectedLayerIndex < 0)
                return;

            MovingPlatformLayerRuntime runtime = runtimes[selectedLayerIndex];

            if (runtime.ResetWhenSelected)
                runtime.ResetMotion(wiringContext);

            if (publishLayerSignals)
                sceneKernel.KernelEvents.RaiseSignal(layerEnabledSignal);
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
            {
                runtimes[i]?.HandleSignal(signalEvent.Signal);
            }
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
            private int currentStepIndex;
            private float stepElapsedTime;
            private int direction = 1;
            private bool onceCompleted;
            private bool activeStepEntered;

            public int Priority => layer != null ? layer.Priority : int.MinValue;
            public bool ResetWhenSelected => layer != null && layer.ResetWhenSelected;

            public MovingPlatformLayerRuntime(MovingPlatformLayer layer)
            {
                this.layer = layer;
            }

            public void Initialize(SceneKernel sceneKernel)
            {
                signalGateActive = layer != null && (!layer.UseSignalGate || layer.ActiveOnStart);

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

            public bool Tick(float deltaTime, in WiringActionContext wiringContext)
            {
                if (layer == null || layer.StepCount <= 0 || deltaTime <= 0.0f || onceCompleted)
                    return false;

                currentStepIndex = Mathf.Clamp(currentStepIndex, 0, layer.StepCount - 1);
                EnsureStepEntered(wiringContext);

                bool sequenceCompleted = false;
                float remainingTime = deltaTime;
                int guard = 0;

                while (remainingTime > 0.0f && guard++ < 128 && !onceCompleted)
                {
                    float duration = layer.GetStepDuration(currentStepIndex);

                    if (direction >= 0)
                    {
                        float timeToStepEnd = Mathf.Max(0.0f, duration - stepElapsedTime);

                        if (remainingTime < timeToStepEnd)
                        {
                            stepElapsedTime += remainingTime;
                            remainingTime = 0.0f;
                            continue;
                        }

                        stepElapsedTime = duration;
                        remainingTime -= timeToStepEnd;
                        sequenceCompleted |= AdvanceForward(wiringContext);
                    }
                    else
                    {
                        float timeToStepStart = Mathf.Max(0.0f, stepElapsedTime);

                        if (remainingTime < timeToStepStart)
                        {
                            stepElapsedTime -= remainingTime;
                            remainingTime = 0.0f;
                            continue;
                        }

                        stepElapsedTime = 0.0f;
                        remainingTime -= timeToStepStart;
                        sequenceCompleted |= AdvanceBackward(wiringContext);
                    }
                }

                return sequenceCompleted;
            }

            public MovingPlatformPose EvaluatePose(in MovingPlatformBasePose basePose)
            {
                return layer != null ? layer.EvaluatePose(basePose, currentStepIndex, GetCurrentStepNormalizedTime()) :
                    new MovingPlatformPose(basePose.Position, basePose.Rotation, basePose.LocalScale);
            }

            public void ResetMotion(in WiringActionContext wiringContext)
            {
                ExitCurrentStep(wiringContext);
                ResetMotion();
            }

            public void ResetMotion()
            {
                currentStepIndex = 0;
                stepElapsedTime = 0.0f;
                direction = 1;
                onceCompleted = false;
                activeStepEntered = false;
            }

            private bool AdvanceForward(in WiringActionContext wiringContext)
            {
                int stepCount = layer.StepCount;

                switch (layer.PlaybackMode)
                {
                    case MovingPlatformPlaybackMode.Loop:
                        return AdvanceForwardLoop(wiringContext, stepCount);

                    case MovingPlatformPlaybackMode.PingPong:
                        return AdvanceForwardPingPong(wiringContext, stepCount);

                    default:
                        return AdvanceForwardOnce(wiringContext, stepCount);
                }
            }

            private bool AdvanceForwardOnce(in WiringActionContext wiringContext, int stepCount)
            {
                if (currentStepIndex < stepCount - 1)
                {
                    ChangeStep(currentStepIndex + 1, 0.0f, wiringContext);
                    return false;
                }

                ExitCurrentStep(wiringContext);
                onceCompleted = true;
                return true;
            }

            private bool AdvanceForwardLoop(in WiringActionContext wiringContext, int stepCount)
            {
                if (currentStepIndex < stepCount - 1)
                {
                    ChangeStep(currentStepIndex + 1, 0.0f, wiringContext);
                    return false;
                }

                ChangeStep(0, 0.0f, wiringContext);
                return true;
            }

            private bool AdvanceForwardPingPong(in WiringActionContext wiringContext, int stepCount)
            {
                if (stepCount <= 1 || currentStepIndex >= stepCount - 1)
                {
                    direction = -1;
                    return true;
                }

                ChangeStep(currentStepIndex + 1, 0.0f, wiringContext);
                return false;
            }

            private bool AdvanceBackward(in WiringActionContext wiringContext)
            {
                if (currentStepIndex <= 0)
                {
                    direction = 1;
                    return true;
                }

                int previousStepIndex = currentStepIndex - 1;
                ChangeStep(previousStepIndex, layer.GetStepDuration(previousStepIndex), wiringContext);
                return false;
            }

            private void ChangeStep(int nextStepIndex, float nextElapsedTime, in WiringActionContext wiringContext)
            {
                ExitCurrentStep(wiringContext);
                currentStepIndex = Mathf.Clamp(nextStepIndex, 0, layer.StepCount - 1);
                stepElapsedTime = Mathf.Clamp(nextElapsedTime, 0.0f, layer.GetStepDuration(currentStepIndex));
                EnsureStepEntered(wiringContext);
            }

            private void EnsureStepEntered(in WiringActionContext wiringContext)
            {
                if (activeStepEntered || layer == null)
                    return;

                if (layer.TryGetStep(currentStepIndex, out MovingPlatformMotionStep step))
                    step.ExecuteEnter(wiringContext);

                activeStepEntered = true;
            }

            private void ExitCurrentStep(in WiringActionContext wiringContext)
            {
                if (!activeStepEntered || layer == null)
                    return;

                if (layer.TryGetStep(currentStepIndex, out MovingPlatformMotionStep step))
                    step.ExecuteExit(wiringContext);

                activeStepEntered = false;
            }

            private float GetCurrentStepNormalizedTime()
            {
                if (layer == null || layer.StepCount <= 0)
                    return 0.0f;

                float duration = layer.GetStepDuration(currentStepIndex);
                return duration > 0.0f ? Mathf.Clamp01(stepElapsedTime / duration) : 0.0f;
            }
        }
    }
}