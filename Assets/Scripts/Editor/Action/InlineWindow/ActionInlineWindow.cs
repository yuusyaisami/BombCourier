using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Editor.Foundation;
using BC.Editor.Foundation.UIToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BC.Editor.Action
{
    public sealed class ActionInlineWindow : SplitViewWindowBase
    {
        private const string WindowTitle = "Inline Action";
        private const string EmptyWindowMessage = "Open this window from an InlineAction inspector to bind a target.";
        private static readonly GUIContent AddStepButtonLabel = new("Add Step");
        private static readonly GUIContent ClearBranchButtonLabel = new("Clear Branch");

        private readonly SerializedObjectBridge serializedObjectBridge = new();
        private readonly ActionBlockTreeViewModel treeViewModel = new();
        private readonly ActionWindowSelectionState selectionState = new();
        private IMGUIContainerBridge detailBridge;
        private IMGUIContainer detailToolbarContainer;

        private ScrollView treeScrollView;
        private Label rootLabel;
        private Label targetLabel;
        private Label pathLabel;
        private Label footerLabel;
        private int validationCount;
        private int missingBranchCount;
        private bool isBoundTargetDirty;

        [MenuItem("Window/BombCourier/Inline Action")]
        private static void OpenWindow()
        {
            ActionInlineWindow window = GetWindow<ActionInlineWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(720f, 420f);
            window.Show();
            window.Focus();
            window.TryBindLastRequestIfNeeded();
            window.RefreshWindow();
        }

        internal static ActionInlineWindow Open(ActionInlineWindowLaunchRequest request)
        {
            ActionInlineWindow window = GetWindow<ActionInlineWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(720f, 420f);
            window.Show();
            window.Focus();
            window.Bind(request);
            return window;
        }

        public override void CreateGUI()
        {
            titleContent = new GUIContent(WindowTitle);
            base.CreateGUI();
            TryBindLastRequestIfNeeded();
            RefreshWindow();
        }

        protected override void BuildToolbar(VisualElement root)
        {
            root.style.justifyContent = Justify.SpaceBetween;

            VisualElement info = new();
            info.style.flexGrow = 1f;
            info.style.flexDirection = FlexDirection.Column;

            rootLabel = new Label();
            rootLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            targetLabel = new Label();
            pathLabel = new Label();
            pathLabel.style.color = new StyleColor(new Color(0.78f, 0.78f, 0.78f, 0.95f));

            info.Add(rootLabel);
            info.Add(targetLabel);
            info.Add(pathLabel);
            root.Add(info);

            Button refreshButton = new(RefreshWindow) { text = "Refresh" };
            root.Add(refreshButton);
        }

        protected override void BuildLeftPane(VisualElement root)
        {
            treeScrollView = new ScrollView();
            treeScrollView.style.flexGrow = 1f;
            root.Add(treeScrollView);
        }

        protected override void BuildRightPane(VisualElement root)
        {
            detailToolbarContainer ??= new IMGUIContainer(DrawDetailToolbar);
            detailToolbarContainer.style.marginBottom = EditorThemeTokens.SectionSpacing;
            root.Add(detailToolbarContainer);

            detailBridge ??= new IMGUIContainerBridge("Select a block, step, or branch to edit.");
            root.Add(detailBridge.Root);
        }

        protected override void BuildFooter(VisualElement root)
        {
            footerLabel = new Label();
            footerLabel.style.flexGrow = 1f;
            root.Add(footerLabel);
        }

        private void Bind(ActionInlineWindowLaunchRequest request)
        {
            if (!request.IsValid)
                return;

            serializedObjectBridge.Bind(request.ResolveTarget(), request.PropertyPath);
            selectionState.Clear();

            if (treeScrollView != null)
                RefreshWindow();
        }

        private void TryBindLastRequestIfNeeded()
        {
            if (serializedObjectBridge.IsBound)
                return;

            if (!ActionInlineWindowLauncher.TryGetLastRequest(out ActionInlineWindowLaunchRequest request))
                return;

            Bind(request);
        }

        private void RefreshWindow()
        {
            if (treeScrollView == null || rootLabel == null || targetLabel == null || pathLabel == null || footerLabel == null)
                return;

            if (!serializedObjectBridge.TryGetProperty(out SerializedProperty rootProperty))
            {
                treeViewModel.Rebuild(string.Empty, null);
                treeScrollView.Clear();
                treeScrollView.Add(CreateEmptyStateLabel(EmptyWindowMessage));
                detailBridge?.Clear(EmptyWindowMessage);
                detailToolbarContainer?.MarkDirtyRepaint();
                UpdateBindingLabels(null, string.Empty);
                validationCount = 0;
                missingBranchCount = 0;
                isBoundTargetDirty = false;
                footerLabel.text = "No InlineAction is currently bound.";
                return;
            }

            treeViewModel.Rebuild(rootProperty);
            UpdateDiagnostics(rootProperty);
            EnsureSelection();
            RebuildTreeRows();
            BindDetailPane();
            detailToolbarContainer?.MarkDirtyRepaint();
            UpdateBindingLabels(rootProperty.serializedObject.targetObject, rootProperty.propertyPath);
            UpdateFooter();
        }

        private void EnsureSelection()
        {
            if (TryFindSelectedItem(out _))
                return;

            if (treeViewModel.Items.Count > 0)
                selectionState.Select(treeViewModel.Items[0]);
            else
                selectionState.Clear();
        }

        private bool TryFindSelectedItem(out ActionBlockTreeItem selectedItem)
        {
            for (int i = 0; i < treeViewModel.Items.Count; i++)
            {
                ActionBlockTreeItem item = treeViewModel.Items[i];

                if (!item.BranchKey.Equals(selectionState.BranchKey))
                    continue;

                if (MapSelectionKind(item.Kind) != selectionState.Kind)
                    continue;

                selectionState.Select(item);
                selectedItem = item;
                return true;
            }

            selectedItem = null;
            return false;
        }

        private void RebuildTreeRows()
        {
            treeScrollView.Clear();

            if (treeViewModel.Items.Count == 0)
            {
                treeScrollView.Add(CreateEmptyStateLabel("The bound InlineAction has no visible tree items."));
                return;
            }

            for (int i = 0; i < treeViewModel.Items.Count; i++)
                treeScrollView.Add(CreateTreeRow(treeViewModel.Items[i]));
        }

        private VisualElement CreateTreeRow(ActionBlockTreeItem item)
        {
            bool selected = IsSelected(item);
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Column;
            row.style.alignItems = Align.Stretch;
            row.style.minHeight = item.Kind == ActionBlockTreeItemKind.Step ? 44f : EditorThemeTokens.RowHeight;
            row.style.marginBottom = EditorThemeTokens.RowSpacing;
            row.style.paddingLeft = 8f + (item.Depth * EditorThemeTokens.IndentWidth);
            row.style.paddingRight = 8f;
            row.style.paddingTop = 4f;
            row.style.paddingBottom = 4f;
            row.style.borderLeftWidth = selected ? 3f : 0f;
            row.style.borderLeftColor = EditorThemeTokens.SelectedColor;
            row.style.backgroundColor = ResolveRowBackground(item, selected);

            switch (item.Kind)
            {
                case ActionBlockTreeItemKind.Block:
                    PopulateBlockRow(row, item);
                    break;

                case ActionBlockTreeItemKind.Branch:
                    PopulateBranchRow(row, item);
                    break;

                default:
                    PopulateStepRow(row, item);
                    break;
            }

            row.tooltip = string.IsNullOrWhiteSpace(item.PropertyPath)
                ? item.Title
                : item.PropertyPath;

            row.RegisterCallback<MouseDownEvent>(_ =>
            {
                selectionState.Select(item);
                RebuildTreeRows();
                BindDetailPane();
                detailToolbarContainer?.MarkDirtyRepaint();
                UpdateFooter();
            });

            return row;
        }

        private void PopulateBlockRow(VisualElement row, ActionBlockTreeItem item)
        {
            SerializedProperty property = FindItemProperty(item);
            int stepCount = GetInlineActionStepCount(property);

            VisualElement header = CreateHorizontalRow();
            Label titleLabel = CreatePrimaryLabel(item.Title);
            header.Add(titleLabel);
            header.Add(CreateSpacer());
            header.Add(CreateBadgeLabel("Block", EditorThemeTokens.TypeBadgeBackground));

            if (property != null)
                header.Add(CreateBadgeLabel(stepCount == 1 ? "1 step" : $"{stepCount} steps", EditorThemeTokens.BranchBackground));

            row.Add(header);
        }

        private void PopulateBranchRow(VisualElement row, ActionBlockTreeItem item)
        {
            SerializedProperty property = FindItemProperty(item);
            int stepCount = GetInlineActionStepCount(property);
            string status = item.IsMissing
                ? "Missing"
                : stepCount <= 0
                    ? "Empty"
                    : stepCount == 1
                        ? "1 step"
                        : $"{stepCount} steps";

            VisualElement header = CreateHorizontalRow();
            header.Add(CreatePrimaryLabel(item.Title));
            header.Add(CreateSpacer());

            if (!string.IsNullOrWhiteSpace(item.Badge))
                header.Add(CreateBadgeLabel(item.Badge, EditorThemeTokens.TypeBadgeBackground));

            header.Add(CreateBadgeLabel(
                status,
                item.IsMissing ? EditorThemeTokens.MissingBackground : EditorThemeTokens.BranchBackground));

            row.Add(header);
        }

        private void PopulateStepRow(VisualElement row, ActionBlockTreeItem item)
        {
            SerializedProperty property = FindItemProperty(item);
            string typeLabel = property != null ? ActionStepSummaryUtility.GetTypeLabel(property) : item.Badge;
            string summary = property != null ? ActionStepSummaryUtility.GetSummary(property) : item.Title;
            IReadOnlyList<ActionStepBadge> badges = property != null
                ? ActionStepChildSlotUtility.GetBadges(property)
                : Array.Empty<ActionStepBadge>();

            VisualElement header = CreateHorizontalRow();
            header.Add(CreateIndexLabel(item.StepIndex));
            header.Add(CreateBadgeLabel(typeLabel, EditorThemeTokens.TypeBadgeBackground));
            header.Add(CreateSpacer());

            for (int i = 0; i < badges.Count; i++)
            {
                ActionStepBadge badge = badges[i];

                if (string.IsNullOrWhiteSpace(badge.Text))
                    continue;

                header.Add(CreateBadgeLabel(
                    badge.Text,
                    badge.Kind == ActionStepBadgeKind.Warning
                        ? EditorThemeTokens.MissingBackground
                        : EditorThemeTokens.BranchBackground));
            }

            Label summaryLabel = CreateSecondaryLabel(summary);
            row.Add(header);
            row.Add(summaryLabel);
        }

        private SerializedProperty FindItemProperty(ActionBlockTreeItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.PropertyPath))
                return null;

            SerializedObject serializedObject = serializedObjectBridge.SerializedObject;

            if (serializedObject == null)
                return null;

            serializedObject.UpdateIfRequiredOrScript();
            return serializedObject.FindProperty(item.PropertyPath);
        }

        private static VisualElement CreateHorizontalRow()
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        private static Label CreatePrimaryLabel(string text)
        {
            Label label = new(text ?? string.Empty);
            label.style.flexShrink = 1f;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            return label;
        }

        private static Label CreateSecondaryLabel(string text)
        {
            Label label = new(text ?? string.Empty);
            label.style.marginTop = 2f;
            label.style.fontSize = 11f;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.color = EditorGUIUtility.isProSkin
                ? new StyleColor(new Color(0.85f, 0.85f, 0.85f, 0.95f))
                : new StyleColor(new Color(0.18f, 0.18f, 0.18f, 0.95f));
            return label;
        }

        private static Label CreateIndexLabel(int stepIndex)
        {
            Label label = new(stepIndex >= 0 ? $"{stepIndex + 1}." : string.Empty);
            label.style.minWidth = 28f;
            label.style.marginRight = 4f;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            return label;
        }

        private static Label CreateBadgeLabel(string text, Color backgroundColor)
        {
            Label badgeLabel = new(text ?? string.Empty);
            badgeLabel.style.backgroundColor = backgroundColor;
            badgeLabel.style.paddingLeft = 6f;
            badgeLabel.style.paddingRight = 6f;
            badgeLabel.style.paddingTop = 2f;
            badgeLabel.style.paddingBottom = 2f;
            badgeLabel.style.marginLeft = 6f;
            badgeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            badgeLabel.style.whiteSpace = WhiteSpace.NoWrap;
            return badgeLabel;
        }

        private static VisualElement CreateSpacer()
        {
            VisualElement spacer = new();
            spacer.style.flexGrow = 1f;
            return spacer;
        }

        private static int GetInlineActionStepCount(SerializedProperty inlineActionProperty)
        {
            if (inlineActionProperty == null)
                return 0;

            if (inlineActionProperty.boxedValue is InlineAction inlineAction)
                return inlineAction.Steps?.Count ?? 0;

            SerializedProperty stepsProperty = inlineActionProperty.FindPropertyRelative("_steps");
            return stepsProperty != null && stepsProperty.isArray ? stepsProperty.arraySize : 0;
        }

        private void BindDetailPane()
        {
            string propertyPath = ResolveDetailPropertyPath();

            if (string.IsNullOrWhiteSpace(propertyPath))
            {
                detailBridge?.Clear("Select a block, step, or branch to edit.");
                return;
            }

            detailBridge?.Bind(serializedObjectBridge.SerializedObject, propertyPath);
        }

        private string ResolveDetailPropertyPath()
        {
            if (serializedObjectBridge.SerializedObject == null)
                return null;

            if (selectionState.Kind == ActionWindowSelectionKind.Step)
                return selectionState.PropertyPath;

            string inlineActionPropertyPath = selectionState.Kind == ActionWindowSelectionKind.None
                ? serializedObjectBridge.PropertyPath
                : selectionState.PropertyPath;

            if (string.IsNullOrWhiteSpace(inlineActionPropertyPath))
                return null;

            serializedObjectBridge.SerializedObject.UpdateIfRequiredOrScript();
            SerializedProperty inlineActionProperty = serializedObjectBridge.SerializedObject.FindProperty(inlineActionPropertyPath);
            SerializedProperty stepsProperty = inlineActionProperty?.FindPropertyRelative("_steps");
            return stepsProperty?.propertyPath;
        }

        private void DrawDetailToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(GetDetailToolbarTitle(), EditorStyles.miniBoldLabel);

                if (!TryResolveSelectedInlineActionProperty(out SerializedProperty inlineActionProperty, out SerializedProperty stepsProperty))
                    return;

                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect addButtonRect = GUILayoutUtility.GetRect(
                        AddStepButtonLabel,
                        EditorStyles.miniButton,
                        GUILayout.Width(90f));

                    if (GUI.Button(addButtonRect, AddStepButtonLabel, EditorStyles.miniButtonLeft))
                    {
                        stepsProperty = EnsureInlineActionStepsProperty(inlineActionProperty?.propertyPath);

                        if (stepsProperty != null)
                            ActionStepPickerDropdown.Show(addButtonRect, stepsProperty, RefreshWindow);
                    }

                    if (selectionState.Kind == ActionWindowSelectionKind.Branch)
                    {
                        using (new EditorGUI.DisabledScope(inlineActionProperty == null || inlineActionProperty.boxedValue == null))
                        {
                            if (GUILayout.Button(ClearBranchButtonLabel, EditorStyles.miniButtonRight, GUILayout.Width(96f)))
                            {
                                ActionStepManagedReferenceUtility.ClearInlineAction(
                                    inlineActionProperty.serializedObject.targetObjects,
                                    inlineActionProperty.propertyPath);
                                RefreshWindow();
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(
                        stepsProperty == null
                            ? "No step list"
                            : stepsProperty.arraySize == 1
                                ? "1 step"
                                : $"{stepsProperty.arraySize} steps",
                        EditorStyles.miniLabel,
                        GUILayout.Width(80f));
                }
            }
        }

        private string GetDetailToolbarTitle()
        {
            return selectionState.Kind switch
            {
                ActionWindowSelectionKind.Block => "Block Operations",
                ActionWindowSelectionKind.Branch => "Branch Operations",
                ActionWindowSelectionKind.Step => "Step Detail",
                _ => "Action Detail",
            };
        }

        private bool TryResolveSelectedInlineActionProperty(
            out SerializedProperty inlineActionProperty,
            out SerializedProperty stepsProperty)
        {
            inlineActionProperty = null;
            stepsProperty = null;

            if (selectionState.Kind == ActionWindowSelectionKind.Step)
                return false;

            string propertyPath = selectionState.Kind == ActionWindowSelectionKind.None
                ? serializedObjectBridge.PropertyPath
                : selectionState.PropertyPath;

            if (string.IsNullOrWhiteSpace(propertyPath) || serializedObjectBridge.SerializedObject == null)
                return false;

            serializedObjectBridge.SerializedObject.UpdateIfRequiredOrScript();
            inlineActionProperty = serializedObjectBridge.SerializedObject.FindProperty(propertyPath);
            stepsProperty = inlineActionProperty?.FindPropertyRelative("_steps");
            return inlineActionProperty != null;
        }

        private SerializedProperty EnsureInlineActionStepsProperty(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath) || serializedObjectBridge.SerializedObject == null)
                return null;

            SerializedObject serializedObject = serializedObjectBridge.SerializedObject;
            SerializedProperty inlineActionProperty = serializedObject.FindProperty(propertyPath);

            if (inlineActionProperty?.boxedValue == null)
            {
                UndoApplyUtility.ApplyToTargets(
                    serializedObject.targetObjects,
                    "Create Inline Action",
                    currentSerializedObject =>
                    {
                        SerializedProperty currentProperty = currentSerializedObject.FindProperty(propertyPath);

                        if (currentProperty != null && currentProperty.boxedValue == null)
                            currentProperty.boxedValue = new InlineAction();
                    });
            }

            serializedObject.UpdateIfRequiredOrScript();
            inlineActionProperty = serializedObject.FindProperty(propertyPath);
            return inlineActionProperty?.FindPropertyRelative("_steps");
        }

        private void UpdateBindingLabels(UnityEngine.Object targetObject, string propertyPath)
        {
            rootLabel.text = string.IsNullOrWhiteSpace(propertyPath)
                ? WindowTitle
                : $"Root: {propertyPath}";
            targetLabel.text = targetObject == null
                ? "Target: Unbound"
                : $"Target: {targetObject.name}";
            pathLabel.text = string.IsNullOrWhiteSpace(propertyPath)
                ? string.Empty
                : $"Path: {propertyPath}";
        }

        private void UpdateFooter()
        {
            string selectionPath = selectionState.Kind == ActionWindowSelectionKind.None
                ? "Root"
                : string.IsNullOrWhiteSpace(selectionState.PropertyPath)
                    ? selectionState.Kind.ToString()
                    : selectionState.PropertyPath;

            footerLabel.text =
                $"Selection: {selectionPath} | Validation: {validationCount} | Missing branches: {missingBranchCount} | Dirty: {(isBoundTargetDirty ? "Yes" : "No")}";
        }

        private void UpdateDiagnostics(SerializedProperty rootProperty)
        {
            validationCount = CountValidationErrors(rootProperty);
            missingBranchCount = CountMissingBranches();
            isBoundTargetDirty = rootProperty?.serializedObject?.targetObject != null &&
                                EditorUtility.IsDirty(rootProperty.serializedObject.targetObject);
        }

        private int CountMissingBranches()
        {
            int count = 0;

            for (int i = 0; i < treeViewModel.Items.Count; i++)
            {
                ActionBlockTreeItem item = treeViewModel.Items[i];

                if (item.Kind == ActionBlockTreeItemKind.Branch && item.IsMissing)
                    count++;
            }

            return count;
        }

        private static int CountValidationErrors(SerializedProperty rootProperty)
        {
            if (rootProperty?.boxedValue is not InlineAction inlineAction)
                return 0;

            ActionValidationContext validationContext = new();
            inlineAction.Validate(validationContext);
            return validationContext.Errors.Count;
        }

        private bool IsSelected(ActionBlockTreeItem item)
        {
            return item != null &&
                   item.BranchKey.Equals(selectionState.BranchKey) &&
                   MapSelectionKind(item.Kind) == selectionState.Kind;
        }

        private static ActionWindowSelectionKind MapSelectionKind(ActionBlockTreeItemKind kind)
        {
            return kind switch
            {
                ActionBlockTreeItemKind.Block => ActionWindowSelectionKind.Block,
                ActionBlockTreeItemKind.Step => ActionWindowSelectionKind.Step,
                ActionBlockTreeItemKind.Branch => ActionWindowSelectionKind.Branch,
                _ => ActionWindowSelectionKind.None,
            };
        }

        private static Label CreateEmptyStateLabel(string text)
        {
            Label label = new(string.IsNullOrWhiteSpace(text) ? EmptyWindowMessage : text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = EditorThemeTokens.SectionSpacing;
            return label;
        }

        private static Color ResolveRowBackground(ActionBlockTreeItem item, bool selected)
        {
            Color baseColor = item.IsMissing
                ? EditorThemeTokens.MissingBackground
                : item.Kind switch
                {
                    ActionBlockTreeItemKind.Block => EditorThemeTokens.BlockBackground,
                    ActionBlockTreeItemKind.Branch => EditorThemeTokens.BranchBackground,
                    _ => EditorThemeTokens.StepBackground,
                };

            return selected
                ? Color.Lerp(baseColor, EditorThemeTokens.SelectedColor, 0.18f)
                : baseColor;
        }
    }
}
