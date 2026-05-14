using BC.Item;
using BC.Rendering;
using UnityEngine;

namespace BC.Player
{
    public sealed class CarryableItemInteractableAdapter : IPlayerInteractable
    {
        private readonly MonoBehaviour owner;
        private readonly ICarryableItem carryableItem;
        private PickupOutlineTargetMB outlineTarget;

        public CarryableItemInteractableAdapter(MonoBehaviour owner, ICarryableItem carryableItem)
        {
            this.owner = owner;
            this.carryableItem = carryableItem;
        }

        public Object KeyObject => owner;
        public ICarryableItem CarryableItem => carryableItem;
        public Transform InteractionTransform => carryableItem != null ? carryableItem.ItemTransform : null;
        public float RequiredHoldDuration => 0f;
        public PickupOutlineTargetMB OutlineTarget
        {
            get
            {
                if (outlineTarget == null && owner != null)
                    outlineTarget = owner.GetComponentInParent<PickupOutlineTargetMB>();

                return outlineTarget;
            }
        }

        public bool TryGetCandidateScore(PlayerInteractionQuery query, out float score)
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

            Vector3 toItem = itemTransform.position - query.FacingPosition;
            toItem.y = 0f;

            float sqrDistance = toItem.sqrMagnitude;

            if (sqrDistance <= 0.0001f)
                return false;

            Vector3 facingForward = query.PlanarFacingForward;

            if (facingForward.sqrMagnitude <= 0.0001f)
                return false;

            Vector3 directionToItem = toItem.normalized;
            float angle = Vector3.Angle(facingForward, directionToItem);

            if (angle > query.MaxAngle)
                return false;

            float maxDistance = Mathf.Max(0.01f, query.MaxDistance);

            if (sqrDistance > maxDistance * maxDistance)
                return false;

            score = sqrDistance + angle * 0.05f;
            return true;
        }

        public void OnInteractionStarted(PlayerInteractionEventData eventData)
        {
        }

        public void OnInteractionUpdated(PlayerInteractionEventData eventData)
        {
        }

        public void OnInteractionCanceled(PlayerInteractionEventData eventData)
        {
        }

        public void OnInteractionCompleted(PlayerInteractionEventData eventData)
        {
        }
    }
}
