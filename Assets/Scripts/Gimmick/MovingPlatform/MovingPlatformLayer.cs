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
    public sealed class MovingPlatformRailRouteSegment
    {
        [Header("Segment")]
        [Tooltip("区間の表示名です。Inspector 上で順番や役割を識別しやすくします。")]
        [SerializeField] private string segmentName = "Segment";
        [Tooltip("この区間の終点となる SharedRails 側 node path です。")]
        [SerializeField, MovingPlatformNodePathDropdown] private string targetNodePath = string.Empty;
        [Tooltip("SharedRails 側 connection の Duration / Easing を上書きするかを指定します。")]
        [SerializeField] private bool overrideConnectionTiming;

        [ShowIf(nameof(overrideConnectionTiming))]
        [Tooltip("この区間の完了までにかかる時間です。")]
        [SerializeField, Min(0.01f)]
        private float duration = 1.0f;

        [ShowIf(nameof(overrideConnectionTiming))]
        [Tooltip("この区間内の補間イージングです。")]
        [SerializeField]
        private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;

        [Header("Actions")]
        [LabelText("Enter Actions")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("この区間へ入った時に 1 回だけ実行する WiringAction の一覧です。")]
        [SerializeField]
        private WiringAction[] onEnterActions = Array.Empty<WiringAction>();

        [LabelText("Exit Actions")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("この区間を抜ける時に 1 回だけ実行する WiringAction の一覧です。")]
        [SerializeField]
        private WiringAction[] onExitActions = Array.Empty<WiringAction>();

        public string SegmentName => !string.IsNullOrWhiteSpace(segmentName)
            ? segmentName
            : !string.IsNullOrWhiteSpace(TargetNodePath)
                ? TargetNodePath
                : "Segment";

        public string TargetNodePath => MovingPlatformRailIdUtility.Normalize(targetNodePath);
        public bool OverrideConnectionTiming => overrideConnectionTiming;
        public float Duration => Mathf.Max(0.01f, duration);
        public MovingPlatformEasingMode EasingMode => easingMode;

        public int ExecuteEnter(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(onEnterActions, context);
        }

        public int ExecuteExit(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(onExitActions, context);
        }
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

        [Header("Rail Route")]
        [Tooltip("このレイヤーの開始ノードです。SharedRails 側の node path を指定します。")]
        [SerializeField, MovingPlatformNodePathDropdown] private string startNodePath = string.Empty;

        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, ListElementLabelName = "SegmentName")]
        [Tooltip("開始ノードの後に順番に再生する SharedRails 区間一覧です。各要素で次の node path と、その区間専用の timing override を定義できます。")]
        [SerializeField]
        private MovingPlatformRailRouteSegment[] routeSegments = { new MovingPlatformRailRouteSegment() };

        public string LayerName => layerName;
        public int Priority => priority;
        public bool ActiveOnStart => activeOnStart;
        public bool ResetWhenSelected => resetWhenSelected;
        public Color VisualizationColor => visualizationColor;
        public bool UseKernelBoolCondition => useKernelBoolCondition;
        public bool UseSignalGate => useSignalGate;
        public ValueKeyReference KernelActiveKey => kernelActiveKey;
        public bool ActiveWhenValue => activeWhenValue;
        public MovingPlatformPlaybackMode PlaybackMode => playbackMode;
        public string StartNodePath => MovingPlatformRailIdUtility.Normalize(startNodePath);
        public bool UsesRailRoute => !string.IsNullOrWhiteSpace(StartNodePath) && routeSegments != null && routeSegments.Length > 0;
        public IReadOnlyList<MovingPlatformRailRouteSegment> RouteSegments => routeSegments ?? Array.Empty<MovingPlatformRailRouteSegment>();
        public int RouteSegmentCount => routeSegments != null ? routeSegments.Length : 0;

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
            if (routeSegments == null || index < 0 || index >= routeSegments.Length)
            {
                segment = null;
                return false;
            }

            segment = routeSegments[index];
            return segment != null;
        }
    }
}
