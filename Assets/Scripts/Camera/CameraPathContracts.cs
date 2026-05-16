using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Base;
using Unity.VisualScripting;
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
        [SerializeField] private ReactiveVector3 position;
        [SerializeField] private ReactiveVector3 eulerAngles;
        [SerializeField, Min(0.0f)] private float holdSeconds;
        [SerializeField] private CameraPathTransitionSettings transitionFromPrevious;
        [SerializeField] private CameraPathLensSettings lens;
        [SerializeField] private InlineAction onArriveAction;

        public string Label => label;
        public ReactiveVector3 Position => position;
        public ReactiveVector3 EulerAngles => eulerAngles;
        public float HoldSeconds => Mathf.Max(0.0f, holdSeconds);
        public CameraPathTransitionSettings TransitionFromPrevious => transitionFromPrevious;
        public CameraPathLensSettings Lens => lens;
        public InlineAction OnArriveAction => onArriveAction;

        public bool HasLiteralPose =>
            position.SourceKind == ReactiveVector3SourceKind.Literal &&
            eulerAngles.SourceKind == ReactiveVector3SourceKind.Literal;

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
            this.position = ReactiveVector3.LiteralValue(position);
            this.eulerAngles = ReactiveVector3.LiteralValue(rotation.eulerAngles);
            this.holdSeconds = Mathf.Max(0.0f, holdSeconds);
            this.transitionFromPrevious = transitionFromPrevious;
            this.lens = lens;
            this.onArriveAction = onArriveAction;
        }

        public bool TryGetLiteralPose(out Vector3 literalPosition, out Quaternion literalRotation)
        {
            literalPosition = Vector3.zero;
            literalRotation = Quaternion.identity;

            if (!HasLiteralPose)
                return false;

            literalPosition = position.Literal;
            literalRotation = Quaternion.Euler(eulerAngles.Literal);
            return true;
        }

        public bool TryGetLiteralPosition(out Vector3 literalPosition)
        {
            literalPosition = Vector3.zero;

            if (position.SourceKind != ReactiveVector3SourceKind.Literal)
                return false;

            literalPosition = position.Literal;
            return true;
        }

        public bool TryGetLiteralRotation(out Quaternion literalRotation)
        {
            literalRotation = Quaternion.identity;

            if (eulerAngles.SourceKind != ReactiveVector3SourceKind.Literal)
                return false;

            literalRotation = Quaternion.Euler(eulerAngles.Literal);
            return true;
        }

        public bool TryResolve(
            in ReactiveEvalContext context,
            ReactiveValueResolverService resolver,
            out CameraPathResolvedPoint point,
            out ReactiveError error)
        {
            point = default;
            error = default;

            if (resolver == null)
            {
                error = new ReactiveError(
                    ReactiveErrorCode.MissingSceneKernel,
                    "Reactive value resolver is not available.",
                    context.ActorEntity,
                    context.TriggerEntity);
                return false;
            }

            ReactiveResult<Vector3> positionResult = resolver.ResolveVector3(context, position);

            if (positionResult.Failed)
            {
                error = positionResult.Error;
                return false;
            }

            ReactiveResult<Vector3> eulerAnglesResult = resolver.ResolveVector3(context, eulerAngles);

            if (eulerAnglesResult.Failed)
            {
                error = eulerAnglesResult.Error;
                return false;
            }

            point = new CameraPathResolvedPoint(
                label,
                positionResult.Value,
                Quaternion.Euler(eulerAnglesResult.Value),
                HoldSeconds,
                transitionFromPrevious,
                lens,
                onArriveAction);
            return true;
        }
    }

    public readonly struct CameraPathResolvedPoint
    {
        public CameraPathResolvedPoint(
            string label,
            Vector3 position,
            Quaternion rotation,
            float holdSeconds,
            CameraPathTransitionSettings transitionFromPrevious,
            CameraPathLensSettings lens,
            InlineAction onArriveAction)
        {
            Label = label;
            Position = position;
            Rotation = rotation;
            HoldSeconds = Mathf.Max(0.0f, holdSeconds);
            TransitionFromPrevious = transitionFromPrevious;
            Lens = lens;
            OnArriveAction = onArriveAction;
        }

        public string Label { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float HoldSeconds { get; }
        public CameraPathTransitionSettings TransitionFromPrevious { get; }
        public CameraPathLensSettings Lens { get; }
        public InlineAction OnArriveAction { get; }
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
        private readonly CameraPathResolvedPoint[] points;

        public CameraPathSequenceDefinition(IReadOnlyList<CameraPathResolvedPoint> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            this.points = new CameraPathResolvedPoint[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                this.points[i] = points[i];
            }
        }

        public IReadOnlyList<CameraPathResolvedPoint> Points => points;
        public int Count => points.Length;

        public static bool TryCreate(
            IReadOnlyList<CameraPathPointDefinition> pointDefinitions,
            SceneKernel sceneKernel,
            EntityRef actor,
            out CameraPathSequenceDefinition sequence)
        {
            sequence = null;

            if (pointDefinitions == null)
            {
                Debug.LogError($"{nameof(CameraPathSequenceDefinition)}: camera path definitions are null.");
                return false;
            }

            if (sceneKernel?.ReactiveValues == null)
            {
                Debug.LogError($"{nameof(CameraPathSequenceDefinition)}: reactive value resolver is not available.");
                return false;
            }

            ReactiveEvalContext context = new(sceneKernel, actor, default);
            CameraPathResolvedPoint[] resolvedPoints = new CameraPathResolvedPoint[pointDefinitions.Count];

            for (int i = 0; i < pointDefinitions.Count; i++)
            {
                CameraPathPointDefinition pointDefinition = pointDefinitions[i];

                if (!pointDefinition.TryResolve(context, sceneKernel.ReactiveValues, out resolvedPoints[i], out ReactiveError error))
                {
                    string pointLabel = string.IsNullOrWhiteSpace(pointDefinition.Label) ? $"Point {i + 1}" : pointDefinition.Label;
                    Debug.LogError($"{nameof(CameraPathSequenceDefinition)}: failed to resolve {pointLabel}. {error.Message}");
                    return false;
                }
            }

            sequence = new CameraPathSequenceDefinition(resolvedPoints);
            return true;
        }
    }
}