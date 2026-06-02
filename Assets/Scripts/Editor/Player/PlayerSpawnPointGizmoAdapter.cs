using BC.Bomb;
using BC.Editor.Foundation.Scene;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Player
{
    // PlayerSpawnPointMB のスポーン向きを SceneView で可視化する Gizmo。
    // スポーン位置を球、向き（ローカル→ワールド変換後）を矢印で表示する。
    internal sealed class PlayerSpawnPointGizmoAdapter : GizmoAdapterBase<PlayerSpawnPointMB>
    {
        private const float SphereRadius = 0.25f;
        private const float ArrowLength = 1.5f;
        private const float ArrowHeadLength = 0.35f;
        private const float ArrowHeadRadius = 0.12f;

        protected override void DrawGizmos(PlayerSpawnPointMB target, bool selected)
        {
            Vector3 origin = target.transform.position;
            Vector3 dir = target.GetWorldSpawnDirection();

            Color previousColor = Gizmos.color;
            Gizmos.color = selected ? SceneHandleStyleTokens.SelectedColor : SceneHandleStyleTokens.LineColor;

            // スポーン位置の球
            Gizmos.DrawWireSphere(origin, SphereRadius);

            // 矢印の軸線
            Vector3 tip = origin + dir * ArrowLength;
            Gizmos.DrawLine(origin, tip);

            // 矢印の先端（4 方向のフィン）
            Vector3 arrowBase = tip - dir * ArrowHeadLength;

            // dir に垂直な 2 方向を求める（ジンバルロック回避のため up, forward 両方を試す）
            Vector3 right = Vector3.Cross(dir, Vector3.up);
            if (right.sqrMagnitude <= 0.0001f)
                right = Vector3.Cross(dir, Vector3.forward);
            right.Normalize();
            Vector3 up = Vector3.Cross(right, dir).normalized;

            Gizmos.DrawLine(arrowBase + right * ArrowHeadRadius, tip);
            Gizmos.DrawLine(arrowBase - right * ArrowHeadRadius, tip);
            Gizmos.DrawLine(arrowBase + up * ArrowHeadRadius, tip);
            Gizmos.DrawLine(arrowBase - up * ArrowHeadRadius, tip);

            Gizmos.color = previousColor;
        }
    }

    internal static class PlayerSpawnPointGizmoDrawer
    {
        private static readonly PlayerSpawnPointGizmoAdapter Adapter = new();

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active)]
        private static void Draw(PlayerSpawnPointMB target, GizmoType gizmoType)
        {
            SceneGizmoBridgeUtility.Draw(target, gizmoType, Adapter);
        }
    }
}
