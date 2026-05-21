using BC.Editor.Foundation.Scene;
using BC.UI;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.UI
{
    internal sealed class UIFallEffectGizmoAdapter : GizmoAdapterBase<UIFallEffectMB>
    {
        private static readonly Color SpawnAreaColor = new(1.0f, 0.0f, 0.0f, 0.5f);
        private const float Depth = 0.01f;

        protected override void DrawGizmos(UIFallEffectMB target, bool selected)
        {
            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.color = SpawnAreaColor;

            RectTransform rectTransform = target.transform as RectTransform;
            if (rectTransform != null)
            {
                Transform parentTransform = rectTransform.parent;
                Gizmos.matrix = parentTransform != null ? parentTransform.localToWorldMatrix : Matrix4x4.identity;

                Vector2 center = rectTransform.anchoredPosition + target.SpawnAreaOffset;
                Vector3 size = new(target.SpawnAreaSize.x, target.SpawnAreaSize.y, Depth);
                Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0.0f), size);
            }
            else
            {
                Vector3 fallbackCenter = target.transform.position + (Vector3)target.SpawnAreaOffset;
                Vector3 fallbackSize = new(target.SpawnAreaSize.x, target.SpawnAreaSize.y, Depth);
                Gizmos.DrawWireCube(fallbackCenter, fallbackSize);
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }

    internal static class UIFallEffectGizmoDrawer
    {
        private static readonly UIFallEffectGizmoAdapter Adapter = new();

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void Draw(UIFallEffectMB target, GizmoType gizmoType)
        {
            SceneGizmoBridgeUtility.Draw(target, gizmoType, Adapter);
        }
    }
}