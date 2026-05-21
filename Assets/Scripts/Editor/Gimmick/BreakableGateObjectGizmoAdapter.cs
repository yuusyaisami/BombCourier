using BC.Editor.Foundation.Scene;
using BC.Gimmick;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Gimmick
{
    internal sealed class BreakableGateObjectGizmoAdapter : GizmoAdapterBase<BreakableGateObjectMB>
    {
        private static readonly Color BreakDirectionColor = Color.red;
        private static readonly Color GoalTargetColor = Color.green;
        private const float BreakDirectionLength = 2.0f;
        private const float MarkerRadius = 0.2f;

        protected override void DrawGizmos(BreakableGateObjectMB target, bool selected)
        {
            Color previousColor = Gizmos.color;

            if (target.BreakForceOrigin != null)
            {
                Gizmos.color = BreakDirectionColor;
                Gizmos.DrawLine(
                    target.BreakForceOrigin.position,
                    target.BreakForceOrigin.position + (target.BreakForceDirection.normalized * BreakDirectionLength));
                Gizmos.DrawWireSphere(target.BreakForceOrigin.position, MarkerRadius);
            }

            if (target.IsGoalGate && target.GoalData != null)
            {
                Gizmos.color = GoalTargetColor;
                Gizmos.DrawWireSphere(target.TargetPoint, MarkerRadius);
            }

            Gizmos.color = previousColor;
        }
    }

    internal static class BreakableGateObjectGizmoDrawer
    {
        private static readonly BreakableGateObjectGizmoAdapter Adapter = new();

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void Draw(BreakableGateObjectMB target, GizmoType gizmoType)
        {
            SceneGizmoBridgeUtility.Draw(target, gizmoType, Adapter);
        }
    }
}