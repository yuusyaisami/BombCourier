using System;
using System.Collections.Generic;
using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.IMGUI
{
    public sealed class InlineListController
    {
        private readonly Func<SerializedProperty, int, float> getRowHeight;
        private readonly Action<Rect, SerializedProperty, int> drawRow;
        private readonly Action<SerializedProperty, int, int> moveRow;
        private readonly float dragHandleWidth;

        private static DragSession dragSession;

        public InlineListController(
            Func<SerializedProperty, int, float> getRowHeight,
            Action<Rect, SerializedProperty, int> drawRow,
            Action<SerializedProperty, int, int> moveRow = null,
            float dragHandleWidth = 0f)
        {
            this.getRowHeight = getRowHeight ?? throw new ArgumentNullException(nameof(getRowHeight));
            this.drawRow = drawRow ?? throw new ArgumentNullException(nameof(drawRow));
            this.moveRow = moveRow;
            this.dragHandleWidth = Mathf.Max(0f, dragHandleWidth);
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
                    height += EditorThemeTokens.StandardSpacing;
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

            RowGeometry[] rows = BuildRowGeometry(position, listProperty);
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            string listStateKey = BuildListStateKey(listProperty);

            TryBeginDrag(listStateKey, rows, controlId);

            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty rowProperty = listProperty.GetArrayElementAtIndex(i);

                if (dragHandleWidth > 0f && moveRow != null)
                    EditorGUIUtility.AddCursorRect(rows[i].HandleRect, MouseCursor.Pan);

                drawRow(rows[i].RowRect, rowProperty, i);
            }

            HandleDrag(listProperty, listStateKey, rows, controlId);
        }

        private RowGeometry[] BuildRowGeometry(Rect position, SerializedProperty listProperty)
        {
            List<RowGeometry> rows = new(listProperty.arraySize);
            Rect cursor = position;

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty rowProperty = listProperty.GetArrayElementAtIndex(i);
                float rowHeight = getRowHeight(rowProperty, i);
                Rect rowRect = RectLayoutUtility.TakeHeight(ref cursor, rowHeight);
                Rect headerRect = new(rowRect.x, rowRect.y, rowRect.width, EditorThemeTokens.LineHeight);
                Rect handleRect = new(
                    headerRect.x,
                    headerRect.y,
                    Mathf.Min(dragHandleWidth, headerRect.width),
                    headerRect.height);

                rows.Add(new RowGeometry(i, rowRect, headerRect, handleRect));
            }

            return rows.ToArray();
        }

        private void TryBeginDrag(string listStateKey, IReadOnlyList<RowGeometry> rows, int controlId)
        {
            if (moveRow == null || dragHandleWidth <= 0f)
                return;

            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
                return;

            for (int i = 0; i < rows.Count; i++)
            {
                if (!rows[i].HandleRect.Contains(currentEvent.mousePosition))
                    continue;

                dragSession = new DragSession(listStateKey, rows[i].Index, rows[i].Index, currentEvent.mousePosition);
                GUIUtility.hotControl = controlId;
                currentEvent.Use();
                return;
            }
        }

        private void HandleDrag(SerializedProperty listProperty, string listStateKey, IReadOnlyList<RowGeometry> rows, int controlId)
        {
            if (!dragSession.IsActive || moveRow == null || dragHandleWidth <= 0f)
                return;

            Event currentEvent = Event.current;

            if (!string.Equals(dragSession.ListStateKey, listStateKey, StringComparison.Ordinal))
                return;

            if (currentEvent.type == EventType.Repaint || currentEvent.type == EventType.MouseDrag)
            {
                dragSession.UpdateInsertIndex(ResolveInsertIndex(rows, currentEvent.mousePosition));
                dragSession.LastMousePosition = currentEvent.mousePosition;
            }

            if (currentEvent.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
            {
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                int destinationIndex = ResolveDestinationIndex(rows.Count, dragSession.SourceIndex, dragSession.InsertIndex);

                if (destinationIndex >= 0 && destinationIndex != dragSession.SourceIndex)
                {
                    moveRow(listProperty, dragSession.SourceIndex, destinationIndex);
                    GUI.changed = true;
                }

                dragSession = default;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.Repaint && dragSession.IsActive)
                DrawInsertionMarker(rows, dragSession.InsertIndex);
        }

        private static string BuildListStateKey(SerializedProperty listProperty)
        {
            return EditorStateKey.ForSerializedObject(listProperty.serializedObject, listProperty.propertyPath, "inline-list");
        }

        private static int ResolveInsertIndex(IReadOnlyList<RowGeometry> rows, Vector2 mousePosition)
        {
            if (rows == null || rows.Count == 0)
                return 0;

            for (int i = 0; i < rows.Count; i++)
            {
                if (mousePosition.y < rows[i].HeaderRect.center.y)
                    return i;
            }

            return rows.Count;
        }

        private static int ResolveDestinationIndex(int count, int sourceIndex, int insertIndex)
        {
            if (count <= 0 || sourceIndex < 0 || sourceIndex >= count)
                return -1;

            int clampedInsertIndex = Mathf.Clamp(insertIndex, 0, count);
            int destinationIndex = clampedInsertIndex > sourceIndex
                ? clampedInsertIndex - 1
                : clampedInsertIndex;

            return Mathf.Clamp(destinationIndex, 0, count - 1);
        }

        private static void DrawInsertionMarker(IReadOnlyList<RowGeometry> rows, int insertIndex)
        {
            if (rows == null || rows.Count == 0)
                return;

            int clampedInsertIndex = Mathf.Clamp(insertIndex, 0, rows.Count);
            float y = clampedInsertIndex >= rows.Count
                ? rows[rows.Count - 1].HeaderRect.yMax
                : rows[clampedInsertIndex].HeaderRect.y;

            Rect markerRect = new(rows[0].HeaderRect.x, y - 1f, rows[0].HeaderRect.width, 2f);
            EditorGUI.DrawRect(markerRect, EditorThemeTokens.WarningColor);
        }

        private readonly struct RowGeometry
        {
            public RowGeometry(int index, Rect rowRect, Rect headerRect, Rect handleRect)
            {
                Index = index;
                RowRect = rowRect;
                HeaderRect = headerRect;
                HandleRect = handleRect;
            }

            public int Index { get; }
            public Rect RowRect { get; }
            public Rect HeaderRect { get; }
            public Rect HandleRect { get; }
        }

        private struct DragSession
        {
            public DragSession(string listStateKey, int sourceIndex, int insertIndex, Vector2 lastMousePosition)
            {
                ListStateKey = listStateKey;
                SourceIndex = sourceIndex;
                InsertIndex = insertIndex;
                LastMousePosition = lastMousePosition;
            }

            public string ListStateKey { get; }
            public int SourceIndex { get; }
            public int InsertIndex { get; private set; }
            public Vector2 LastMousePosition { get; set; }
            public bool IsActive => !string.IsNullOrWhiteSpace(ListStateKey);

            public void UpdateInsertIndex(int insertIndex)
            {
                InsertIndex = insertIndex;
            }
        }
    }
}
