using System;
using BC.ActionSystem;
using BC.Editor.Foundation;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Action
{
    [CustomPropertyDrawer(typeof(InlineAction))]
    public sealed class InlineActionDrawer : PropertyDrawerBase
    {
        private const float HeaderPadding = 4f;
        private const float HandleWidth = 18f;
        private const float FoldoutWidth = 14f;
        private const float IndexWidth = 28f;
        private const float MaxStateWidth = 140f;
        private const float MaxBadgeWidth = 120f;

        private const string RenameActiveStateKey = "BC.Editor.Action.InlineActionDrawer.ActiveRename";
        private const string RenameTextStatePrefix = "BC.Editor.Action.InlineActionDrawer.RenameText.";
        private const string RenameFocusStatePrefix = "BC.Editor.Action.InlineActionDrawer.RenameFocus.";
        private const string RenameFocusedStatePrefix = "BC.Editor.Action.InlineActionDrawer.RenameFocused.";

        private static readonly GUIContent EmptyListLabel = new("Empty");
        private static readonly GUIContent AddStepButtonLabel = new("Add Step");

        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty stepsProperty = property.FindPropertyRelative("_steps");

            if (stepsProperty == null || !stepsProperty.isArray)
                return LineHeight;

            float contentHeight = CreateListController().GetHeight(stepsProperty) + Spacing + LineHeight;

            if (HasVisibleLabel(label))
                contentHeight += RectLayoutUtility.ControlDelta(LineHeight);

            return contentHeight;
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty stepsProperty = property.FindPropertyRelative("_steps");

            if (stepsProperty == null || !stepsProperty.isArray)
            {
                DrawMissingField(position, label, "InlineAction steps are missing.");
                return;
            }

            Rect cursor = position;

            if (HasVisibleLabel(label))
            {
                Rect labelRect = RectLayoutUtility.TakeLine(ref cursor);
                EditorGUI.LabelField(labelRect, label);
            }

            Rect contentRect = RectLayoutUtility.Indented(cursor);
            InlineListController controller = CreateListController();
            float listHeight = controller.GetHeight(stepsProperty);
            Rect listRect = new(contentRect.x, contentRect.y, contentRect.width, listHeight);
            controller.Draw(listRect, stepsProperty, EmptyListLabel);

            Rect footerRect = new(contentRect.x, listRect.yMax + Spacing, contentRect.width, LineHeight);
            DrawFooter(footerRect, stepsProperty);
        }

        private InlineListController CreateListController()
        {
            return new InlineListController(GetRowHeight, DrawRow);
        }

        private float GetRowHeight(SerializedProperty stepProperty, int index)
        {
            if (stepProperty == null)
                return LineHeight;

            float height = LineHeight;

            if (stepProperty.isExpanded)
            {
                float detailHeight = GetExpandedDetailHeight(stepProperty);

                if (detailHeight > 0f)
                    height += Spacing + detailHeight;
            }

            return height;
        }

        private void DrawRow(Rect rowRect, SerializedProperty stepProperty, int index)
        {
            Rect headerRect = new(rowRect.x, rowRect.y, rowRect.width, LineHeight);
            DrawHeaderBackground(headerRect, stepProperty);
            HandleContextClick(headerRect, stepProperty, index);

            string typeLabel = ActionStepSummaryUtility.GetTypeLabel(stepProperty);
            string summary = ActionStepSummaryUtility.GetSummary(stepProperty);
            string stateText = ActionStepSummaryUtility.GetStateText(stepProperty);

            Rect contentRect = new(
                headerRect.x + HeaderPadding,
                headerRect.y,
                Mathf.Max(0f, headerRect.width - (HeaderPadding * 2f)),
                headerRect.height);

            Rect handleRect = RectLayoutUtility.TakeLeft(ref contentRect, HandleWidth, 2f);
            Rect foldoutRect = RectLayoutUtility.TakeLeft(ref contentRect, FoldoutWidth, 2f);
            Rect indexRect = RectLayoutUtility.TakeLeft(ref contentRect, IndexWidth, 4f);

            float stateWidth = ResolveStateWidth(stateText);
            Rect stateRect = stateWidth > 0f
                ? RectLayoutUtility.TakeRight(ref contentRect, stateWidth, 4f)
                : Rect.zero;

            float badgeWidth = ResolveBadgeWidth(typeLabel);
            Rect badgeRect = RectLayoutUtility.TakeLeft(ref contentRect, badgeWidth, 4f);
            Rect summaryRect = contentRect;

            GUI.Label(handleRect, "||", EditorStyles.centeredGreyMiniLabel);
            stepProperty.isExpanded = EditorGUI.Foldout(foldoutRect, stepProperty.isExpanded, GUIContent.none, false);
            EditorGUI.LabelField(indexRect, (index + 1).ToString(), EditorStyles.centeredGreyMiniLabel);
            DrawBadge(badgeRect, typeLabel);

            if (IsRenameActive(stepProperty))
                DrawRenameField(summaryRect, stepProperty);
            else
                EditorGUI.LabelField(summaryRect, summary);

            if (stateWidth > 0f)
                EditorGUI.LabelField(stateRect, stateText, EditorStyles.miniLabel);

            if (!stepProperty.isExpanded)
                return;

            float detailHeight = GetExpandedDetailHeight(stepProperty);

            if (detailHeight <= 0f)
                return;

            Rect detailRect = new(
                rowRect.x + EditorThemeTokens.IndentWidth,
                headerRect.yMax + Spacing,
                Mathf.Max(0f, rowRect.width - EditorThemeTokens.IndentWidth),
                detailHeight);

            DrawExpandedDetail(detailRect, stepProperty);
        }

        private static void DrawHeaderBackground(Rect headerRect, SerializedProperty stepProperty)
        {
            Color background = stepProperty?.managedReferenceValue == null
                ? EditorThemeTokens.MissingBackground
                : EditorThemeTokens.StepBackground;

            EditorGUI.DrawRect(headerRect, background);
        }

        private static float ResolveBadgeWidth(string typeLabel)
        {
            return Mathf.Min(MaxBadgeWidth, EditorStyles.miniBoldLabel.CalcSize(new GUIContent(typeLabel)).x + 12f);
        }

        private static float ResolveStateWidth(string stateText)
        {
            if (string.IsNullOrWhiteSpace(stateText))
                return 0f;

            return Mathf.Min(MaxStateWidth, EditorStyles.miniLabel.CalcSize(new GUIContent(stateText)).x + 8f);
        }

        private static void DrawBadge(Rect badgeRect, string typeLabel)
        {
            EditorGUI.DrawRect(badgeRect, EditorThemeTokens.TypeBadgeBackground);
            EditorGUI.LabelField(RectLayoutUtility.WithPadding(badgeRect, 2f), typeLabel, EditorStyles.miniBoldLabel);
        }

        private static bool HasVisibleLabel(GUIContent label)
        {
            return label != null && !string.IsNullOrWhiteSpace(label.text);
        }

        private static float GetExpandedDetailHeight(SerializedProperty stepProperty)
        {
            if (stepProperty?.managedReferenceValue == null)
                return 0f;

            float height = 0f;
            SerializedProperty iterator = stepProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int rootDepth = iterator.depth;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;

                if (iterator.depth != rootDepth + 1)
                    continue;

                if (iterator.name == "DisplayName")
                    continue;

                height += EditorGUI.GetPropertyHeight(iterator, true);
                height += Spacing;
            }

            return height > 0f ? height - Spacing : 0f;
        }

        private static void DrawExpandedDetail(Rect detailRect, SerializedProperty stepProperty)
        {
            Rect cursor = detailRect;
            SerializedProperty iterator = stepProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int rootDepth = iterator.depth;
            bool enterChildren = true;

            // Draw direct child properties manually so DisplayName stays hidden outside inline rename mode.
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;

                if (iterator.depth != rootDepth + 1)
                    continue;

                if (iterator.name == "DisplayName")
                    continue;

                float childHeight = EditorGUI.GetPropertyHeight(iterator, true);
                Rect childRect = RectLayoutUtility.TakeHeight(ref cursor, childHeight);
                EditorGUI.PropertyField(childRect, iterator, true);
            }
        }

        private static void DrawFooter(Rect footerRect, SerializedProperty stepsProperty)
        {
            if (!GUI.Button(footerRect, AddStepButtonLabel, EditorStyles.miniButton))
                return;

            ShowAddStepMenu(footerRect, stepsProperty);
        }

        private static void ShowAddStepMenu(Rect buttonRect, SerializedProperty stepsProperty)
        {
            if (stepsProperty == null)
                return;

            GenericMenu menu = new();
            IReadOnlyList<Type> stepTypes = ActionStepManagedReferenceUtility.GetStepTypes();
            UnityEngine.Object[] targets = stepsProperty.serializedObject.targetObjects;
            string listPropertyPath = stepsProperty.propertyPath;

            for (int i = 0; i < stepTypes.Count; i++)
            {
                Type stepType = stepTypes[i];
                string label = ActionStepManagedReferenceUtility.GetStepTypeLabel(stepType);
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    ActionStepManagedReferenceUtility.AddStep(targets, listPropertyPath, stepType);
                });
            }

            menu.DropDown(buttonRect);
        }

        private static void HandleContextClick(Rect headerRect, SerializedProperty stepProperty, int index)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.ContextClick || !headerRect.Contains(currentEvent.mousePosition))
                return;

            ShowStepContextMenu(stepProperty, index);
            currentEvent.Use();
        }

        private static void ShowStepContextMenu(SerializedProperty stepProperty, int index)
        {
            if (stepProperty == null)
                return;

            UnityEngine.Object[] targets = stepProperty.serializedObject.targetObjects;
            string stepPropertyPath = stepProperty.propertyPath;
            string listPropertyPath = ResolveParentListPropertyPath(stepPropertyPath);
            string renameRowKey = GetRenameRowKey(stepProperty);
            string currentDisplayName = stepProperty.FindPropertyRelative("DisplayName")?.stringValue ?? string.Empty;
            bool hasCustomLabel = !string.IsNullOrWhiteSpace(currentDisplayName);
            int arraySize = ResolveArraySize(stepProperty);

            ContextMenuBuilder menu = new();
            menu.AddItem(
                "Rename Label",
                stepProperty.managedReferenceValue != null,
                () => BeginRename(renameRowKey, currentDisplayName));
            menu.AddItem(
                "Clear Label",
                hasCustomLabel,
                () => ActionStepManagedReferenceUtility.ClearDisplayName(targets, stepPropertyPath));
            menu.AddSeparator();
            menu.AddItem(
                "Duplicate Step",
                !string.IsNullOrWhiteSpace(listPropertyPath),
                () => ActionStepManagedReferenceUtility.DuplicateStep(targets, listPropertyPath, index));
            menu.AddItem(
                "Delete Step",
                !string.IsNullOrWhiteSpace(listPropertyPath),
                () =>
                {
                    CancelRename(renameRowKey);
                    ActionStepManagedReferenceUtility.DeleteStep(targets, listPropertyPath, index);
                });
            menu.AddSeparator();
            menu.AddItem(
                "Move Up",
                index > 0,
                () => ActionStepManagedReferenceUtility.MoveStep(targets, listPropertyPath, index, index - 1));
            menu.AddItem(
                "Move Down",
                index >= 0 && index < arraySize - 1,
                () => ActionStepManagedReferenceUtility.MoveStep(targets, listPropertyPath, index, index + 1));
            menu.ShowAsContext();
        }

        private static int ResolveArraySize(SerializedProperty stepProperty)
        {
            string listPropertyPath = ResolveParentListPropertyPath(stepProperty.propertyPath);
            SerializedProperty listProperty = stepProperty.serializedObject.FindProperty(listPropertyPath);
            return listProperty != null && listProperty.isArray ? listProperty.arraySize : 0;
        }

        private static string ResolveParentListPropertyPath(string stepPropertyPath)
        {
            if (string.IsNullOrWhiteSpace(stepPropertyPath))
                return string.Empty;

            int markerIndex = stepPropertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            return markerIndex < 0 ? string.Empty : stepPropertyPath.Substring(0, markerIndex);
        }

        private static bool IsRenameActive(SerializedProperty stepProperty)
        {
            return string.Equals(
                SessionState.GetString(RenameActiveStateKey, string.Empty),
                GetRenameRowKey(stepProperty),
                StringComparison.Ordinal);
        }

        private static string GetRenameRowKey(SerializedProperty stepProperty)
        {
            return EditorStateKey.ForProperty(stepProperty, "rename");
        }

        private static string GetRenameTextStateKey(string rowKey)
        {
            return RenameTextStatePrefix + rowKey;
        }

        private static string GetRenameFocusStateKey(string rowKey)
        {
            return RenameFocusStatePrefix + rowKey;
        }

        private static string GetRenameFocusedStateKey(string rowKey)
        {
            return RenameFocusedStatePrefix + rowKey;
        }

        private static void BeginRename(string rowKey, string initialValue)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
                return;

            SessionState.SetString(RenameActiveStateKey, rowKey);
            SessionState.SetString(GetRenameTextStateKey(rowKey), initialValue ?? string.Empty);
            SessionState.SetBool(GetRenameFocusStateKey(rowKey), true);
            SessionState.SetBool(GetRenameFocusedStateKey(rowKey), false);
        }

        private static void CancelRename(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
                return;

            if (string.Equals(SessionState.GetString(RenameActiveStateKey, string.Empty), rowKey, StringComparison.Ordinal))
                SessionState.SetString(RenameActiveStateKey, string.Empty);

            SessionState.EraseString(GetRenameTextStateKey(rowKey));
            SessionState.SetBool(GetRenameFocusStateKey(rowKey), false);
            SessionState.SetBool(GetRenameFocusedStateKey(rowKey), false);
        }

        private static void CommitRename(SerializedProperty stepProperty, string rowKey, string displayName)
        {
            ActionStepManagedReferenceUtility.SetDisplayName(
                stepProperty.serializedObject.targetObjects,
                stepProperty.propertyPath,
                displayName);

            CancelRename(rowKey);
        }

        private static void DrawRenameField(Rect summaryRect, SerializedProperty stepProperty)
        {
            string rowKey = GetRenameRowKey(stepProperty);
            string textStateKey = GetRenameTextStateKey(rowKey);
            string focusStateKey = GetRenameFocusStateKey(rowKey);
            string focusedStateKey = GetRenameFocusedStateKey(rowKey);
            string controlName = $"InlineActionRename_{rowKey}";

            string currentText = SessionState.GetString(
                textStateKey,
                stepProperty.FindPropertyRelative("DisplayName")?.stringValue ?? string.Empty);

            GUI.SetNextControlName(controlName);
            string nextText = EditorGUI.TextField(summaryRect, currentText);

            if (!string.Equals(nextText, currentText, StringComparison.Ordinal))
                SessionState.SetString(textStateKey, nextText);

            if (SessionState.GetBool(focusStateKey, true))
            {
                EditorGUI.FocusTextInControl(controlName);
                SessionState.SetBool(focusStateKey, false);
            }

            Event currentEvent = Event.current;
            bool isFocused = GUI.GetNameOfFocusedControl() == controlName;
            bool hadFocus = SessionState.GetBool(focusedStateKey, false);

            if (isFocused)
                SessionState.SetBool(focusedStateKey, true);
            else if (hadFocus && currentEvent.type != EventType.Layout)
            {
                CommitRename(stepProperty, rowKey, SessionState.GetString(textStateKey, nextText));
                return;
            }

            if (!isFocused || currentEvent.type != EventType.KeyDown)
                return;

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                CommitRename(stepProperty, rowKey, SessionState.GetString(textStateKey, nextText));
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Escape)
            {
                CancelRename(rowKey);
                currentEvent.Use();
            }
        }
    }
}