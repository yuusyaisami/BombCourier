using System;
using System.Collections.Generic;
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
        private const float FooterSpacing = 4f;
        private const float MinSummaryWidth = 72f;
        private const float MaxTypeBadgeWidth = 120f;
        private const float MaxStateBadgeWidth = 132f;

        private static readonly GUIContent EmptyListLabel = new("Empty");
        private static readonly GUIContent AddStepButtonLabel = new("Add Step");
        private static readonly GUIContent OpenInWindowButtonLabel = new("Open in Window");

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
            DrawFooter(footerRect, property, stepsProperty);
        }

        private InlineListController CreateListController()
        {
            return new InlineListController(
                GetRowHeight,
                DrawRow,
                MoveRow,
                HeaderPadding + HandleWidth);
        }

        private float GetRowHeight(SerializedProperty stepProperty, int index)
        {
            if (stepProperty == null)
                return LineHeight;

            float height = LineHeight;

            if (InlineActionEditorState.IsExpanded(stepProperty))
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
            IReadOnlyList<ActionStepBadge> badges = ActionStepChildSlotUtility.GetBadges(stepProperty);

            Rect contentRect = new(
                headerRect.x + HeaderPadding,
                headerRect.y,
                Mathf.Max(0f, headerRect.width - (HeaderPadding * 2f)),
                headerRect.height);

            Rect handleRect = RectLayoutUtility.TakeLeft(ref contentRect, HandleWidth, 2f);
            Rect foldoutRect = RectLayoutUtility.TakeLeft(ref contentRect, FoldoutWidth, 2f);
            Rect indexRect = RectLayoutUtility.TakeLeft(ref contentRect, IndexWidth, 4f);
            float typeBadgeWidth = ResolveTypeBadgeWidth(typeLabel);
            Rect typeBadgeRect = RectLayoutUtility.TakeLeft(ref contentRect, typeBadgeWidth, 4f);
            Rect summaryRect = contentRect;

            DrawStateBadges(ref summaryRect, badges);

            GUI.Label(handleRect, "||", EditorStyles.centeredGreyMiniLabel);

            bool expanded = EditorGUI.Foldout(
                foldoutRect,
                InlineActionEditorState.IsExpanded(stepProperty),
                GUIContent.none,
                false);
            InlineActionEditorState.SetExpanded(stepProperty, expanded);

            EditorGUI.LabelField(indexRect, (index + 1).ToString(), EditorStyles.centeredGreyMiniLabel);
            DrawTypeBadge(typeBadgeRect, typeLabel);

            if (InlineActionEditorState.IsRenameActive(stepProperty))
                DrawRenameField(summaryRect, stepProperty);
            else
                EditorGUI.LabelField(summaryRect, summary);

            if (!expanded)
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

        private void MoveRow(SerializedProperty listProperty, int sourceIndex, int destinationIndex)
        {
            ActionStepManagedReferenceUtility.MoveStep(
                listProperty.serializedObject.targetObjects,
                listProperty.propertyPath,
                sourceIndex,
                destinationIndex);
        }

        private static void DrawHeaderBackground(Rect headerRect, SerializedProperty stepProperty)
        {
            Color background = stepProperty?.managedReferenceValue == null
                ? EditorThemeTokens.MissingBackground
                : EditorThemeTokens.StepBackground;

            EditorGUI.DrawRect(headerRect, background);
        }

        private static float ResolveTypeBadgeWidth(string typeLabel)
        {
            return Mathf.Min(
                MaxTypeBadgeWidth,
                EditorStyles.miniBoldLabel.CalcSize(new GUIContent(typeLabel)).x + 12f);
        }

        private static float ResolveStateBadgeWidth(string text)
        {
            return Mathf.Min(
                MaxStateBadgeWidth,
                EditorStyles.miniBoldLabel.CalcSize(new GUIContent(text)).x + 12f);
        }

        private static void DrawTypeBadge(Rect badgeRect, string typeLabel)
        {
            EditorGUI.DrawRect(badgeRect, EditorThemeTokens.TypeBadgeBackground);
            EditorGUI.LabelField(RectLayoutUtility.WithPadding(badgeRect, 2f), typeLabel, EditorStyles.miniBoldLabel);
        }

        private static void DrawStateBadges(ref Rect summaryRect, IReadOnlyList<ActionStepBadge> badges)
        {
            if (badges == null || badges.Count == 0)
                return;

            for (int i = badges.Count - 1; i >= 0; i--)
            {
                ActionStepBadge badge = badges[i];

                if (string.IsNullOrWhiteSpace(badge.Text))
                    continue;

                float badgeWidth = ResolveStateBadgeWidth(badge.Text);

                if (summaryRect.width - badgeWidth - 4f < MinSummaryWidth)
                    break;

                Rect badgeRect = RectLayoutUtility.TakeRight(ref summaryRect, badgeWidth, 4f);
                DrawStateBadge(badgeRect, badge);
            }
        }

        private static void DrawStateBadge(Rect badgeRect, ActionStepBadge badge)
        {
            Color background = badge.Kind == ActionStepBadgeKind.Warning
                ? EditorThemeTokens.MissingBackground
                : EditorThemeTokens.BranchBackground;

            EditorGUI.DrawRect(badgeRect, background);
            EditorGUI.LabelField(RectLayoutUtility.WithPadding(badgeRect, 2f), badge.Text, EditorStyles.miniBoldLabel);
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

        private static void DrawFooter(
            Rect footerRect,
            SerializedProperty inlineActionProperty,
            SerializedProperty stepsProperty)
        {
            float buttonWidth = Mathf.Max(0f, (footerRect.width - FooterSpacing) * 0.5f);
            Rect addButtonRect = new(footerRect.x, footerRect.y, buttonWidth, footerRect.height);
            Rect openButtonRect = new(addButtonRect.xMax + FooterSpacing, footerRect.y, buttonWidth, footerRect.height);

            if (GUI.Button(addButtonRect, AddStepButtonLabel, EditorStyles.miniButtonLeft))
                ActionStepPickerDropdown.Show(addButtonRect, stepsProperty);

            using (new EditorGUI.DisabledScope(!ActionInlineWindowLauncher.CanLaunch(inlineActionProperty)))
            {
                if (GUI.Button(openButtonRect, OpenInWindowButtonLabel, EditorStyles.miniButtonRight))
                    ActionInlineWindowLauncher.LaunchAndOpen(inlineActionProperty);
            }
        }

        private static void HandleContextClick(Rect headerRect, SerializedProperty stepProperty, int index)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.ContextClick || !headerRect.Contains(currentEvent.mousePosition))
                return;

            ActionStepContextMenuUtility.Show(stepProperty, index);
            currentEvent.Use();
        }

        private static void DrawRenameField(Rect summaryRect, SerializedProperty stepProperty)
        {
            string controlName = InlineActionEditorState.GetRenameControlName(stepProperty);
            string currentText = InlineActionEditorState.GetRenameText(stepProperty);

            GUI.SetNextControlName(controlName);
            string nextText = EditorGUI.TextField(summaryRect, currentText);

            if (!string.Equals(nextText, currentText, StringComparison.Ordinal))
                InlineActionEditorState.SetRenameText(stepProperty, nextText);

            if (InlineActionEditorState.ConsumeRenameFocus(stepProperty))
                EditorGUI.FocusTextInControl(controlName);

            Event currentEvent = Event.current;
            bool isFocused = GUI.GetNameOfFocusedControl() == controlName;

            if (InlineActionEditorState.ShouldCommitRenameOnFocusLoss(stepProperty, isFocused, currentEvent.type))
            {
                InlineActionEditorState.CommitRename(stepProperty);
                return;
            }

            if (!isFocused || currentEvent.type != EventType.KeyDown)
                return;

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                InlineActionEditorState.CommitRename(stepProperty);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Escape)
            {
                InlineActionEditorState.CancelRename(stepProperty);
                currentEvent.Use();
            }
        }
    }
}
