using UnityEngine;

namespace BC.Player
{
    public interface IPlayerInteractionPromptProvider
    {
        Transform PromptAnchor { get; }
        Vector3 PromptWorldOffset { get; }
    }

    public static class PlayerInteractionPromptResolver
    {
        private static readonly Vector3 DefaultWorldOffset = new(0f, 0.1f, 0f);

        public static Transform ResolveAnchor(IPlayerInteractable interactable)
        {
            if (interactable == null)
                return null;

            if (interactable is IPlayerInteractionPromptProvider provider &&
                provider.PromptAnchor != null)
            {
                return provider.PromptAnchor;
            }

            if (interactable.InteractionTransform != null)
                return interactable.InteractionTransform;

            if (interactable.OutlineTarget != null)
                return interactable.OutlineTarget.transform;

            return null;
        }

        public static bool TryResolveWorldPosition(IPlayerInteractable interactable, out Vector3 worldPosition)
        {
            worldPosition = default;

            if (interactable == null)
                return false;

            if (interactable is IPlayerInteractionPromptProvider provider &&
                provider.PromptAnchor != null)
            {
                worldPosition = provider.PromptAnchor.position + provider.PromptWorldOffset;
                return true;
            }

            if (interactable.OutlineTarget != null &&
                interactable.OutlineTarget.TryGetWorldBounds(out Bounds bounds))
            {
                worldPosition = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + DefaultWorldOffset;
                return true;
            }

            Transform anchor = ResolveAnchor(interactable);
            if (anchor == null)
                return false;

            worldPosition = anchor.position + DefaultWorldOffset;
            return true;
        }
    }
}