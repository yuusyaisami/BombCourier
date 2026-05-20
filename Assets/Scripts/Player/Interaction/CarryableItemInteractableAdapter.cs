using BC.Item;
using BC.Rendering;
using UnityEngine;

namespace BC.Player
{
    public sealed class CarryableItemInteractableAdapter : IInteractionTarget, IInteractionPromptProvider, IInteractionPromptDetailTextProvider
    {
        private readonly MonoBehaviour owner;
        private readonly ICarryableItem carryableItem;
        private InteractionVisualTargetMB visualTarget;
        private IInteractionPromptDetailTextProvider promptDetailTextProvider;
        private bool promptDetailTextProviderResolved;

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
        public string PromptDetailText => ResolvePromptDetailText();
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

        private string ResolvePromptDetailText()
        {
            IInteractionPromptDetailTextProvider provider = ResolvePromptDetailTextProvider();
            return provider != null ? provider.PromptDetailText ?? string.Empty : string.Empty;
        }

        private IInteractionPromptDetailTextProvider ResolvePromptDetailTextProvider()
        {
            if (promptDetailTextProviderResolved)
                return promptDetailTextProvider;

            promptDetailTextProviderResolved = true;

            if (carryableItem is IInteractionPromptDetailTextProvider carryableProvider)
            {
                promptDetailTextProvider = carryableProvider;
                return promptDetailTextProvider;
            }

            if (owner == null)
                return null;

            MonoBehaviour[] ownerBehaviours = owner.GetComponents<MonoBehaviour>();
            for (int i = 0; i < ownerBehaviours.Length; i++)
            {
                if (ownerBehaviours[i] is IInteractionPromptDetailTextProvider provider)
                {
                    promptDetailTextProvider = provider;
                    break;
                }
            }

            return promptDetailTextProvider;
        }
    }
}
