using System.Collections.Generic;
using UnityEngine;

namespace BC.Rendering
{
    [DisallowMultipleComponent]
    public sealed class OutlineServiceMB : MonoBehaviour
    {
        private readonly HashSet<OutlineTargetMB> targets = new();
        private readonly List<Renderer> rendererBuffer = new();

        public void SetSingle(OutlineTargetMB target)
        {
            targets.Clear();

            if (target != null)
            {
                targets.Add(target);
            }
        }

        public void Clear()
        {
            targets.Clear();
        }

        public int CopyRenderers(List<Renderer> destination)
        {
            destination.Clear();

            foreach (OutlineTargetMB target in targets)
            {
                if (target == null)
                    continue;

                Renderer[] renderers = target.Renderers;
                if (renderers == null)
                    continue;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];

                    if (renderer == null)
                        continue;

                    if (!renderer.enabled)
                        continue;

                    destination.Add(renderer);
                }
            }

            return destination.Count;
        }
    }
}