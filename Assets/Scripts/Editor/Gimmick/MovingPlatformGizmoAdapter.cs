using BC.Editor.Foundation.Scene;
using BC.Gimmick.MovingPlatform;
using UnityEditor;

namespace BC.Editor.Gimmick
{
    internal sealed class MovingPlatformGizmoAdapter : GizmoAdapterBase<MovingPlatformMB>
    {
        private readonly MovingPlatformTools.MovingPlatformGizmoPresenter presenter = new();

        protected override void DrawGizmos(MovingPlatformMB target, bool selected)
        {
            presenter.Draw(target);
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