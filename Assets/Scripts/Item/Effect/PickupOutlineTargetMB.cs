using UnityEngine;

namespace BC.Rendering
{
    [DisallowMultipleComponent]
    public sealed class PickupOutlineTargetMB : MonoBehaviour
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
            ClearOutline();
        }

        public void SetOutline(PickupOutlineKind kind)
        {
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                PickupOutlineRegistry.Set(renderer, kind);
            }
        }

        public void ClearOutline()
        {
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                PickupOutlineRegistry.Remove(renderer);
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