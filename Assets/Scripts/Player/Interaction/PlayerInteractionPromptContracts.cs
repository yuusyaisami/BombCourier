using UnityEngine;

namespace BC.Player
{
    public interface IInteractionPromptProvider
    {
        Transform PromptAnchor { get; }
        Vector3 PromptWorldOffset { get; }
    }

    public interface IInteractionPromptDetailTextProvider
    {
        string PromptDetailText { get; }
    }

    public static class InteractionPromptResolver
    {
        private static readonly Vector3 DefaultWorldOffset = new(0f, 0.1f, 0f);

        public static Transform ResolveAnchor(IInteractionTarget interactable)
        {
            if (interactable == null)
                return null;

            if (interactable is IInteractionPromptProvider provider &&
                provider.PromptAnchor != null)
            {
                return provider.PromptAnchor;
            }

            if (interactable.InteractionTransform != null)
                return interactable.InteractionTransform;

            if (interactable.VisualTarget != null)
                return interactable.VisualTarget.transform;

            return null;
        }

        public static bool TryResolveWorldPosition(IInteractionTarget interactable, out Vector3 worldPosition)
        {
            worldPosition = default;

            if (interactable == null)
                return false;

            if (interactable is IInteractionPromptProvider provider &&
                provider.PromptAnchor != null)
            {
                worldPosition = provider.PromptAnchor.position + provider.PromptWorldOffset;
                return true;
            }

            if (interactable.VisualTarget != null &&
                interactable.VisualTarget.TryGetWorldBounds(out Bounds bounds))
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

        public static string ResolveDetailText(IInteractionTarget interactable)
        {
            if (interactable is not IInteractionPromptDetailTextProvider provider)
                return string.Empty;

            return provider.PromptDetailText ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class InteractionPromptDetailTextMB : MonoBehaviour, IInteractionPromptDetailTextProvider
    {
        [SerializeField, TextArea] private string promptDetailText = string.Empty;

        public string PromptDetailText => promptDetailText ?? string.Empty;
    }
}