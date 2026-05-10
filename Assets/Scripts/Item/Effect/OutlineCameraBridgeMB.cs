using System.Collections.Generic;
using UnityEngine;

namespace BC.Rendering
{
    [DisallowMultipleComponent]
    public sealed class OutlineCameraBridgeMB : MonoBehaviour
    {
        [SerializeField] private OutlineServiceMB outlineService;

        public int CopyRenderers(List<Renderer> destination)
        {
            if (outlineService == null)
            {
                destination.Clear();
                return 0;
            }

            return outlineService.CopyRenderers(destination);
        }
    }
}