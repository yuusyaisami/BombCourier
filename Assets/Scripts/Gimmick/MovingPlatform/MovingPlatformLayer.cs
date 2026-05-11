using System;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    [Serializable]
    public sealed class MovingPlatformLayer
    {
        [Header("Layer")]
        [SerializeField] private string layerName = "Layer";
        [SerializeField] private int priority;
        [SerializeField] private bool activeOnStart = true;
        [SerializeField] private bool resetWhenSelected = true;

        [Header("Condition")]
        [SerializeField] private bool useKernelBoolCondition;

        [ShowIf(nameof(useKernelBoolCondition))]
        [SerializeField, ValueKeyDropdown(typeof(bool), "Kernel")]
        private ValueKeyReference kernelActiveKey;

        [ShowIf(nameof(useKernelBoolCondition))]
        [SerializeField]
        private bool activeWhenValue = true;

        [Header("Signals")]
        [SerializeField] private bool useSignalGate;

        [ShowIf(nameof(useSignalGate))]
        [SerializeField, SignalDropdown]
        private KernelSignalReference activateSignal;

        [ShowIf(nameof(useSignalGate))]
        [SerializeField, SignalDropdown]
        private KernelSignalReference deactivateSignal;

        [Header("Motion")]
        [SerializeField, Min(0.01f)] private float duration = 2.0f;
        [SerializeField] private MovingPlatformPlaybackMode playbackMode = MovingPlatformPlaybackMode.PingPong;
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;
        [SerializeField] private MovingPlatformPathMode pathMode = MovingPlatformPathMode.LocalOffset;

        [ShowIf(nameof(UsesLocalOffsetPath))]
        [SerializeField]
        private Vector3 localPositionOffset = new(0.0f, 0.0f, 4.0f);

        [SerializeField] private Vector3 localEulerOffset;
        [SerializeField] private Vector3 targetLocalScaleMultiplier = Vector3.one;

        [Header("Transform Points")]

        [ShowIf(nameof(UsesTransformPointsPath))]
        [LabelText("Path Points")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
        [SerializeField]
        private Transform[] pathPoints = Array.Empty<Transform>();

        [Header("Cubic Bezier")]

        [TitleGroup("Path / Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("Start")]
        [SerializeField]
        private Transform bezierStart;

        [TitleGroup("Path / Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("Control A")]
        [SerializeField]
        private Transform bezierControlA;

        [TitleGroup("Path / Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("Control B")]
        [SerializeField]
        private Transform bezierControlB;

        [TitleGroup("Path / Cubic Bezier")]
        [ShowIf(nameof(UsesBezierPath))]
        [LabelText("End")]
        [SerializeField]
        private Transform bezierEnd;

        public string LayerName => layerName;
        public int Priority => priority;
        public bool ActiveOnStart => activeOnStart;
        public bool ResetWhenSelected => resetWhenSelected;
        public bool UseKernelBoolCondition => useKernelBoolCondition;
        public ValueKeyReference KernelActiveKey => kernelActiveKey;
        public bool ActiveWhenValue => activeWhenValue;
        public float Duration => Mathf.Max(0.01f, duration);
        public MovingPlatformPlaybackMode PlaybackMode => playbackMode;

        private bool UsesLocalOffsetPath => pathMode == MovingPlatformPathMode.LocalOffset;
        private bool UsesTransformPointsPath => pathMode == MovingPlatformPathMode.TransformPoints;
        private bool UsesBezierPath => pathMode == MovingPlatformPathMode.CubicBezier;

        public bool MatchesActivateSignal(SignalId signalId)
        {
            return useSignalGate && activateSignal.TryResolve(out Signal signal) && signal.Id.Equals(signalId);
        }

        public bool MatchesDeactivateSignal(SignalId signalId)
        {
            return useSignalGate && deactivateSignal.TryResolve(out Signal signal) && signal.Id.Equals(signalId);
        }

        public MovingPlatformPose EvaluatePose(in MovingPlatformBasePose basePose, float normalizedTime)
        {
            float easedTime = Ease(Mathf.Clamp01(normalizedTime));
            Vector3 position = EvaluatePosition(basePose, easedTime);
            Quaternion rotation = basePose.Rotation * Quaternion.Slerp(
                Quaternion.identity,
                Quaternion.Euler(localEulerOffset),
                easedTime);
            Vector3 targetScale = Vector3.Scale(basePose.LocalScale, SanitizeScaleMultiplier(targetLocalScaleMultiplier));
            Vector3 scale = Vector3.Lerp(basePose.LocalScale, targetScale, easedTime);

            return new MovingPlatformPose(position, rotation, scale);
        }

        private Vector3 EvaluatePosition(in MovingPlatformBasePose basePose, float easedTime)
        {
            switch (pathMode)
            {
                case MovingPlatformPathMode.TransformPoints:
                    if (TryEvaluateTransformPoints(easedTime, out Vector3 pointPosition))
                        return pointPosition;
                    break;

                case MovingPlatformPathMode.CubicBezier:
                    if (TryEvaluateBezier(easedTime, out Vector3 bezierPosition))
                        return bezierPosition;
                    break;
            }

            return basePose.Position + basePose.Rotation * Vector3.Lerp(Vector3.zero, localPositionOffset, easedTime);
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
}