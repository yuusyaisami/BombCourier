using System;
using System.Collections.Generic;
using BC.ActionSystem;
using UnityEngine;

namespace BC.Camera
{
    public enum CameraPathTransitionKind
    {
        Cut = 0,
        Ease = 1,
    }

    [Serializable]
    public struct CameraPathTransitionSettings
    {
        [SerializeField] private CameraPathTransitionKind kind;
        [SerializeField, Min(0.0f)] private float duration;
        [SerializeField] private AnimationCurve ease;

        public CameraPathTransitionKind Kind => kind;
        public float Duration => Mathf.Max(0.0f, duration);
        public AnimationCurve Ease => ease;

        public float Evaluate(float t)
        {
            t = Mathf.Clamp01(t);

            if (kind == CameraPathTransitionKind.Cut || Duration <= 0.0f)
                return 1.0f;

            return ease != null ? Mathf.Clamp01(ease.Evaluate(t)) : SmoothStep(t);
        }

        public static CameraPathTransitionSettings Cut()
        {
            return new CameraPathTransitionSettings
            {
                kind = CameraPathTransitionKind.Cut,
                duration = 0.0f,
            };
        }

        public static CameraPathTransitionSettings EaseInOut(float duration)
        {
            return new CameraPathTransitionSettings
            {
                kind = CameraPathTransitionKind.Ease,
                duration = Mathf.Max(0.0f, duration),
                ease = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f),
            };
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3.0f - 2.0f * t);
        }
    }

    [Serializable]
    public struct CameraPathLensSettings
    {
        [SerializeField] private bool overrideFieldOfView;
        [SerializeField, Range(1.0f, 179.0f)] private float fieldOfView;

        public bool OverrideFieldOfView => overrideFieldOfView;
        public float FieldOfView => Mathf.Clamp(fieldOfView <= 0.0f ? 60.0f : fieldOfView, 1.0f, 179.0f);
    }

    [Serializable]
    public struct CameraPathPointDefinition
    {
        [SerializeField] private string label;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 eulerAngles;
        [SerializeField, Min(0.0f)] private float holdSeconds;
        [SerializeField] private CameraPathTransitionSettings transitionFromPrevious;
        [SerializeField] private CameraPathLensSettings lens;
        [SerializeField] private InlineAction onArriveAction;

        public string Label => label;
        public Vector3 Position => position;
        public Quaternion Rotation => Quaternion.Euler(eulerAngles);
        public Vector3 EulerAngles => eulerAngles;
        public float HoldSeconds => Mathf.Max(0.0f, holdSeconds);
        public CameraPathTransitionSettings TransitionFromPrevious => transitionFromPrevious;
        public CameraPathLensSettings Lens => lens;
        public InlineAction OnArriveAction => onArriveAction;

        public CameraPathPointDefinition(
            string label,
            Vector3 position,
            Quaternion rotation,
            float holdSeconds,
            CameraPathTransitionSettings transitionFromPrevious,
            CameraPathLensSettings lens,
            InlineAction onArriveAction)
        {
            this.label = label;
            this.position = position;
            this.eulerAngles = rotation.eulerAngles;
            this.holdSeconds = Mathf.Max(0.0f, holdSeconds);
            this.transitionFromPrevious = transitionFromPrevious;
            this.lens = lens;
            this.onArriveAction = onArriveAction;
        }
    }

    public interface ICameraPathPointSource
    {
        bool TryBuildPoint(out CameraPathPointDefinition point);
    }

    public interface ICameraPathSequenceSource
    {
        IReadOnlyList<CameraPathPointDefinition> BuildSequence();
    }

    public sealed class CameraPathSequenceDefinition
    {
        private readonly CameraPathPointDefinition[] points;

        public CameraPathSequenceDefinition(IReadOnlyList<CameraPathPointDefinition> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            this.points = new CameraPathPointDefinition[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                this.points[i] = points[i];
            }
        }

        public IReadOnlyList<CameraPathPointDefinition> Points => points;
        public int Count => points.Length;
    }
}