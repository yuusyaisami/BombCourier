using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.IMGUI
{
    public static class RectLayoutUtility
    {
        public static Rect TakeLine(ref Rect rect)
        {
            return TakeHeight(ref rect, EditorThemeTokens.LineHeight);
        }

        public static Rect TakeHeight(ref Rect rect, float height)
        {
            Rect result = new(rect.x, rect.y, rect.width, Mathf.Max(0f, height));
            rect.y += result.height + EditorThemeTokens.StandardSpacing;
            rect.height = Mathf.Max(0f, rect.height - result.height - EditorThemeTokens.StandardSpacing);
            return result;
        }

        public static Rect TakeSpacing(ref Rect rect, float spacing)
        {
            Rect result = new(rect.x, rect.y, rect.width, Mathf.Max(0f, spacing));
            rect.y += result.height;
            rect.height = Mathf.Max(0f, rect.height - result.height);
            return result;
        }

        public static Rect PrefixLabel(Rect rect, GUIContent label)
        {
            return EditorGUI.PrefixLabel(rect, label ?? GUIContent.none);
        }

        public static Rect Indented(Rect rect)
        {
            return EditorGUI.IndentedRect(rect);
        }

        public static Rect WithPadding(Rect rect, float padding)
        {
            return new Rect(
                rect.x + padding,
                rect.y + padding,
                Mathf.Max(0f, rect.width - padding * 2f),
                Mathf.Max(0f, rect.height - padding * 2f));
        }

        public static Rect TakeLeft(ref Rect rect, float width, float spacing = 0f)
        {
            float resolvedWidth = Mathf.Clamp(width, 0f, rect.width);
            Rect result = new(rect.x, rect.y, resolvedWidth, rect.height);
            rect.x += resolvedWidth + spacing;
            rect.width = Mathf.Max(0f, rect.width - resolvedWidth - spacing);
            return result;
        }

        public static Rect TakeRight(ref Rect rect, float width, float spacing = 0f)
        {
            float resolvedWidth = Mathf.Clamp(width, 0f, rect.width);
            Rect result = new(rect.xMax - resolvedWidth, rect.y, resolvedWidth, rect.height);
            rect.width = Mathf.Max(0f, rect.width - resolvedWidth - spacing);
            return result;
        }

        public static float ControlDelta(float controlHeight)
        {
            return controlHeight <= 0f ? 0f : controlHeight + EditorThemeTokens.StandardSpacing;
        }
    }
}
