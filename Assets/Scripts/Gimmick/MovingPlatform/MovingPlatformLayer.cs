using System;
using System.Collections.Generic;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    [Serializable]
    public sealed class MovingPlatformMotionStep
    {
        [Header("Step")]
        [Tooltip("ステップの表示名です。Inspector 上で順番や役割を識別しやすくします。")]
        [SerializeField] private string stepName = "Step";
        [Tooltip("このステップの移動・回転・スケール指定をどの基準で解釈するかを指定します。")]
        [SerializeField] private MovingPlatformStepPoseBasis poseBasis = MovingPlatformStepPoseBasis.LayerBase;
        [Tooltip("このステップの完了までにかかる時間です。")]
        [SerializeField, Min(0.01f)] private float duration = 2.0f;
        [Tooltip("このステップ内の補間イージングです。")]
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;
        [Tooltip("このステップの位置経路をローカルオフセット、Transform 参照ポイント列、Vector3 ポイント列、ベジェのどれで定義するかを指定します。")]
        [SerializeField] private MovingPlatformPathMode pathMode = MovingPlatformPathMode.LocalOffset;

        [ShowIf(nameof(UsesLocalOffsetPath))]
        [Tooltip("LocalOffset モード時の目標位置です。基準モードに応じて相対値またはワールド座標として扱われます。")]
        [SerializeField]
        private Vector3 localPositionOffset = new(0.0f, 0.0f, 4.0f);

        [Tooltip("このステップの目標回転です。基準モードに応じて相対回転またはワールド回転として扱われます。")]
        [SerializeField] private Vector3 localEulerOffset;
        [Tooltip("このステップの目標スケールです。相対基準では倍率、World 基準では絶対スケールとして扱います。")]
        [SerializeField] private Vector3 targetLocalScaleMultiplier = Vector3.one;

        [Header("Transform Points")]

        [ShowIf(nameof(UsesTransformPointsPath))]
        [LabelText("Path Points")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("TransformPoints モードでこのステップ中に順番に通過する Transform 参照ポイント一覧です。")]
        [SerializeField]
        private Transform[] pathPoints = Array.Empty<Transform>();

        [Header("Vector3 Points")]

        [ShowIf(nameof(UsesVector3PointsPath))]
        [LabelText("Vector3 Points")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("Vector3Points モードでこのステップ中に順番に通過する数値ポイント一覧です。基準モードに応じて相対値またはワールド座標として解釈されます。")]
        [SerializeField]
        private Vector3[] vectorPathPoints = Array.Empty<Vector3>();

        [Header("Cubic Bezier")]

        [BoxGroup("Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("Start")]
        [Tooltip("このステップのベジェ曲線の始点です。")]
        [SerializeField]
        private Transform bezierStart;

        [BoxGroup("Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("Control A")]
        [Tooltip("このステップのベジェ曲線の第 1 制御点です。")]
        [SerializeField]
        private Transform bezierControlA;

        [BoxGroup("Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("Control B")]
        [Tooltip("このステップのベジェ曲線の第 2 制御点です。")]
        [SerializeField]
        private Transform bezierControlB;

        [BoxGroup("Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("End")]
        [Tooltip("このステップのベジェ曲線の終点です。")]
        [SerializeField]
        private Transform bezierEnd;

        [Header("Actions")]
        [LabelText("Enter Actions")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("このステップへ入った時に 1 回だけ実行する WiringAction の一覧です。")]
        [SerializeField]
        private WiringAction[] onEnterActions = Array.Empty<WiringAction>();

        [LabelText("Exit Actions")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [Tooltip("このステップを抜ける時に 1 回だけ実行する WiringAction の一覧です。")]
        [SerializeField]
        private WiringAction[] onExitActions = Array.Empty<WiringAction>();

        public string StepName => stepName;
        public float Duration => Mathf.Max(0.01f, duration);

        private bool UsesLocalOffsetPath => pathMode == MovingPlatformPathMode.LocalOffset;
        private bool UsesTransformPointsPath => pathMode == MovingPlatformPathMode.TransformPoints;
        private bool UsesVector3PointsPath => pathMode == MovingPlatformPathMode.Vector3Points;
        private bool UsesBezierPath => pathMode == MovingPlatformPathMode.CubicBezier;

        public int ExecuteEnter(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(onEnterActions, context);
        }

        public int ExecuteExit(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(onExitActions, context);
        }

        public MovingPlatformPose EvaluatePose(
            in MovingPlatformBasePose basePose,
            in MovingPlatformPose startPose,
            float normalizedTime)
        {
            float easedTime = Ease(Mathf.Clamp01(normalizedTime));
            MovingPlatformPose targetPose = ResolveTargetPose(basePose, startPose);
            Vector3 position = EvaluatePosition(basePose, startPose, targetPose, easedTime);
            Quaternion rotation = Quaternion.Slerp(startPose.Rotation, targetPose.Rotation, easedTime);
            Vector3 scale = Vector3.Lerp(startPose.LocalScale, targetPose.LocalScale, easedTime);

            return new MovingPlatformPose(position, rotation, scale);
        }

        private MovingPlatformPose ResolveTargetPose(
            in MovingPlatformBasePose basePose,
            in MovingPlatformPose startPose)
        {
            Vector3 safeScale = SanitizeScaleMultiplier(targetLocalScaleMultiplier);

            switch (poseBasis)
            {
                case MovingPlatformStepPoseBasis.PreviousStepEnd:
                    return new MovingPlatformPose(
                        startPose.Position + startPose.Rotation * localPositionOffset,
                        startPose.Rotation * Quaternion.Euler(localEulerOffset),
                        Vector3.Scale(startPose.LocalScale, safeScale));

                case MovingPlatformStepPoseBasis.World:
                    return new MovingPlatformPose(
                        localPositionOffset,
                        Quaternion.Euler(localEulerOffset),
                        safeScale);

                default:
                    return new MovingPlatformPose(
                        basePose.Position + basePose.Rotation * localPositionOffset,
                        basePose.Rotation * Quaternion.Euler(localEulerOffset),
                        Vector3.Scale(basePose.LocalScale, safeScale));
            }
        }

        private Vector3 EvaluatePosition(
            in MovingPlatformBasePose basePose,
            in MovingPlatformPose startPose,
            in MovingPlatformPose targetPose,
            float easedTime)
        {
            switch (pathMode)
            {
                case MovingPlatformPathMode.TransformPoints:
                    if (TryEvaluateTransformPoints(easedTime, out Vector3 pointPosition))
                        return pointPosition;
                    break;

                case MovingPlatformPathMode.Vector3Points:
                    if (TryEvaluateVector3Points(basePose, startPose, easedTime, out Vector3 vectorPointPosition))
                        return vectorPointPosition;
                    break;

                case MovingPlatformPathMode.CubicBezier:
                    if (TryEvaluateBezier(easedTime, out Vector3 bezierPosition))
                        return bezierPosition;
                    break;
            }

            return Vector3.Lerp(startPose.Position, targetPose.Position, easedTime);
        }

        private bool TryEvaluateTransformPoints(float easedTime, out Vector3 position)
        {
            position = default;

            if (pathPoints == null)
                return false;

            int validCount = CountValidPathPoints();

            if (validCount <= 0)
                return false;

            if (validCount == 1)
            {
                position = GetValidPathPoint(0).position;
                return true;
            }

            float scaledTime = easedTime * (validCount - 1);
            int index = Mathf.Min(Mathf.FloorToInt(scaledTime), validCount - 2);
            float segmentTime = scaledTime - index;

            Transform from = GetValidPathPoint(index);
            Transform to = GetValidPathPoint(index + 1);
            position = Vector3.Lerp(from.position, to.position, segmentTime);
            return true;
        }

        private bool TryEvaluateVector3Points(
            in MovingPlatformBasePose basePose,
            in MovingPlatformPose startPose,
            float easedTime,
            out Vector3 position)
        {
            position = default;

            if (vectorPathPoints == null || vectorPathPoints.Length == 0)
                return false;

            if (vectorPathPoints.Length == 1)
            {
                position = ResolveVectorPoint(basePose, startPose, vectorPathPoints[0]);
                return true;
            }

            float scaledTime = easedTime * (vectorPathPoints.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(scaledTime), vectorPathPoints.Length - 2);
            float segmentTime = scaledTime - index;

            Vector3 from = ResolveVectorPoint(basePose, startPose, vectorPathPoints[index]);
            Vector3 to = ResolveVectorPoint(basePose, startPose, vectorPathPoints[index + 1]);
            position = Vector3.Lerp(from, to, segmentTime);
            return true;
        }

        private int CountValidPathPoints()
        {
            int count = 0;

            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] != null)
                    count++;
            }

            return count;
        }

        private Transform GetValidPathPoint(int validIndex)
        {
            int count = 0;

            for (int i = 0; i < pathPoints.Length; i++)
            {
                Transform point = pathPoints[i];

                if (point == null)
                    continue;

                if (count == validIndex)
                    return point;

                count++;
            }

            return null;
        }

        internal void AppendVisualizationAnchors(
            in MovingPlatformBasePose basePose,
            in MovingPlatformPose startPose,
            List<Vector3> anchors)
        {
            if (anchors == null)
                return;

            switch (pathMode)
            {
                case MovingPlatformPathMode.TransformPoints:
                    int validCount = CountValidPathPoints();

                    for (int i = 0; i < validCount; i++)
                    {
                        Transform point = GetValidPathPoint(i);

                        if (point != null)
                            anchors.Add(point.position);
                    }
                    break;

                case MovingPlatformPathMode.Vector3Points:
                    if (vectorPathPoints == null)
                        return;

                    for (int i = 0; i < vectorPathPoints.Length; i++)
                        anchors.Add(ResolveVectorPoint(basePose, startPose, vectorPathPoints[i]));
                    break;

                case MovingPlatformPathMode.CubicBezier:
                    if (bezierStart != null)
                        anchors.Add(bezierStart.position);

                    if (bezierControlA != null)
                        anchors.Add(bezierControlA.position);

                    if (bezierControlB != null)
                        anchors.Add(bezierControlB.position);

                    if (bezierEnd != null)
                        anchors.Add(bezierEnd.position);
                    break;
            }
        }

        private bool TryEvaluateBezier(float easedTime, out Vector3 position)
        {
            position = default;

            if (bezierStart == null || bezierControlA == null || bezierControlB == null || bezierEnd == null)
                return false;

            float inverseTime = 1.0f - easedTime;
            position =
                inverseTime * inverseTime * inverseTime * bezierStart.position +
                3.0f * inverseTime * inverseTime * easedTime * bezierControlA.position +
                3.0f * inverseTime * easedTime * easedTime * bezierControlB.position +
                easedTime * easedTime * easedTime * bezierEnd.position;
            return true;
        }

        private Vector3 ResolveVectorPoint(
            in MovingPlatformBasePose basePose,
            in MovingPlatformPose startPose,
            Vector3 authoredPoint)
        {
            switch (poseBasis)
            {
                case MovingPlatformStepPoseBasis.PreviousStepEnd:
                    return startPose.Position + startPose.Rotation * authoredPoint;

                case MovingPlatformStepPoseBasis.World:
                    return authoredPoint;

                default:
                    return basePose.Position + basePose.Rotation * authoredPoint;
            }
        }

        private float Ease(float time)
        {
            return easingMode switch
            {
                MovingPlatformEasingMode.EaseInOutSine => 0.5f - 0.5f * Mathf.Cos(time * Mathf.PI),
                MovingPlatformEasingMode.SmoothStep => time * time * (3.0f - 2.0f * time),
                _ => time,
            };
        }

        private static Vector3 SanitizeScaleMultiplier(Vector3 multiplier)
        {
            return new Vector3(
                Mathf.Approximately(multiplier.x, 0.0f) ? 1.0f : multiplier.x,
                Mathf.Approximately(multiplier.y, 0.0f) ? 1.0f : multiplier.y,
                Mathf.Approximately(multiplier.z, 0.0f) ? 1.0f : multiplier.z);
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
        [Tooltip("このレイヤー内のステップ再生方法です。1 回だけ、ループ、往復を選べます。")]
        [SerializeField] private MovingPlatformPlaybackMode playbackMode = MovingPlatformPlaybackMode.PingPong;

        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, ListElementLabelName = "StepName")]
        [Tooltip("このレイヤーが有効な時に順番に再生する移動ステップ一覧です。")]
        [SerializeField]
        private MovingPlatformMotionStep[] steps = { new MovingPlatformMotionStep() };

        public string LayerName => layerName;
        public int Priority => priority;
        public bool ActiveOnStart => activeOnStart;
        public bool ResetWhenSelected => resetWhenSelected;
        public bool UseKernelBoolCondition => useKernelBoolCondition;
        public bool UseSignalGate => useSignalGate;
        public ValueKeyReference KernelActiveKey => kernelActiveKey;
        public bool ActiveWhenValue => activeWhenValue;
        public MovingPlatformPlaybackMode PlaybackMode => playbackMode;
        public int StepCount => steps != null ? steps.Length : 0;

        public bool MatchesActivateSignal(SignalId signalId)
        {
            return useSignalGate && activateSignal.TryResolve(out Signal signal) && signal.Id.Equals(signalId);
        }

        public bool MatchesDeactivateSignal(SignalId signalId)
        {
            return useSignalGate && deactivateSignal.TryResolve(out Signal signal) && signal.Id.Equals(signalId);
        }

        public bool TryGetStep(int index, out MovingPlatformMotionStep step)
        {
            if (steps == null || index < 0 || index >= steps.Length)
            {
                step = null;
                return false;
            }

            step = steps[index];
            return step != null;
        }

        public float GetStepDuration(int index)
        {
            return TryGetStep(index, out MovingPlatformMotionStep step) ? step.Duration : 0.01f;
        }

        public MovingPlatformPose EvaluatePose(
            in MovingPlatformBasePose basePose,
            int stepIndex,
            float normalizedTime)
        {
            if (StepCount <= 0)
                return basePose.ToPose();

            int clampedStepIndex = Mathf.Clamp(stepIndex, 0, StepCount - 1);
            MovingPlatformPose stepStartPose = basePose.ToPose();

            for (int i = 0; i < clampedStepIndex; i++)
            {
                if (TryGetStep(i, out MovingPlatformMotionStep previousStep))
                    stepStartPose = previousStep.EvaluatePose(basePose, stepStartPose, 1.0f);
            }

            if (!TryGetStep(clampedStepIndex, out MovingPlatformMotionStep currentStep))
                return stepStartPose;

            return currentStep.EvaluatePose(basePose, stepStartPose, normalizedTime);
        }

    }
}