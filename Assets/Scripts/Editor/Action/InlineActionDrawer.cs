using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Editor.Foundation;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.ActionSystem
{
    [CustomPropertyDrawer(typeof(InlineAction))]
    public sealed class InlineActionDrawer : PropertyDrawerBase
    {
        private const float HeaderPadding = 4f;
        private const float HandleWidth = 12f;
        private const float HandleSpacing = 10f;
        private const float FoldoutWidth = 14f;
        private const float FoldoutSpacing = 6f;
        private const float IndexWidth = 28f;
        private const float MinSummaryWidth = 72f;
        private const float MaxTypeBadgeWidth = 120f;
        private const float MaxStateBadgeWidth = 132f;

        private static readonly GUIContent EmptyListLabel = new("Empty");
        private static readonly HashSet<string> AutoExpandedPropertyNames = new(StringComparer.Ordinal)
        {
            "talkRequestData",
            "dialogueRequestData",
            "screenOverlayShowRequestData",
        };

        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty stepsProperty = property.FindPropertyRelative("_steps");

            if (stepsProperty == null || !stepsProperty.isArray)
                return LineHeight;

            float contentHeight = CreateListController(property).GetHeight(stepsProperty);

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
            InlineListController controller = CreateListController(property);
            float listHeight = controller.GetHeight(stepsProperty);
            Rect listRect = new(contentRect.x, contentRect.y, contentRect.width, listHeight);
            controller.Draw(listRect, stepsProperty, EmptyListLabel);
            HandleSectionInteraction(listRect, property, stepsProperty);
        }

        private InlineListController CreateListController(SerializedProperty inlineActionProperty)
        {
            // Keep the owning InlineAction property in scope so row callbacks can open the dedicated window or show section menus.
            return new InlineListController(
                GetRowHeight,
                (rowRect, stepProperty, index) => DrawRow(rowRect, stepProperty, index, inlineActionProperty),
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

        private void DrawRow(Rect rowRect, SerializedProperty stepProperty, int index, SerializedProperty inlineActionProperty)
        {
            Rect headerRect = new(rowRect.x, rowRect.y, rowRect.width, LineHeight);
            DrawHeaderBackground(headerRect, stepProperty);

            string typeLabel = ActionStepSummaryUtility.GetTypeLabel(stepProperty);
            string summary = ActionStepSummaryUtility.GetSummary(stepProperty);
            IReadOnlyList<ActionStepBadge> badges = ActionStepChildSlotUtility.GetBadges(stepProperty);

            Rect contentRect = new(
                headerRect.x + HeaderPadding,
                headerRect.y,
                Mathf.Max(0f, headerRect.width - (HeaderPadding * 2f)),
                headerRect.height);

            Rect handleRect = RectLayoutUtility.TakeLeft(ref contentRect, HandleWidth, HandleSpacing);
            Rect foldoutRect = RectLayoutUtility.TakeLeft(ref contentRect, FoldoutWidth, FoldoutSpacing);
            Rect indexRect = RectLayoutUtility.TakeLeft(ref contentRect, IndexWidth, 4f);
            float typeBadgeWidth = ResolveTypeBadgeWidth(typeLabel);
            Rect typeBadgeRect = RectLayoutUtility.TakeLeft(ref contentRect, typeBadgeWidth, 4f);
            Rect summaryRect = contentRect;

            HandleRowInteraction(headerRect, foldoutRect, stepProperty, index, inlineActionProperty);

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

            if (InlineActionEditorState.IsActive(stepProperty))
                background = Color.Lerp(background, EditorThemeTokens.SelectedColor, 0.18f);

            EditorGUI.DrawRect(headerRect, background);

            if (InlineActionEditorState.IsActive(stepProperty))
            {
                Rect selectionRect = new(headerRect.x, headerRect.y, 3f, headerRect.height);
                EditorGUI.DrawRect(selectionRect, EditorThemeTokens.SelectedColor);
            }
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

                if (ShouldAutoExpandDetail(iterator))
                    iterator.isExpanded = true;

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

                if (ShouldAutoExpandDetail(iterator))
                    iterator.isExpanded = true;

                float childHeight = EditorGUI.GetPropertyHeight(iterator, true);
                Rect childRect = RectLayoutUtility.TakeHeight(ref cursor, childHeight);
                EditorGUI.PropertyField(childRect, iterator, true);
            }
        }

        private static void HandleSectionInteraction(
            Rect interactionRect,
            SerializedProperty inlineActionProperty,
            SerializedProperty stepsProperty)
        {
            Event currentEvent = Event.current;

            // The inline surface now exposes structural actions from right-click/double-click instead of footer buttons.
            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                currentEvent.clickCount == 2 &&
                interactionRect.Contains(currentEvent.mousePosition) &&
                ActionInlineWindowLauncher.CanLaunch(inlineActionProperty))
            {
                ActionInlineWindowLauncher.LaunchAndOpen(inlineActionProperty);
                currentEvent.Use();
                return;
            }

            if (currentEvent.type != EventType.ContextClick || !interactionRect.Contains(currentEvent.mousePosition))
                return;

            UnityEngine.Object[] targets = inlineActionProperty?.serializedObject?.targetObjects;
            string stepsPropertyPath = stepsProperty?.propertyPath;
            ContextMenuBuilder menu = new();
            IReadOnlyList<Type> stepTypes = ActionStepManagedReferenceUtility.GetStepTypes();
            bool canAddStep = targets != null && !string.IsNullOrWhiteSpace(stepsPropertyPath);

            ActionStepRecentSelectionUtility.AppendRecentStepMenuItems(
                menu,
                "Add Step",
                canAddStep,
                stepType => ActionStepManagedReferenceUtility.AddStep(targets, stepsPropertyPath, stepType));

            for (int i = 0; i < stepTypes.Count; i++)
            {
                Type stepType = stepTypes[i];
                menu.AddItem(
                    $"Add Step/{ActionStepManagedReferenceUtility.GetStepTypeLabel(stepType)}",
                    canAddStep,
                    () => ActionStepManagedReferenceUtility.AddStep(targets, stepsPropertyPath, stepType));
            }

            menu.AddSeparator();
            menu.AddItem(
                "Open in Window",
                ActionInlineWindowLauncher.CanLaunch(inlineActionProperty),
                () => ActionInlineWindowLauncher.LaunchAndOpen(inlineActionProperty));
            menu.AddItem(
                "Delete",
                targets != null && !string.IsNullOrWhiteSpace(stepsPropertyPath) && stepsProperty != null && stepsProperty.arraySize > 0,
                () =>
                {
                    InlineActionEditorState.ClearActive();
                    ActionStepManagedReferenceUtility.ClearSteps(targets, stepsPropertyPath);
                });
            menu.AddItem(
                "Paste",
                targets != null && !string.IsNullOrWhiteSpace(stepsPropertyPath) && ActionStepManagedReferenceUtility.CanPasteStep(),
                () => ActionStepManagedReferenceUtility.PasteStep(targets, stepsPropertyPath));
            menu.ShowAsContext();
            currentEvent.Use();
        }

        private static bool ShouldAutoExpandDetail(SerializedProperty property)
        {
            // Wrapper payloads like talkRequestData become useful only when their nested fields are immediately visible.
            return property != null &&
                   property.propertyType == SerializedPropertyType.Generic &&
                   property.hasVisibleChildren &&
                   !property.isArray &&
                   AutoExpandedPropertyNames.Contains(property.name);
        }

        private static void HandleRowInteraction(
            Rect headerRect,
            Rect foldoutRect,
            SerializedProperty stepProperty,
            int index,
            SerializedProperty inlineActionProperty)
        {
            Event currentEvent = Event.current;

            if (stepProperty == null)
                return;

            // Row-local keyboard shortcuts operate on the last clicked item so delete/copy/paste feel predictable.
            if (currentEvent.type == EventType.MouseDown && headerRect.Contains(currentEvent.mousePosition))
            {
                InlineActionEditorState.SetActive(stepProperty);

                // 折り畳みの当たり判定上では window を開かず、展開/折り畳み操作を優先する。
                if (currentEvent.button == 0 &&
                    currentEvent.clickCount == 2 &&
                    !foldoutRect.Contains(currentEvent.mousePosition) &&
                    ActionInlineWindowLauncher.CanLaunch(inlineActionProperty))
                {
                    ActionInlineWindowLauncher.LaunchAndOpen(inlineActionProperty);
                    currentEvent.Use();
                    return;
                }

                if (currentEvent.button == 1)
                {
                    ActionStepContextMenuUtility.Show(stepProperty, index);
                    currentEvent.Use();
                    return;
                }
            }

            if (!InlineActionEditorState.IsActive(stepProperty) || currentEvent.type != EventType.KeyDown)
                return;

            if (InlineActionEditorState.IsRenameActive(stepProperty) || EditorGUIUtility.editingTextField)
                return;

            string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(stepProperty.propertyPath);
            UnityEngine.Object[] targets = stepProperty.serializedObject.targetObjects;

            if (string.IsNullOrWhiteSpace(listPropertyPath) || targets == null)
                return;

            if (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace)
            {
                InlineActionEditorState.CancelRename(stepProperty);
                InlineActionEditorState.ClearActive();
                ActionStepManagedReferenceUtility.DeleteStep(targets, listPropertyPath, index);
                currentEvent.Use();
                GUI.changed = true;
                GUIUtility.ExitGUI();
            }

            if (!IsActionShortcut(currentEvent))
                return;

            if (currentEvent.keyCode == KeyCode.C)
            {
                ActionStepManagedReferenceUtility.CopyStep(stepProperty);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.V && ActionStepManagedReferenceUtility.CanPasteStep())
            {
                ActionStepManagedReferenceUtility.PasteStep(targets, listPropertyPath, index + 1);
                currentEvent.Use();
                GUI.changed = true;
                GUIUtility.ExitGUI();
            }
        }

        private static bool IsActionShortcut(Event currentEvent)
        {
            return currentEvent != null && (currentEvent.control || currentEvent.command);
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
