using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    [DisallowMultipleComponent]
    public sealed class MovingPlatformMB : MonoBehaviour, IMovingPlatformMotionSource
    {
        [Header("Layers")]
        [LabelText("Layers")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [SerializeField]
        private MovingPlatformLayer[] layers = System.Array.Empty<MovingPlatformLayer>();

        [Header("Signals")]
        [SerializeField] private bool publishLayerSignals = true;

        [ShowIf(nameof(publishLayerSignals))]
        [SerializeField, SignalDropdown("Gimmick.MovingPlatform")]
        private KernelSignalReference layerEnabledSignal =
            KernelSignalReference.From(Signals.Gimmick.MovingPlatform.LayerEnabled);

        [ShowIf(nameof(publishLayerSignals))]
        [SerializeField, SignalDropdown("Gimmick.MovingPlatform")]
        private KernelSignalReference layerDisabledSignal =
            KernelSignalReference.From(Signals.Gimmick.MovingPlatform.LayerDisabled);

        [ShowIf(nameof(publishLayerSignals))]
        [SerializeField, SignalDropdown("Gimmick.MovingPlatform")]
        private KernelSignalReference sequenceCompletedSignal =
            KernelSignalReference.From(Signals.Gimmick.MovingPlatform.SequenceCompleted);

        private MovingPlatformLayerRuntime[] runtimes;
        private MovingPlatformBasePose basePose;
        private SceneKernel sceneKernel;
        private EventSubscription signalSubscription;
        private int selectedLayerIndex = -1;

        private Vector3 accumulatedPositionDelta;
        private Quaternion accumulatedRotationDelta = Quaternion.identity;
        private bool hasAccumulatedMotion;

        private void Awake()
        {
            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null || kernelMB.Kernel == null)
            {
                Debug.LogError($"{nameof(MovingPlatformMB)}: SceneKernelMB was not found.", this);
                enabled = false;
                return;
            }

            sceneKernel = kernelMB.Kernel;
            basePose = new MovingPlatformBasePose(transform.position, transform.rotation, transform.localScale);
            BuildLayerRuntimes();
            signalSubscription = sceneKernel.KernelEvents.Subscribe<KernelSignalRaisedEvent>(OnKernelSignalRaised);
        }

        private void OnDestroy()
        {
            signalSubscription?.Dispose();
            signalSubscription = null;
        }

        private void FixedUpdate()
        {
            if (runtimes == null || runtimes.Length == 0)
                return;

            TickLayers(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            accumulatedPositionDelta = Vector3.zero;
            accumulatedRotationDelta = Quaternion.identity;
            hasAccumulatedMotion = false;
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

        private void TickLayers(float deltaTime)
        {
            int nextSelectedLayerIndex = SelectActiveLayerIndex();

            if (nextSelectedLayerIndex != selectedLayerIndex)
                ChangeSelectedLayer(nextSelectedLayerIndex);

            if (selectedLayerIndex < 0)
                return;

            MovingPlatformLayerRuntime runtime = runtimes[selectedLayerIndex];
            bool completed = runtime.Tick(deltaTime);
            ApplyPose(runtime.EvaluatePose(basePose));

            if (completed)
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

            MovingPlatformLayerRuntime runtime = runtimes[selectedLayerIndex];

            if (runtime.ResetWhenSelected)
                runtime.ResetMotion();

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

        private sealed class MovingPlatformLayerRuntime
        {
            private readonly MovingPlatformLayer layer;
            private ValueWatchHandle<bool> activeValueHandle;
            private bool signalGateActive;
            private float normalizedTime;
            private int direction = 1;
            private bool onceCompleted;

            public int Priority => layer != null ? layer.Priority : int.MinValue;
            public bool ResetWhenSelected => layer != null && layer.ResetWhenSelected;

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

                if (!signalGateActive)
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
                if (layer == null)
                    return;

                if (layer.MatchesActivateSignal(signalId))
                    signalGateActive = true;

                if (layer.MatchesDeactivateSignal(signalId))
                    signalGateActive = false;
            }

            public bool Tick(float deltaTime)
            {
                if (layer == null || deltaTime <= 0.0f)
                    return false;

                float step = deltaTime / layer.Duration;

                switch (layer.PlaybackMode)
                {
                    case MovingPlatformPlaybackMode.Loop:
                        normalizedTime += step;

                        if (normalizedTime < 1.0f)
                            return false;

                        normalizedTime -= Mathf.Floor(normalizedTime);
                        return true;

                    case MovingPlatformPlaybackMode.PingPong:
                        normalizedTime += step * direction;

                        if (normalizedTime >= 1.0f)
                        {
                            normalizedTime = 1.0f;
                            direction = -1;
                            return true;
                        }

                        if (normalizedTime <= 0.0f)
                        {
                            normalizedTime = 0.0f;
                            direction = 1;
                            return true;
                        }

                        return false;
                    default:
                        if (onceCompleted)
                            return false;

                        normalizedTime += step;

                        if (normalizedTime < 1.0f)
                            return false;

                        normalizedTime = 1.0f;
                        onceCompleted = true;
                        return true;
                }
            }

            public MovingPlatformPose EvaluatePose(in MovingPlatformBasePose basePose)
            {
                return layer != null ? layer.EvaluatePose(basePose, normalizedTime) :
                    new MovingPlatformPose(basePose.Position, basePose.Rotation, basePose.LocalScale);
            }

            public void ResetMotion()
            {
                normalizedTime = 0.0f;
                direction = 1;
                onceCompleted = false;
            }
        }
    }
}