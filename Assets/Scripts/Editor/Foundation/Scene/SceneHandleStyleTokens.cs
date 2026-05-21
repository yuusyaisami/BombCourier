using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.Scene
{
    public static class SceneHandleStyleTokens
    {
        public static Color LineColor =>
            EditorGUIUtility.isProSkin ? new Color(0.55f, 0.78f, 1.0f, 0.9f) : new Color(0.12f, 0.36f, 0.74f, 0.9f);

        public static Color SelectedColor =>
            EditorGUIUtility.isProSkin ? new Color(1.0f, 0.82f, 0.36f, 1.0f) : new Color(0.86f, 0.48f, 0.04f, 1.0f);

        public static Color DisabledColor =>
            EditorGUIUtility.isProSkin ? new Color(0.45f, 0.45f, 0.45f, 0.55f) : new Color(0.55f, 0.55f, 0.55f, 0.55f);

        public const float HandleSizeMultiplier = 0.08f;
        public const float LabelOffset = 0.35f;
        public const float WireAlpha = 0.45f;
    }
}
