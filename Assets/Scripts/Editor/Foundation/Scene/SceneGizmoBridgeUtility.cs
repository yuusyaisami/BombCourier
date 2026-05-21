using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.Scene
{
    public static class SceneGizmoBridgeUtility
    {
        public static void Draw<TTarget>(TTarget target, GizmoType gizmoType, GizmoAdapterBase<TTarget> adapter)
            where TTarget : Object
        {
            if (target == null || adapter == null)
                return;

            bool selected = (gizmoType & (GizmoType.Selected | GizmoType.Active)) != 0;
            adapter.Draw(target, selected);
        }
    }
}