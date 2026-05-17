using BC.Item;
using BC.Rendering;
using UnityEngine;

namespace BC.Player
{
    public sealed class CarryableItemInteractableAdapter : IInteractionTarget, IInteractionPromptProvider
    {
        private readonly MonoBehaviour owner;
        private readonly ICarryableItem carryableItem;
        private InteractionVisualTargetMB visualTarget;

        public CarryableItemInteractableAdapter(MonoBehaviour owner, ICarryableItem carryableItem)
        {
            this.owner = owner;
            this.carryableItem = carryableItem;
        }

        public Object OwnerObject => owner;
        public ICarryableItem CarryableItem => carryableItem;
        public Transform InteractionTransform => carryableItem != null ? carryableItem.ItemTransform : null;
        public Transform PromptAnchor => InteractionTransform;
        public Vector3 PromptWorldOffset => Vector3.up * 0.15f;
        public float RequiredHoldDuration => 0f;
        public InteractionVisualTargetMB VisualTarget
        {
            get
            {
                if (visualTarget == null && owner != null)
                    visualTarget = owner.GetComponentInParent<InteractionVisualTargetMB>();

                return visualTarget;
            }
        }

        public bool TryGetCandidateScore(InteractionQuery query, out float score)
        {
            score = float.MaxValue;

            if (carryableItem == null ||
                !carryableItem.CanBeCarried ||
                carryableItem.IsHandled)
            {
                return false;
            }

            Transform itemTransform = carryableItem.ItemTransform;

            if (itemTransform == null)
                return false;

            return InteractionScoringUtility.TryGetPlanarFacingScore(
                query,
                itemTransform.position,
                query.MaxDistance,
                query.MaxAngle,
                out score);
        }

        public void OnInteractionStarted(InteractionEventData eventData)
        {
        }

        public void OnInteractionUpdated(InteractionEventData eventData)
        {
        }

        public void OnInteractionCanceled(InteractionEventData eventData)
        {
        }

        public void OnInteractionCompleted(InteractionEventData eventData)
        {
        }
    }
}
