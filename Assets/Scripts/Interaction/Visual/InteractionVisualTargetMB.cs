using UnityEngine;

namespace BC.Rendering
{
    [DisallowMultipleComponent]
    public sealed class InteractionVisualTargetMB : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;

        private void Reset()
        {
            CollectRenderers();
        }

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                CollectRenderers();
            }
        }

        private void OnDisable()
        {
            ClearHighlight();
        }

        public void SetHighlight(InteractionHighlightKind kind)
        {
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                InteractionHighlightRegistry.Set(renderer, kind);
            }
        }

        public void ClearHighlight()
        {
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                InteractionHighlightRegistry.Remove(renderer);
            }
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            if (renderers == null || renderers.Length == 0)
            {
                CollectRenderers();
            }

            bounds = default;
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private void CollectRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
    }
}