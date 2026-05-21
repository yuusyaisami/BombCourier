using System;
using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.IMGUI
{
    public sealed class InlineListController
    {
        private readonly Func<SerializedProperty, int, float> getRowHeight;
        private readonly Action<Rect, SerializedProperty, int> drawRow;

        public InlineListController(
            Func<SerializedProperty, int, float> getRowHeight,
            Action<Rect, SerializedProperty, int> drawRow)
        {
            this.getRowHeight = getRowHeight ?? throw new ArgumentNullException(nameof(getRowHeight));
            this.drawRow = drawRow ?? throw new ArgumentNullException(nameof(drawRow));
        }

        public float GetHeight(SerializedProperty listProperty)
        {
            if (listProperty == null || !listProperty.isArray || listProperty.arraySize == 0)
                return EditorThemeTokens.LineHeight;

            float height = 0f;

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                height += getRowHeight(listProperty.GetArrayElementAtIndex(i), i);

                if (i < listProperty.arraySize - 1)
                    height += EditorThemeTokens.RowSpacing;
            }

            return Mathf.Max(EditorThemeTokens.LineHeight, height);
        }

        public void Draw(Rect position, SerializedProperty listProperty, GUIContent emptyLabel = null)
        {
            if (listProperty == null || !listProperty.isArray)
            {
                EditorGUI.HelpBox(position, "List property is missing.", MessageType.Error);
                return;
            }

            if (listProperty.arraySize == 0)
            {
                EditorGUI.LabelField(position, emptyLabel ?? new GUIContent("Empty"));
                return;
            }

            Rect cursor = position;

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty rowProperty = listProperty.GetArrayElementAtIndex(i);
                float rowHeight = getRowHeight(rowProperty, i);
                Rect rowRect = RectLayoutUtility.TakeHeight(ref cursor, rowHeight);
                drawRow(rowRect, rowProperty, i);
            }
        }
    }
}
