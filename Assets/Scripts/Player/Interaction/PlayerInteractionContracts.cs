using System;
using System.Collections.Generic;
using BC.Rendering;
using UnityEngine;

namespace BC.Player
{
    public enum PlayerInteractionEventType
    {
        Started = 1,
        Updated = 2,
        Canceled = 3,
        Completed = 4
    }

    public readonly struct PlayerInteractionQuery
    {
        public readonly Vector3 DetectionPosition;
        public readonly Vector3 FacingPosition;
        public readonly Vector3 FacingForward;
        public readonly Vector3 PlanarFacingForward;
        public readonly float MaxDistance;
        public readonly float MaxAngle;
        public readonly Collider HitCollider;

        public PlayerInteractionQuery(
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

    public readonly struct PlayerInteractionCandidate
    {
        public readonly IPlayerInteractable Interactable;
        public readonly float Score;
        public readonly bool IsBest;

        public PlayerInteractionCandidate(IPlayerInteractable interactable, float score, bool isBest)
        {
            Interactable = interactable;
            Score = score;
            IsBest = isBest;
        }
    }

    public readonly struct PlayerInteractionEventData
    {
        public readonly IPlayerInteractionSource Source;
        public readonly IPlayerInteractable Interactable;
        public readonly PlayerInteractionEventType EventType;
        public readonly float HoldDuration;
        public readonly float HoldProgress;

        public PlayerInteractionEventData(
            IPlayerInteractionSource source,
            IPlayerInteractable interactable,
            PlayerInteractionEventType eventType,
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

    public interface IPlayerInteractionSource
    {
        bool IsInputPressed { get; }
        float InputHoldDuration { get; }
        int InputPressSequence { get; }
        int InputReleaseSequence { get; }
        bool HasCandidate { get; }
        IPlayerInteractable CurrentBestInteractable { get; }
        IPlayerInteractable ActiveInteractable { get; }
        float ActiveHoldProgress { get; }
        IReadOnlyList<PlayerInteractionCandidate> Candidates { get; }

        event Action<PlayerInteractionEventData> InteractionEvent;
    }

    public interface IPlayerInteractable
    {
        Transform InteractionTransform { get; }
        float RequiredHoldDuration { get; }
        PickupOutlineTargetMB OutlineTarget { get; }

        bool TryGetCandidateScore(PlayerInteractionQuery query, out float score);
        void OnInteractionStarted(PlayerInteractionEventData eventData);
        void OnInteractionUpdated(PlayerInteractionEventData eventData);
        void OnInteractionCanceled(PlayerInteractionEventData eventData);
        void OnInteractionCompleted(PlayerInteractionEventData eventData);
    }
}
