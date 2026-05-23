using BC.Character;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Character
{
    [CustomEditor(typeof(FaceExpressionUvSet), true)]
    public sealed class FaceExpressionUvSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open UV Window", GUILayout.Height(24f), GUILayout.Width(180f)))
                {
                    FaceExpressionUvSetWindow.OpenForAsset((FaceExpressionUvSet)target);
                }
            }
        }
    }
}
