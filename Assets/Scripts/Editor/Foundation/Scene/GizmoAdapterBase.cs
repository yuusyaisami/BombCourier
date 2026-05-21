using UnityEngine;

namespace BC.Editor.Foundation.Scene
{
    public abstract class GizmoAdapterBase<TTarget>
        where TTarget : Object
    {
        public void Draw(TTarget target, bool selected)
        {
            if (target == null)
                return;

            DrawGizmos(target, selected);
        }

        protected abstract void DrawGizmos(TTarget target, bool selected);
    }
}
