using System;
using System.Collections.Generic;
using BC.Rendering;
using UnityEngine;

namespace BC.Player
{
    public enum InteractionEventType
    {
        Started = 1,
        Updated = 2,
        Canceled = 3,
        Completed = 4
    }

    public readonly struct InteractionQuery
    {
        public readonly Vector3 DetectionPosition;
        public readonly Vector3 FacingPosition;
        public readonly Vector3 FacingForward;
        public readonly Vector3 PlanarFacingForward;
        public readonly float MaxDistance;
        public readonly float MaxAngle;
        public readonly Collider HitCollider;

        public InteractionQuery(
            Vector3 detectionPosition,
            Vector3 facingPosition,
            Vector3 facingForward,
            Vector3 planarFacingForward,
            float maxDistance,
            float maxAngle,
            Collider hitCollider)
        {
            DetectionPosition = detectionPosition;
            FacingPosition = facingPosition;
            FacingForward = facingForward;
            PlanarFacingForward = planarFacingForward;
            MaxDistance = maxDistance;
            MaxAngle = maxAngle;
            HitCollider = hitCollider;
        }
    }

    public readonly struct InteractionCandidate
    {
        public readonly IInteractionTarget Interactable;
        public readonly float Score;
        public readonly bool IsBest;

        public InteractionCandidate(IInteractionTarget interactable, float score, bool isBest)
        {
            Interactable = interactable;
            Score = score;
            IsBest = isBest;
        }
    }

    public readonly struct InteractionEventData
    {
        public readonly IInteractionSource Source;
        public readonly IInteractionTarget Interactable;
        public readonly InteractionEventType EventType;
        public readonly float HoldDuration;
        public readonly float HoldProgress;

        public InteractionEventData(
            IInteractionSource source,
            IInteractionTarget interactable,
            InteractionEventType eventType,
            float holdDuration,
            float holdProgress)
        {
            Source = source;
            Interactable = interactable;
            EventType = eventType;
            HoldDuration = holdDuration;
            HoldProgress = holdProgress;
        }
    }

    public interface IInteractionSource
    {
        bool IsInputPressed { get; }
        float InputHoldDuration { get; }
        int InputPressSequence { get; }
        int InputReleaseSequence { get; }
        bool HasCandidate { get; }
        IInteractionTarget CurrentBestInteractable { get; }
        IInteractionTarget ActiveInteractable { get; }
        float ActiveHoldProgress { get; }
        IReadOnlyList<InteractionCandidate> Candidates { get; }

        event Action<InteractionEventData> InteractionEvent;
    }

    public interface IInteractionTarget
    {
        Transform InteractionTransform { get; }
        float RequiredHoldDuration { get; }
        InteractionVisualTargetMB VisualTarget { get; }

        bool TryGetCandidateScore(InteractionQuery query, out float score);
        void OnInteractionStarted(InteractionEventData eventData);
        void OnInteractionUpdated(InteractionEventData eventData);
        void OnInteractionCanceled(InteractionEventData eventData);
        void OnInteractionCompleted(InteractionEventData eventData);
    }
}
