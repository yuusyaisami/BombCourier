using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.Scene
{
    public abstract class SceneToolEditorBase<TTarget> : UnityEditor.Editor
        where TTarget : Object
    {
        protected TTarget TypedTarget => target as TTarget;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            DrawInspectorGUI();
            UndoApplyUtility.ApplyModifiedProperties(serializedObject);
        }

        protected virtual void DrawInspectorGUI()
        {
            DrawDefaultInspector();
        }

        protected virtual void OnSceneGUI()
        {
            TTarget typedTarget = TypedTarget;

            if (typedTarget == null)
                return;

            serializedObject.UpdateIfRequiredOrScript();
            DrawSceneGUI(typedTarget);
            UndoApplyUtility.ApplyModifiedProperties(serializedObject);
        }

        protected abstract void DrawSceneGUI(TTarget target);

        protected void RepaintSceneView()
        {
            SceneView.RepaintAll();
            Repaint();
        }
    }
}
