using UnityEngine;

namespace BC.Rendering
{
    [DisallowMultipleComponent]
    public sealed class OutlineTargetMB : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;

        public Renderer[] Renderers => renderers;

        private void Reset()
        {
            CacheRenderers();
        }

        private void OnValidate()
        {
            if (renderers == null || renderers.Length == 0)
            {
                CacheRenderers();
            }
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
    }
}