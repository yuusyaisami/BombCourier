using BC.Editor.Foundation.Scene;
using BC.Player;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Player
{
    internal sealed class PlayerItemHandleStateGizmoAdapter : GizmoAdapterBase<PlayerItemHandleStateMB>
    {
        protected override void DrawGizmos(PlayerItemHandleStateMB target, bool selected)
        {
            if (target.HandleItemPoint == null)
                return;

            Color previousColor = Gizmos.color;
            Gizmos.color = SceneHandleStyleTokens.LineColor;
            Gizmos.DrawWireSphere(target.HandleItemPoint.position, target.HandleItemDistance);
            Gizmos.color = previousColor;
        }
    }

    internal static class PlayerItemHandleStateGizmoDrawer
    {
        private static readonly PlayerItemHandleStateGizmoAdapter Adapter = new();

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void Draw(PlayerItemHandleStateMB target, GizmoType gizmoType)
        {
            SceneGizmoBridgeUtility.Draw(target, gizmoType, Adapter);
        }
    }
}