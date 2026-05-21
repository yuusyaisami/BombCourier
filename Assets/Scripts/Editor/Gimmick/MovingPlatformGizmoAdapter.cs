using BC.Editor.Foundation.Scene;
using BC.Gimmick.MovingPlatform;
using UnityEditor;

namespace BC.Editor.Gimmick
{
    internal sealed class MovingPlatformGizmoAdapter : GizmoAdapterBase<MovingPlatformMB>
    {
        protected override void DrawGizmos(MovingPlatformMB target, bool selected)
        {
            target.DrawEditorPathGizmos();
        }
    }

    internal static class MovingPlatformGizmoDrawer
    {
        private static readonly MovingPlatformGizmoAdapter Adapter = new();

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active)]
        private static void Draw(MovingPlatformMB target, GizmoType gizmoType)
        {
            SceneGizmoBridgeUtility.Draw(target, gizmoType, Adapter);
        }
    }
}