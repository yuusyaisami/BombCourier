using System;
using System.Collections.Generic;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MovingPlatformNodePathDropdownAttribute : PropertyAttribute
    {
    }

    [Serializable]
    public abstract class MovingPlatformLayerSegment
    {
        public abstract string SegmentName { get; }
    }

    [Serializable]
    public sealed class MovingPlatformRailRouteSegment : MovingPlatformLayerSegment
    {
        [Header("Segment")]
        [Tooltip("この区間の終点となる SharedRails 側 node path です。")]
        [SerializeField, MovingPlatformNodePathDropdown] private string targetNodePath = string.Empty;
        [Tooltip("SharedRails 側 connection の Duration / Easing を上書きするかを指定します。")]
        [SerializeField] private bool overrideConnectionTiming;

        [ShowIf(nameof(overrideConnectionTiming))]
        [Tooltip("区間時間を Duration で直接指定するか、Speed で距離から算出するかを指定します。")]
        [SerializeField] private MovingPlatformTimingControl timingControl = MovingPlatformTimingControl.Duration;

        [ShowIf(nameof(ShowDurationField))]
        [Tooltip("この区間の完了までにかかる時間です。")]
        [SerializeField, Min(0.01f)]
        private float duration = 1.0f;

        [ShowIf(nameof(ShowSpeedField))]
        [Tooltip("この区間の移動速度です。実際の区間距離から Duration を算出します。")]
        [SerializeField, Min(0.01f)]
        private float speed = 1.0f;

        [ShowIf(nameof(overrideConnectionTiming))]
        [Tooltip("この区間内の補間イージングです。")]
        [SerializeField]
        private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;

        public override string SegmentName => !string.IsNullOrWhiteSpace(TargetNodePath)
                ? TargetNodePath
            : "Move";

        public string TargetNodePath => MovingPlatformRailIdUtility.Normalize(targetNodePath);
        public bool OverrideConnectionTiming => overrideConnectionTiming;
        public MovingPlatformTimingControl TimingControl => timingControl;
        public float Duration => Mathf.Max(0.01f, duration);
        public float Speed => Mathf.Max(0.01f, speed);
        public MovingPlatformEasingMode EasingMode => easingMode;

        private bool ShowDurationField()
        {
            return overrideConnectionTiming && timingControl == MovingPlatformTimingControl.Duration;
        }

        private bool ShowSpeedField()
        {
            return overrideConnectionTiming && timingControl == MovingPlatformTimingControl.Speed;
        }

    }

    public enum MovingPlatformWaitMode
    {
        Duration = 0,
        Signal = 1,
    }

    [Serializable]
    public sealed class MovingPlatformWaitSegment : MovingPlatformLayerSegment
    {
        [SerializeField] private MovingPlatformWaitMode waitMode = MovingPlatformWaitMode.Duration;

        [ShowIf(nameof(ShowDurationField))]
        [SerializeField, Min(0.01f)]
        private float duration = 1.0f;

        [ShowIf(nameof(ShowSignalField))]
        [SerializeField, SignalDropdown]
        private KernelSignalReference signal;

        public override string SegmentName => waitMode == MovingPlatformWaitMode.Signal ? "Wait(Signal)" : "Wait";
        public MovingPlatformWaitMode WaitMode => waitMode;
        public float Duration => Mathf.Max(0.01f, duration);
        public KernelSignalReference Signal => signal;

        private bool ShowDurationField()
        {
            return waitMode == MovingPlatformWaitMode.Duration;
        }

        private bool ShowSignalField()
        {
            return waitMode == MovingPlatformWaitMode.Signal;
        }
    }

    [Serializable]
    public sealed class MovingPlatformInlineActionSegment : MovingPlatformLayerSegment
    {
        [LabelText("Actions")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [SerializeField]
        private WiringAction[] actions = Array.Empty<WiringAction>();

        public override string SegmentName => "InlineAction";

        public int Execute(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(actions, context);
        }
    }

    [Serializable]
    public sealed class MovingPlatformRotationSegment : MovingPlatformLayerSegment
    {
        [SerializeField, Min(0.01f)] private float duration = 1.0f;
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;
        [SerializeField] private Vector3 eulerDelta = new(0.0f, 90.0f, 0.0f);
        [SerializeField] private bool usePivotOffset;

        [ShowIf(nameof(usePivotOffset))]
        [SerializeField]
        private ReactiveVector3 pivotLocalOffset = default;

        public override string SegmentName => "Rotate";
        public float Duration => Mathf.Max(0.01f, duration);
        public MovingPlatformEasingMode EasingMode => easingMode;
        public Vector3 EulerDelta => eulerDelta;
        public bool UsePivotOffset => usePivotOffset;
        public ReactiveVector3 PivotLocalOffset => pivotLocalOffset;
    }

    public enum MovingPlatformScaleMode
    {
        Absolute = 0,
        Multiply = 1,
    }

    [Serializable]
    public sealed class MovingPlatformScaleSegment : MovingPlatformLayerSegment
    {
        [SerializeField, Min(0.01f)] private float duration = 1.0f;
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;
        [SerializeField] private MovingPlatformScaleMode scaleMode = MovingPlatformScaleMode.Absolute;
        [SerializeField] private Vector3 targetScale = Vector3.one;
        [SerializeField] private bool usePivotOffset;

        [ShowIf(nameof(usePivotOffset))]
        [SerializeField]
        private ReactiveVector3 pivotLocalOffset = default;

        public override string SegmentName => "Scale";
        public float Duration => Mathf.Max(0.01f, duration);
        public MovingPlatformEasingMode EasingMode => easingMode;
        public MovingPlatformScaleMode ScaleMode => scaleMode;
        public Vector3 TargetScale => targetScale;
        public bool UsePivotOffset => usePivotOffset;
        public ReactiveVector3 PivotLocalOffset => pivotLocalOffset;
    }

    [Serializable]
    public sealed class MovingPlatformLayer
    {
        [Header("Layer")]
        [Tooltip("レイヤーの表示名です。Inspector 上で識別しやすい名前を付けます。")]
        [SerializeField] private string layerName = "Layer";
        [Tooltip("複数レイヤーが同時に有効な時の優先順位です。値が大きいほど優先されます。")]
        [SerializeField] private int priority;
        [Tooltip("シーン開始時にこのレイヤーを有効として扱うかを指定します。")]
        [SerializeField] private bool activeOnStart = true;
        [Tooltip("このレイヤーが選ばれた時に再生位置を最初に戻すかを指定します。")]
        [SerializeField] private bool resetWhenSelected = true;
        [Tooltip("Editor と runtime の path preview で使う表示色です。alpha が 0 の時は自動色にフォールバックします。")]
        [SerializeField] private Color visualizationColor = Color.clear;

        [Header("Runtime Path Emission")]
        [Tooltip("runtime path line の発光設定を Layer 単位で上書きします。")]
        [SerializeField] private bool overrideRuntimePathEmission;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("有効状態の発光色です。")]
        [SerializeField] private Color runtimePathEmissionColor = Color.white;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("有効状態の発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathActiveEmissionStrength = 2.0f;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("無効状態の発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathInactiveEmissionStrength;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("EnvironmentStylizedLit の SimpleBoost 発光を同期するかを指定します。")]
        [SerializeField] private bool syncRuntimePathSimpleBoost = true;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("有効状態の SimpleBoost 発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathActiveSimpleBoostIntensity = 4.0f;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("無効状態の SimpleBoost 発光強度です。")]
        [SerializeField, Min(0.0f)] private float runtimePathInactiveSimpleBoostIntensity;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("無効状態のラインを薄く表示するかを指定します。")]
        [SerializeField] private bool dimRuntimePathWhenInactive = true;

        [ShowIf(nameof(overrideRuntimePathEmission))]
        [Tooltip("無効状態で乗算するラインのアルファ値です。")]
        [SerializeField, Range(0.0f, 1.0f)] private float runtimePathInactiveAlphaMultiplier = 0.35f;

        [Header("Condition")]
        [Tooltip("Kernel の Bool 値でこのレイヤーの有効/無効を制御するかを指定します。")]
        [SerializeField] private bool useKernelBoolCondition;

        [ShowIf(nameof(useKernelBoolCondition))]
        [Tooltip("有効判定に使う Kernel 側の Bool ValueKey です。")]
        [SerializeField, ValueKeyDropdown(typeof(bool), "Kernel")]
        private ValueKeyReference kernelActiveKey;

        [ShowIf(nameof(useKernelBoolCondition))]
        [Tooltip("ValueKey の値がこの値と一致した時にレイヤーを有効にします。")]
        [SerializeField]
        private bool activeWhenValue = true;

        [Header("Signals")]
        [Tooltip("Signal の受信でこのレイヤーの有効状態を切り替えるかを指定します。")]
        [SerializeField] private bool useSignalGate;

        [ShowIf(nameof(useSignalGate))]
        [Tooltip("この Signal を受信した時にレイヤーを有効化します。")]
        [SerializeField, SignalDropdown]
        private KernelSignalReference activateSignal;

        [ShowIf(nameof(useSignalGate))]
        [Tooltip("この Signal を受信した時にレイヤーを無効化します。")]
        [SerializeField, SignalDropdown]
        private KernelSignalReference deactivateSignal;

        [Header("Sequence")]
        [Tooltip("このレイヤー内の区間再生方法です。1 回だけ、ループ、往復を選べます。")]
        [SerializeField] private MovingPlatformPlaybackMode playbackMode = MovingPlatformPlaybackMode.PingPong;

        [Header("Default Move Timing")]
        [SerializeField] private MovingPlatformTimingControl defaultTimingControl = MovingPlatformTimingControl.Duration;

        [ShowIf(nameof(ShowDefaultDurationField))]
        [SerializeField, Min(0.01f)]
        private float defaultDuration = 1.0f;

        [ShowIf(nameof(ShowDefaultSpeedField))]
        [SerializeField, Min(0.01f)]
        private float defaultSpeed = 1.0f;

        [SerializeField] private MovingPlatformEasingMode defaultEasingMode = MovingPlatformEasingMode.SmoothStep;

        [Header("Rail Route")]
        [Tooltip("このレイヤーの開始ノードです。SharedRails 側の node path を指定します。")]
        [SerializeField, MovingPlatformNodePathDropdown] private string startNodePath = string.Empty;

        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, ListElementLabelName = "SegmentName")]
        [Tooltip("開始ノードの後に順番に再生する Segment 一覧です。Move/Wait/InlineAction/Rotate/Scale を混在できます。")]
        [SerializeReference]
        private List<MovingPlatformLayerSegment> routeSegments = new() { new MovingPlatformRailRouteSegment() };

        public string LayerName => layerName;
        public int Priority => priority;
        public bool ActiveOnStart => activeOnStart;
        public bool ResetWhenSelected => resetWhenSelected;
        public Color VisualizationColor => visualizationColor;
        public bool OverrideRuntimePathEmission => overrideRuntimePathEmission;
        public Color RuntimePathEmissionColor => runtimePathEmissionColor;
        public float RuntimePathActiveEmissionStrength => Mathf.Max(0.0f, runtimePathActiveEmissionStrength);
        public float RuntimePathInactiveEmissionStrength => Mathf.Max(0.0f, runtimePathInactiveEmissionStrength);
        public bool SyncRuntimePathSimpleBoost => syncRuntimePathSimpleBoost;
        public float RuntimePathActiveSimpleBoostIntensity => Mathf.Max(0.0f, runtimePathActiveSimpleBoostIntensity);
        public float RuntimePathInactiveSimpleBoostIntensity => Mathf.Max(0.0f, runtimePathInactiveSimpleBoostIntensity);
        public bool DimRuntimePathWhenInactive => dimRuntimePathWhenInactive;
        public float RuntimePathInactiveAlphaMultiplier => Mathf.Clamp01(runtimePathInactiveAlphaMultiplier);
        public bool UseKernelBoolCondition => useKernelBoolCondition;
        public bool UseSignalGate => useSignalGate;
        public ValueKeyReference KernelActiveKey => kernelActiveKey;
        public bool ActiveWhenValue => activeWhenValue;
        public MovingPlatformPlaybackMode PlaybackMode => playbackMode;
        public MovingPlatformTimingControl DefaultTimingControl => defaultTimingControl;
        public float DefaultDuration => Mathf.Max(0.01f, defaultDuration);
        public float DefaultSpeed => Mathf.Max(0.01f, defaultSpeed);
        public MovingPlatformEasingMode DefaultEasingMode => defaultEasingMode;
        public string StartNodePath => MovingPlatformRailIdUtility.Normalize(startNodePath);
        public bool UsesRailRoute => !string.IsNullOrWhiteSpace(StartNodePath) && routeSegments != null && routeSegments.Count > 0;
        public IReadOnlyList<MovingPlatformLayerSegment> Segments => routeSegments ?? (IReadOnlyList<MovingPlatformLayerSegment>)Array.Empty<MovingPlatformLayerSegment>();

        public int RouteSegmentCount
        {
            get
            {
                if (routeSegments == null)
                    return 0;

                int count = 0;
                for (int i = 0; i < routeSegments.Count; i++)
                {
                    if (routeSegments[i] is MovingPlatformRailRouteSegment)
                        count++;
                }

                return count;
            }
        }

        public bool MatchesActivateSignal(SignalId signalId)
        {
            return useSignalGate && activateSignal.TryResolve(out Signal signal) && signal.Id.Equals(signalId);
        }

        public bool MatchesDeactivateSignal(SignalId signalId)
        {
            return useSignalGate && deactivateSignal.TryResolve(out Signal signal) && signal.Id.Equals(signalId);
        }

        public bool TryGetRouteSegment(int index, out MovingPlatformRailRouteSegment segment)
        {
            if (routeSegments == null || index < 0)
            {
                segment = null;
                return false;
            }

            int moveIndex = -1;
            for (int i = 0; i < routeSegments.Count; i++)
            {
                if (routeSegments[i] is not MovingPlatformRailRouteSegment moveSegment)
                    continue;

                moveIndex++;
                if (moveIndex != index)
                    continue;

                segment = moveSegment;
                return true;
            }

            segment = null;
            return false;
        }

        public bool TryGetSegment(int index, out MovingPlatformLayerSegment segment)
        {
            if (routeSegments == null || index < 0 || index >= routeSegments.Count)
            {
                segment = null;
                return false;
            }

            segment = routeSegments[index];
            return segment != null;
        }

        private bool ShowDefaultDurationField()
        {
            return defaultTimingControl == MovingPlatformTimingControl.Duration;
        }

        private bool ShowDefaultSpeedField()
        {
            return defaultTimingControl == MovingPlatformTimingControl.Speed;
        }
    }
}
