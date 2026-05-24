using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Editor.Foundation;
using BC.Editor.Foundation.IMGUI;
using BC.Editor.Foundation.UIToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BC.Editor.ActionSystem
{
    public sealed class ActionInlineWindow : SplitViewWindowBase
    {
        private const string WindowTitle = "Inline Action";
        private const string EmptyWindowMessage = "Open this window from an InlineAction inspector to bind a target.";
        private const float ContainerRowHeight = 28f;
        private const float StepRowHeight = 40f;
        private const float RowPaddingLeft = 6f;
        private const float TreeIndentWidth = 8f;
        private const float ExpansionSlotWidth = 14f;
        private const float ExpansionSlotSpacing = 4f;
        private const float DragHandleWidth = 12f;
        private const float DragHandleSpacing = 4f;
        private const float IndexColumnWidth = 24f;
        private const float IndexColumnSpacing = 4f;
        private const float StepSummaryIndent = 52f;
        private const float IndentGuideOffset = 10f;
        private const float DragStartHoldSeconds = 0.4f;
        private const float DragHoverAutoExpandSeconds = 0.4f;
        private const float DropEdgeRatio = 0.35f;
        private static readonly GUIContent AddStepButtonLabel = new("Add Step");
        private static readonly GUIContent ClearBranchButtonLabel = new("Clear Branch");
        private static readonly Color DropIndicatorColor = new(0.32f, 0.67f, 1.0f, 0.95f);

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
        private DragRuntimeState dragState;
        private ActionBlockTreeItem pendingDragItem;
        private double pendingDragStartTime;
        private string dragSourceListPropertyPath;
        private readonly List<int> dragSourceIndices = new();
        private ActionDropTarget currentDropTarget;
        private ActionBlockTreeItem hoverExpandItem;
        private double hoverExpandStartTime;
        private readonly List<string> pendingSelectStepPropertyPaths = new();
        private bool hasPendingDetailRefresh;

        private enum DragRuntimeState
        {
            None = 0,
            Pending = 1,
            Dragging = 2,
        }

        private enum DropPlacement
        {
            None = 0,
            BeforeStep = 1,
            AfterStep = 2,
            IntoContainer = 3,
        }

        private readonly struct ActionDropTarget
        {
            internal ActionDropTarget(
                string listPropertyPath,
                int insertIndex,
                string inlineActionPropertyPath,
                string visualPropertyPath,
                ActionBlockTreeItemKind visualKind,
                DropPlacement placement)
            {
                ListPropertyPath = listPropertyPath ?? string.Empty;
                InsertIndex = insertIndex;
                InlineActionPropertyPath = inlineActionPropertyPath ?? string.Empty;
                VisualPropertyPath = visualPropertyPath ?? string.Empty;
                VisualKind = visualKind;
                Placement = placement;
            }

            internal string ListPropertyPath { get; }
            internal int InsertIndex { get; }
            internal string InlineActionPropertyPath { get; }
            internal string VisualPropertyPath { get; }
            internal ActionBlockTreeItemKind VisualKind { get; }
            internal DropPlacement Placement { get; }
            internal bool IsValid => !string.IsNullOrWhiteSpace(ListPropertyPath) && InsertIndex >= 0;
        }

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
            treeScrollView.focusable = true;
            treeScrollView.RegisterCallback<KeyDownEvent>(HandleTreeKeyDown);
            treeScrollView.RegisterCallback<MouseUpEvent>(HandleTreeMouseUp);
            root.Add(treeScrollView);
        }

        protected override void BuildRightPane(VisualElement root)
        {
            detailToolbarContainer ??= new IMGUIContainer(DrawDetailToolbar);
            detailToolbarContainer.style.marginBottom = EditorThemeTokens.SectionSpacing;
            root.Add(detailToolbarContainer);

            if (detailBridge == null)
            {
                detailBridge = new IMGUIContainerBridge("Select a block, step, or branch to edit.");
                detailBridge.Applied += HandleDetailPropertyApplied;
            }

            root.Add(detailBridge.Root);
        }

        private void Update()
        {
            if (!hasPendingDetailRefresh)
                return;

            hasPendingDetailRefresh = false;
            RefreshWindow();
        }

        private void HandleDetailPropertyApplied()
        {
            // Defer refresh so tree rebuild runs outside IMGUI drawing and picks up dynamic child-slot changes.
            hasPendingDetailRefresh = true;
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

            CancelDragState();
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
                CancelDragState();
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
            TrySelectPendingMovedSteps();

            List<ActionBlockTreeItem> stepItems = GetStepItemsInTreeOrder();
            selectionState.SyncStepSelection(stepItems);

            if (TryFindSelectedItem(out _))
                return;

            if (treeViewModel.Items.Count > 0)
                selectionState.Select(treeViewModel.Items[0]);
            else
                selectionState.Clear();
        }

        private void TrySelectPendingMovedSteps()
        {
            if (pendingSelectStepPropertyPaths.Count <= 0)
                return;

            List<ActionBlockTreeItem> pendingSelectionItems = new();
            ActionBlockTreeItem focusedItem = null;

            for (int i = 0; i < treeViewModel.Items.Count; i++)
            {
                ActionBlockTreeItem item = treeViewModel.Items[i];

                if (item.Kind != ActionBlockTreeItemKind.Step)
                    continue;

                for (int pendingIndex = 0; pendingIndex < pendingSelectStepPropertyPaths.Count; pendingIndex++)
                {
                    if (!string.Equals(item.PropertyPath, pendingSelectStepPropertyPaths[pendingIndex], StringComparison.Ordinal))
                        continue;

                    pendingSelectionItems.Add(item);

                    if (focusedItem == null)
                        focusedItem = item;

                    break;
                }
            }

            if (pendingSelectionItems.Count > 0)
                selectionState.SetStepSelection(pendingSelectionItems, focusedItem, focusedItem);

            pendingSelectStepPropertyPaths.Clear();
        }

        private bool TryFindSelectedItem(out ActionBlockTreeItem selectedItem)
        {
            for (int i = 0; i < treeViewModel.Items.Count; i++)
            {
                ActionBlockTreeItem item = treeViewModel.Items[i];

                if (selectionState.Kind == ActionWindowSelectionKind.Step)
                {
                    if (string.Equals(item.PropertyPath, selectionState.PropertyPath, StringComparison.Ordinal))
                    {
                        selectedItem = item;
                        return true;
                    }

                    continue;
                }

                if (!item.BranchKey.Equals(selectionState.BranchKey))
                    continue;

                if (MapSelectionKind(item.Kind) != selectionState.Kind)
                    continue;

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

            int collapsedDepth = int.MinValue;

            for (int i = 0; i < treeViewModel.Items.Count; i++)
            {
                ActionBlockTreeItem item = treeViewModel.Items[i];

                // Once a container is collapsed, skip every deeper descendant until the tree climbs back out.
                if (collapsedDepth != int.MinValue)
                {
                    if (item.Depth > collapsedDepth)
                        continue;

                    collapsedDepth = int.MinValue;
                }

                treeScrollView.Add(CreateTreeRow(item));

                if (item.CanExpand && !selectionState.IsExpanded(item))
                    collapsedDepth = item.Depth;
            }
        }

        private VisualElement CreateTreeRow(ActionBlockTreeItem item)
        {
            bool selected = IsSelected(item);
            VisualElement row = new();
            row.style.position = Position.Relative;
            row.style.flexDirection = FlexDirection.Column;
            row.style.alignItems = Align.Stretch;
            row.style.minHeight = item.Kind == ActionBlockTreeItemKind.Step ? StepRowHeight : ContainerRowHeight;
            row.style.marginBottom = EditorThemeTokens.RowSpacing;
            row.style.paddingLeft = RowPaddingLeft + (item.Depth * TreeIndentWidth);
            row.style.paddingRight = 8f;
            row.style.paddingTop = 4f;
            row.style.paddingBottom = 4f;
            row.style.backgroundColor = ResolveRowBackground(item, selected);
            ApplyDropIndicatorStyle(row, item);

            // Draw guide lines before the content so block/branch/step rows all share the same depth scaffold.
            AddIndentGuides(row, item.Depth);

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

            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                HandleRowMouseDown(evt, item);
            });

            row.RegisterCallback<MouseMoveEvent>(evt => HandleRowMouseMove(evt, item, row));
            row.RegisterCallback<MouseUpEvent>(evt => HandleRowMouseUp(evt));

            return row;
        }

        private void PopulateBlockRow(VisualElement row, ActionBlockTreeItem item)
        {
            SerializedProperty property = FindItemProperty(item);
            int stepCount = GetInlineActionStepCount(property);

            VisualElement header = CreateHorizontalRow();
            header.Add(CreateExpansionIndicator(item));
            header.Add(CreateIndexSpacer());
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
            header.Add(CreateExpansionIndicator(item));
            header.Add(CreateIndexSpacer());
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
            header.Add(CreateExpansionIndicator(item));
            header.Add(CreateDragHandleLabel());
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
            summaryLabel.style.marginLeft = StepSummaryIndent;
            row.Add(header);
            row.Add(summaryLabel);
        }

        private VisualElement CreateExpansionIndicator(ActionBlockTreeItem item)
        {
            Label indicator = new();
            indicator.style.minWidth = ExpansionSlotWidth;
            indicator.style.marginRight = ExpansionSlotSpacing;
            indicator.style.unityTextAlign = TextAnchor.MiddleCenter;
            indicator.text = item.CanExpand
                ? selectionState.IsExpanded(item) ? "v" : ">"
                : string.Empty;

            if (!item.CanExpand)
                return indicator;

            indicator.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                selectionState.ToggleExpanded(item);
                selectionState.Select(item);
                RefreshWindow();
                evt.StopImmediatePropagation();
            });

            return indicator;
        }

        private static VisualElement CreateExpansionSpacer()
        {
            VisualElement spacer = new();
            spacer.style.minWidth = ExpansionSlotWidth;
            spacer.style.marginRight = ExpansionSlotSpacing;
            return spacer;
        }

        private static VisualElement CreateIndexSpacer()
        {
            VisualElement spacer = new();
            spacer.style.minWidth = IndexColumnWidth;
            spacer.style.marginRight = IndexColumnSpacing;
            return spacer;
        }

        private static Label CreateDragHandleLabel()
        {
            Label handle = new("||");
            handle.style.minWidth = DragHandleWidth;
            handle.style.marginRight = DragHandleSpacing;
            handle.style.unityTextAlign = TextAnchor.MiddleCenter;
            handle.style.color = EditorGUIUtility.isProSkin
                ? new StyleColor(new Color(0.75f, 0.75f, 0.75f, 0.95f))
                : new StyleColor(new Color(0.25f, 0.25f, 0.25f, 0.95f));
            return handle;
        }

        private void HandleRowMouseDown(MouseDownEvent evt, ActionBlockTreeItem item)
        {
            if (evt == null || item == null)
                return;

            if (evt.button == 1)
            {
                if (dragState == DragRuntimeState.Dragging)
                    return;

                if (!(item.Kind == ActionBlockTreeItemKind.Step && selectionState.IsStepSelected(item)))
                    SelectItem(item);

                treeScrollView?.Focus();
                ShowContextMenu(item);
                evt.StopImmediatePropagation();
                return;
            }

            if (evt.button != 0)
                return;

            if (item.Kind == ActionBlockTreeItemKind.Step)
                ApplyStepSelectionFromMouse(item, evt.shiftKey, evt.ctrlKey || evt.commandKey);
            else
                SelectItem(item);

            treeScrollView?.Focus();

            if (item.Kind == ActionBlockTreeItemKind.Step)
                BeginDragPending(item);
            else
                CancelPendingDrag();
        }

        private void HandleRowMouseMove(MouseMoveEvent evt, ActionBlockTreeItem item, VisualElement row)
        {
            if (evt == null || item == null || row == null)
                return;

            if (dragState == DragRuntimeState.Pending)
            {
                if ((evt.pressedButtons & 1) == 0)
                {
                    CancelPendingDrag();
                    return;
                }

                double elapsed = EditorApplication.timeSinceStartup - pendingDragStartTime;
                if (elapsed >= DragStartHoldSeconds)
                {
                    BeginDragging();
                    RebuildTreeRows();
                }
            }

            if (dragState != DragRuntimeState.Dragging)
                return;

            float rowHeight = Mathf.Max(1f, row.layout.height);
            UpdateDropTarget(item, evt.localMousePosition.y, rowHeight);
            TryAutoExpandDuringDrag(item);
        }

        private void HandleRowMouseUp(MouseUpEvent evt)
        {
            if (evt == null || evt.button != 0)
                return;

            if (dragState == DragRuntimeState.Dragging)
            {
                CommitDragDrop();
                evt.StopImmediatePropagation();
                return;
            }

            CancelPendingDrag();
        }

        private void HandleTreeMouseUp(MouseUpEvent evt)
        {
            if (evt == null || evt.button != 0)
                return;

            if (dragState == DragRuntimeState.Dragging)
            {
                CommitDragDrop();
                evt.StopImmediatePropagation();
                return;
            }

            CancelPendingDrag();
        }

        private void BeginDragPending(ActionBlockTreeItem item)
        {
            dragState = DragRuntimeState.Pending;
            pendingDragItem = item;
            pendingDragStartTime = EditorApplication.timeSinceStartup;
            dragSourceListPropertyPath = string.Empty;
            dragSourceIndices.Clear();
            currentDropTarget = default;
            hoverExpandItem = null;
            hoverExpandStartTime = 0d;
        }

        private void BeginDragging()
        {
            if (pendingDragItem == null || pendingDragItem.Kind != ActionBlockTreeItemKind.Step)
            {
                CancelDragState();
                return;
            }

            string sourceListPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(pendingDragItem.PropertyPath);
            if (string.IsNullOrWhiteSpace(sourceListPath))
            {
                CancelDragState();
                return;
            }

            dragState = DragRuntimeState.Dragging;
            dragSourceListPropertyPath = sourceListPath;
            dragSourceIndices.Clear();

            List<ActionBlockTreeItem> selectedItems = GetSelectedStepItemsInTreeOrder();
            bool includeMultiSelection = selectionState.IsStepSelected(pendingDragItem) && selectedItems.Count > 1;

            if (includeMultiSelection)
            {
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    ActionBlockTreeItem selectedItem = selectedItems[i];
                    string selectedListPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(selectedItem.PropertyPath);

                    if (!string.Equals(selectedListPropertyPath, sourceListPath, StringComparison.Ordinal))
                        continue;

                    dragSourceIndices.Add(selectedItem.StepIndex);
                }
            }

            if (dragSourceIndices.Count <= 0)
                dragSourceIndices.Add(pendingDragItem.StepIndex);

            pendingDragItem = null;
            currentDropTarget = default;
            hoverExpandItem = null;
            hoverExpandStartTime = 0d;
        }

        private void UpdateDropTarget(ActionBlockTreeItem hoverItem, float localY, float rowHeight)
        {
            ActionDropTarget nextTarget = BuildDropTarget(hoverItem, localY, rowHeight);

            if (AreSameDropTarget(currentDropTarget, nextTarget))
                return;

            currentDropTarget = nextTarget;
            RebuildTreeRows();
            UpdateFooter();
        }

        private ActionDropTarget BuildDropTarget(ActionBlockTreeItem hoverItem, float localY, float rowHeight)
        {
            if (dragState != DragRuntimeState.Dragging || hoverItem == null)
                return default;

            if (hoverItem.Kind == ActionBlockTreeItemKind.Step)
            {
                string targetListPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(hoverItem.PropertyPath);
                if (string.IsNullOrWhiteSpace(targetListPath))
                    return default;

                float ratio = Mathf.Clamp01(localY / Mathf.Max(1f, rowHeight));
                DropPlacement placement = ratio < DropEdgeRatio
                    ? DropPlacement.BeforeStep
                    : DropPlacement.AfterStep;
                int insertIndex = hoverItem.StepIndex + (placement == DropPlacement.AfterStep ? 1 : 0);

                ActionDropTarget candidate = new(
                    targetListPath,
                    insertIndex,
                    ResolveInlineActionPropertyPathFromListPath(targetListPath),
                    hoverItem.PropertyPath,
                    hoverItem.Kind,
                    placement);

                return IsValidDropTarget(candidate)
                    ? candidate
                    : default;
            }

            if (hoverItem.Kind == ActionBlockTreeItemKind.Block || hoverItem.Kind == ActionBlockTreeItemKind.Branch)
            {
                string targetListPath = ResolveInlineActionStepsListPath(hoverItem.PropertyPath);
                if (string.IsNullOrWhiteSpace(targetListPath))
                    return default;

                int insertIndex = GetStepCountFromListPath(targetListPath);
                ActionDropTarget candidate = new(
                    targetListPath,
                    insertIndex,
                    hoverItem.PropertyPath,
                    hoverItem.PropertyPath,
                    hoverItem.Kind,
                    DropPlacement.IntoContainer);

                return IsValidDropTarget(candidate)
                    ? candidate
                    : default;
            }

            return default;
        }

        private bool IsValidDropTarget(ActionDropTarget target)
        {
            if (!target.IsValid)
                return false;

            if (string.IsNullOrWhiteSpace(dragSourceListPropertyPath) || dragSourceIndices.Count <= 0)
                return false;

            if (!string.Equals(target.ListPropertyPath, dragSourceListPropertyPath, StringComparison.Ordinal))
                return true;

            if (dragSourceIndices.Count == 1)
            {
                int sourceIndex = dragSourceIndices[0];
                return target.InsertIndex != sourceIndex && target.InsertIndex != sourceIndex + 1;
            }

            return true;
        }

        private void TryAutoExpandDuringDrag(ActionBlockTreeItem hoverItem)
        {
            if (dragState != DragRuntimeState.Dragging || hoverItem == null)
                return;

            if (!hoverItem.CanExpand || selectionState.IsExpanded(hoverItem))
            {
                hoverExpandItem = null;
                hoverExpandStartTime = 0d;
                return;
            }

            if (!IsSameTreeItem(hoverExpandItem, hoverItem))
            {
                hoverExpandItem = hoverItem;
                hoverExpandStartTime = EditorApplication.timeSinceStartup;
                return;
            }

            if (EditorApplication.timeSinceStartup - hoverExpandStartTime < DragHoverAutoExpandSeconds)
                return;

            selectionState.SetExpanded(hoverItem, true);
            hoverExpandItem = null;
            hoverExpandStartTime = 0d;
            RebuildTreeRows();
        }

        private void CommitDragDrop()
        {
            if (dragState != DragRuntimeState.Dragging)
            {
                CancelDragState();
                return;
            }

            ActionDropTarget target = currentDropTarget;
            bool hasValidTarget = target.IsValid &&
                                  !string.IsNullOrWhiteSpace(dragSourceListPropertyPath) &&
                                  dragSourceIndices.Count > 0 &&
                                  serializedObjectBridge.SerializedObject != null;

            if (!hasValidTarget)
            {
                CancelDragState();
                RebuildTreeRows();
                UpdateFooter();
                return;
            }

            List<int> sortedIndices = new(dragSourceIndices);
            sortedIndices.Sort();

            int finalInsertIndex = Mathf.Max(0, target.InsertIndex);

            if (string.Equals(dragSourceListPropertyPath, target.ListPropertyPath, StringComparison.Ordinal))
            {
                int removedBeforeInsert = 0;

                for (int i = 0; i < sortedIndices.Count; i++)
                {
                    if (sortedIndices[i] < finalInsertIndex)
                        removedBeforeInsert++;
                }

                finalInsertIndex = Mathf.Max(0, finalInsertIndex - removedBeforeInsert);
            }

            pendingSelectStepPropertyPaths.Clear();

            for (int i = 0; i < sortedIndices.Count; i++)
                pendingSelectStepPropertyPaths.Add(BuildStepPropertyPath(target.ListPropertyPath, finalInsertIndex + i));

            ActionStepManagedReferenceUtility.MoveStepsBetweenLists(
                serializedObjectBridge.SerializedObject.targetObjects,
                dragSourceListPropertyPath,
                sortedIndices,
                target.ListPropertyPath,
                target.InsertIndex);

            CancelDragState();
            RefreshAfterMutation();
        }

        private void CancelPendingDrag()
        {
            if (dragState != DragRuntimeState.Pending)
                return;

            CancelDragState();
        }

        private void CancelDragState()
        {
            dragState = DragRuntimeState.None;
            pendingDragItem = null;
            pendingDragStartTime = 0d;
            dragSourceListPropertyPath = string.Empty;
            dragSourceIndices.Clear();
            currentDropTarget = default;
            hoverExpandItem = null;
            hoverExpandStartTime = 0d;
        }

        private static string BuildStepPropertyPath(string listPropertyPath, int index)
        {
            return string.IsNullOrWhiteSpace(listPropertyPath)
                ? string.Empty
                : $"{listPropertyPath}.Array.data[{Mathf.Max(0, index)}]";
        }

        private string ResolveInlineActionStepsListPath(string inlineActionPropertyPath)
        {
            if (string.IsNullOrWhiteSpace(inlineActionPropertyPath) || serializedObjectBridge.SerializedObject == null)
                return string.Empty;

            SerializedObject serializedObject = serializedObjectBridge.SerializedObject;
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty inlineActionProperty = serializedObject.FindProperty(inlineActionPropertyPath);
            SerializedProperty stepsProperty = inlineActionProperty?.FindPropertyRelative("_steps");
            return stepsProperty != null && stepsProperty.isArray
                ? stepsProperty.propertyPath
                : string.Empty;
        }

        private int GetStepCountFromListPath(string listPropertyPath)
        {
            if (string.IsNullOrWhiteSpace(listPropertyPath) || serializedObjectBridge.SerializedObject == null)
                return 0;

            SerializedObject serializedObject = serializedObjectBridge.SerializedObject;
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty listProperty = serializedObject.FindProperty(listPropertyPath);
            return listProperty != null && listProperty.isArray
                ? listProperty.arraySize
                : 0;
        }

        private static string ResolveInlineActionPropertyPathFromListPath(string listPropertyPath)
        {
            const string suffix = "._steps";
            return !string.IsNullOrWhiteSpace(listPropertyPath) && listPropertyPath.EndsWith(suffix, StringComparison.Ordinal)
                ? listPropertyPath.Substring(0, listPropertyPath.Length - suffix.Length)
                : string.Empty;
        }

        private static bool IsSameTreeItem(ActionBlockTreeItem left, ActionBlockTreeItem right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            return left.Kind == right.Kind &&
                   left.StepIndex == right.StepIndex &&
                   left.BranchKey.Equals(right.BranchKey) &&
                   string.Equals(left.PropertyPath, right.PropertyPath, StringComparison.Ordinal);
        }

        private static bool AreSameDropTarget(ActionDropTarget left, ActionDropTarget right)
        {
            return left.InsertIndex == right.InsertIndex &&
                   left.VisualKind == right.VisualKind &&
                   left.Placement == right.Placement &&
                   string.Equals(left.ListPropertyPath, right.ListPropertyPath, StringComparison.Ordinal) &&
                   string.Equals(left.VisualPropertyPath, right.VisualPropertyPath, StringComparison.Ordinal);
        }

        private void ApplyDropIndicatorStyle(VisualElement row, ActionBlockTreeItem item)
        {
            if (row == null || item == null)
                return;

            row.style.borderTopWidth = 0f;
            row.style.borderBottomWidth = 0f;
            row.style.borderLeftWidth = 0f;
            row.style.borderTopColor = Color.clear;
            row.style.borderBottomColor = Color.clear;
            row.style.borderLeftColor = Color.clear;

            if (dragState != DragRuntimeState.Dragging || !currentDropTarget.IsValid)
                return;

            if (currentDropTarget.VisualKind != item.Kind ||
                !string.Equals(currentDropTarget.VisualPropertyPath, item.PropertyPath, StringComparison.Ordinal))
            {
                return;
            }

            switch (currentDropTarget.Placement)
            {
                case DropPlacement.BeforeStep:
                    row.style.borderTopWidth = 2f;
                    row.style.borderTopColor = DropIndicatorColor;
                    break;

                case DropPlacement.AfterStep:
                    row.style.borderBottomWidth = 2f;
                    row.style.borderBottomColor = DropIndicatorColor;
                    break;

                case DropPlacement.IntoContainer:
                    row.style.borderLeftWidth = 3f;
                    row.style.borderLeftColor = DropIndicatorColor;
                    break;
            }
        }

        private void HandleTreeKeyDown(KeyDownEvent evt)
        {
            if (evt == null)
                return;

            // Mirror the inline inspector shortcuts so selection behaves the same in both editing surfaces.
            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    if (DeleteSelectedItem())
                        evt.StopImmediatePropagation();
                    return;

                case KeyCode.C:
                    if (IsActionShortcut(evt) && CopySelectedStep())
                        evt.StopImmediatePropagation();
                    return;

                case KeyCode.V:
                    if (IsActionShortcut(evt) && PasteIntoSelection())
                        evt.StopImmediatePropagation();
                    return;
            }
        }

        private void SelectItem(ActionBlockTreeItem item)
        {
            selectionState.Select(item);
            RefreshSelectionView();
        }

        private void ApplyStepSelectionFromMouse(ActionBlockTreeItem item, bool shiftSelect, bool toggleSelect)
        {
            if (item == null || item.Kind != ActionBlockTreeItemKind.Step)
                return;

            if (shiftSelect && TryBuildRangeSelection(item, out List<ActionBlockTreeItem> rangeSelection, out ActionBlockTreeItem anchorItem))
            {
                selectionState.SetStepSelection(rangeSelection, item, anchorItem);
                RefreshSelectionView();
                return;
            }

            if (toggleSelect)
            {
                List<ActionBlockTreeItem> selectedItems = GetSelectedStepItemsInTreeOrder();
                bool isSelected = selectionState.IsStepSelected(item);

                if (isSelected)
                {
                    for (int i = selectedItems.Count - 1; i >= 0; i--)
                    {
                        if (!string.Equals(selectedItems[i].PropertyPath, item.PropertyPath, StringComparison.Ordinal))
                            continue;

                        selectedItems.RemoveAt(i);
                    }
                }
                else
                {
                    selectedItems.Add(item);
                }

                ActionBlockTreeItem focusedItem = selectedItems.Count > 0
                    ? selectedItems[selectedItems.Count - 1]
                    : null;
                ActionBlockTreeItem anchor = ResolveAnchorItem(selectedItems, focusedItem);

                selectionState.SetStepSelection(selectedItems, focusedItem, anchor);
                RefreshSelectionView();
                return;
            }

            selectionState.Select(item);
            RefreshSelectionView();
        }

        private bool TryBuildRangeSelection(
            ActionBlockTreeItem targetItem,
            out List<ActionBlockTreeItem> rangeSelection,
            out ActionBlockTreeItem anchorItem)
        {
            rangeSelection = new List<ActionBlockTreeItem>();
            anchorItem = null;

            if (targetItem == null || targetItem.Kind != ActionBlockTreeItemKind.Step)
                return false;

            if (!selectionState.TryGetStepAnchor(out string anchorListPropertyPath, out int anchorIndex))
                return false;

            string targetListPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(targetItem.PropertyPath);

            if (!string.Equals(anchorListPropertyPath, targetListPropertyPath, StringComparison.Ordinal))
                return false;

            int minIndex = Mathf.Min(anchorIndex, targetItem.StepIndex);
            int maxIndex = Mathf.Max(anchorIndex, targetItem.StepIndex);
            List<ActionBlockTreeItem> stepItems = GetStepItemsInTreeOrder();

            for (int i = 0; i < stepItems.Count; i++)
            {
                ActionBlockTreeItem stepItem = stepItems[i];
                string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(stepItem.PropertyPath);

                if (!string.Equals(listPropertyPath, anchorListPropertyPath, StringComparison.Ordinal))
                    continue;

                if (stepItem.StepIndex < minIndex || stepItem.StepIndex > maxIndex)
                    continue;

                rangeSelection.Add(stepItem);

                if (stepItem.StepIndex == anchorIndex)
                    anchorItem = stepItem;
            }

            return rangeSelection.Count > 0;
        }

        private ActionBlockTreeItem ResolveAnchorItem(List<ActionBlockTreeItem> selectedItems, ActionBlockTreeItem fallback)
        {
            if (selectionState.TryGetStepAnchor(out string anchorListPropertyPath, out int anchorIndex))
            {
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    ActionBlockTreeItem selectedItem = selectedItems[i];
                    string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(selectedItem.PropertyPath);

                    if (!string.Equals(listPropertyPath, anchorListPropertyPath, StringComparison.Ordinal))
                        continue;

                    if (selectedItem.StepIndex == anchorIndex)
                        return selectedItem;
                }
            }

            return fallback;
        }

        private void RefreshSelectionView()
        {
            RebuildTreeRows();
            BindDetailPane();
            detailToolbarContainer?.MarkDirtyRepaint();
            UpdateFooter();
        }

        private void ShowContextMenu(ActionBlockTreeItem item)
        {
            if (item == null)
                return;

            ContextMenuBuilder menu = new();

            if (item.Kind == ActionBlockTreeItemKind.Step)
                PopulateStepContextMenu(menu, item);
            else
                PopulateContainerContextMenu(menu, item);

            menu.ShowAsContext();
        }

        private void PopulateStepContextMenu(ContextMenuBuilder menu, ActionBlockTreeItem item)
        {
            SerializedProperty stepProperty = FindItemProperty(item);
            string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(item.PropertyPath);
            UnityEngine.Object[] targets = serializedObjectBridge.SerializedObject?.targetObjects;
            bool canMutate = targets != null && !string.IsNullOrWhiteSpace(listPropertyPath);

            // Step rows insert relative to the selected step, matching the inline inspector context menu.
            AddStepMenuItems(menu, stepType =>
            {
                ActionStepManagedReferenceUtility.AddStep(targets, listPropertyPath, stepType, item.StepIndex + 1);
                RefreshAfterMutation();
            }, canMutate);

            menu.AddSeparator();
            menu.AddItem(
                "Delete",
                canMutate,
                () =>
                {
                    if (selectionState.IsStepSelected(item) && selectionState.SelectedStepCount > 1)
                    {
                        DeleteSelectedSteps();
                        return;
                    }

                    PrepareForDestructiveMutation();

                    ActionStepManagedReferenceUtility.DeleteStep(targets, listPropertyPath, item.StepIndex);
                    RefreshAfterMutation();
                });
            menu.AddItem(
                "Copy",
                stepProperty?.managedReferenceValue != null,
                () => ActionStepManagedReferenceUtility.CopyStep(stepProperty));
            menu.AddItem(
                "Paste",
                canMutate && ActionStepManagedReferenceUtility.CanPasteStep(),
                () =>
                {
                    ActionStepManagedReferenceUtility.PasteStep(targets, listPropertyPath, item.StepIndex + 1);
                    RefreshAfterMutation();
                });
        }

        private void PopulateContainerContextMenu(ContextMenuBuilder menu, ActionBlockTreeItem item)
        {
            bool canAdd = !string.IsNullOrWhiteSpace(item.PropertyPath) && serializedObjectBridge.SerializedObject != null;

            // Block and branch rows treat the selected inline action as the insertion target.
            AddStepMenuItems(menu, stepType =>
            {
                AddStepToInlineAction(item.PropertyPath, stepType);
                RefreshAfterMutation();
            }, canAdd);

            menu.AddSeparator();
            menu.AddItem(
                "Delete",
                CanDeleteContainer(item),
                () =>
                {
                    DeleteContainer(item);
                    RefreshAfterMutation();
                });
            menu.AddItem(
                "Paste",
                canAdd && ActionStepManagedReferenceUtility.CanPasteStep(),
                () =>
                {
                    PasteStepToInlineAction(item.PropertyPath);
                    RefreshAfterMutation();
                });
        }

        private void AddStepMenuItems(ContextMenuBuilder menu, Action<Type> addStep, bool enabled)
        {
            IReadOnlyList<Type> stepTypes = ActionStepManagedReferenceUtility.GetStepTypes();

            ActionStepRecentSelectionUtility.AppendRecentStepMenuItems(
                menu,
                "Add Step",
                enabled,
                stepType => addStep?.Invoke(stepType));

            if (stepTypes.Count == 0)
            {
                menu.AddItem("Add Step/No matching items", false, null);
                return;
            }

            for (int i = 0; i < stepTypes.Count; i++)
            {
                Type stepType = stepTypes[i];
                string label = $"Add Step/{ActionStepManagedReferenceUtility.GetStepTypeLabel(stepType)}";
                menu.AddItem(label, enabled, () => addStep?.Invoke(stepType));
            }
        }

        private bool DeleteSelectedItem()
        {
            if (!TryFindSelectedItem(out ActionBlockTreeItem item))
                return false;

            if (item.Kind == ActionBlockTreeItemKind.Step)
            {
                return DeleteSelectedSteps();
            }

            if (!CanDeleteContainer(item))
                return false;

            DeleteContainer(item);
            RefreshAfterMutation();
            return true;
        }

        private bool DeleteSelectedSteps()
        {
            SerializedObject serializedObject = serializedObjectBridge.SerializedObject;

            if (serializedObject == null)
                return false;

            List<ActionBlockTreeItem> selectedStepItems = GetSelectedStepItemsInTreeOrder();

            if (selectedStepItems.Count <= 0)
                return false;

            Dictionary<string, List<int>> groupedIndices = new(StringComparer.Ordinal);

            for (int i = 0; i < selectedStepItems.Count; i++)
            {
                ActionBlockTreeItem stepItem = selectedStepItems[i];
                string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(stepItem.PropertyPath);

                if (string.IsNullOrWhiteSpace(listPropertyPath))
                    continue;

                if (!groupedIndices.TryGetValue(listPropertyPath, out List<int> indices))
                {
                    indices = new List<int>();
                    groupedIndices.Add(listPropertyPath, indices);
                }

                indices.Add(stepItem.StepIndex);
            }

            if (groupedIndices.Count <= 0)
                return false;

            PrepareForDestructiveMutation();

            foreach (KeyValuePair<string, List<int>> pair in groupedIndices)
                ActionStepManagedReferenceUtility.DeleteSteps(serializedObject.targetObjects, pair.Key, pair.Value);

            RefreshAfterMutation();
            return true;
        }

        private bool CopySelectedStep()
        {
            if (!TryFindSelectedItem(out ActionBlockTreeItem item) || item.Kind != ActionBlockTreeItemKind.Step)
                return false;

            List<ActionBlockTreeItem> selectedStepItems = GetSelectedStepItemsInTreeOrder();

            if (selectionState.IsStepSelected(item) && selectedStepItems.Count > 1)
            {
                List<SerializedProperty> stepProperties = new(selectedStepItems.Count);

                for (int i = 0; i < selectedStepItems.Count; i++)
                {
                    SerializedProperty selectedProperty = FindItemProperty(selectedStepItems[i]);

                    if (selectedProperty?.managedReferenceValue != null)
                        stepProperties.Add(selectedProperty);
                }

                if (stepProperties.Count <= 0)
                    return false;

                ActionStepManagedReferenceUtility.CopySteps(stepProperties);
                return true;
            }

            SerializedProperty stepProperty = FindItemProperty(item);

            if (stepProperty?.managedReferenceValue == null)
                return false;

            ActionStepManagedReferenceUtility.CopyStep(stepProperty);
            return true;
        }

        private bool PasteIntoSelection()
        {
            if (!ActionStepManagedReferenceUtility.CanPasteStep() || !TryFindSelectedItem(out ActionBlockTreeItem item))
                return false;

            if (item.Kind == ActionBlockTreeItemKind.Step)
            {
                string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(item.PropertyPath);

                if (string.IsNullOrWhiteSpace(listPropertyPath) || serializedObjectBridge.SerializedObject == null)
                    return false;

                ActionStepManagedReferenceUtility.PasteStep(
                    serializedObjectBridge.SerializedObject.targetObjects,
                    listPropertyPath,
                    item.StepIndex + 1);
                RefreshAfterMutation();
                return true;
            }

            if (string.IsNullOrWhiteSpace(item.PropertyPath))
                return false;

            PasteStepToInlineAction(item.PropertyPath);
            RefreshAfterMutation();
            return true;
        }

        private bool CanDeleteContainer(ActionBlockTreeItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.PropertyPath))
                return false;

            if (item.Kind == ActionBlockTreeItemKind.Branch)
                return true;

            SerializedProperty stepsProperty = FindInlineActionStepsProperty(item.PropertyPath);
            return stepsProperty != null && stepsProperty.isArray && stepsProperty.arraySize > 0;
        }

        private void DeleteContainer(ActionBlockTreeItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.PropertyPath))
                return;

            PrepareForDestructiveMutation();

            if (item.Kind == ActionBlockTreeItemKind.Branch)
            {
                ActionStepManagedReferenceUtility.ClearInlineAction(
                    serializedObjectBridge.SerializedObject.targetObjects,
                    item.PropertyPath);
                return;
            }

            SerializedProperty stepsProperty = FindInlineActionStepsProperty(item.PropertyPath);

            if (stepsProperty == null)
                return;

            ActionStepManagedReferenceUtility.ClearSteps(stepsProperty.serializedObject.targetObjects, stepsProperty.propertyPath);
        }

        private void AddStepToInlineAction(string inlineActionPropertyPath, Type stepType)
        {
            SerializedProperty stepsProperty = EnsureInlineActionStepsProperty(inlineActionPropertyPath);

            if (stepsProperty == null)
                return;

            ActionStepManagedReferenceUtility.AddStep(
                stepsProperty.serializedObject.targetObjects,
                stepsProperty.propertyPath,
                stepType);
        }

        private void PasteStepToInlineAction(string inlineActionPropertyPath)
        {
            SerializedProperty stepsProperty = EnsureInlineActionStepsProperty(inlineActionPropertyPath);

            if (stepsProperty == null)
                return;

            ActionStepManagedReferenceUtility.PasteStep(
                stepsProperty.serializedObject.targetObjects,
                stepsProperty.propertyPath);
        }

        private SerializedProperty FindInlineActionStepsProperty(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath) || serializedObjectBridge.SerializedObject == null)
                return null;

            SerializedObject serializedObject = serializedObjectBridge.SerializedObject;
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty inlineActionProperty = serializedObject.FindProperty(propertyPath);
            return inlineActionProperty?.FindPropertyRelative("_steps");
        }

        private void PrepareForDestructiveMutation()
        {
            // Clearing the selection avoids stale detail bindings after delete/clear operations.
            selectionState.Clear();
            detailBridge?.Clear("Select a block, step, or branch to edit.");
            detailToolbarContainer?.MarkDirtyRepaint();
        }

        private void RefreshAfterMutation()
        {
            RefreshWindow();
            hasPendingDetailRefresh = true;
        }

        private static bool IsActionShortcut(KeyDownEvent evt)
        {
            return evt != null && (evt.ctrlKey || evt.commandKey);
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
            label.style.minWidth = IndexColumnWidth;
            label.style.marginRight = IndexColumnSpacing;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            return label;
        }

        private static void AddIndentGuides(VisualElement row, int depth)
        {
            if (row == null || depth <= 0)
                return;

            // Use lightweight vertical guides so nested child actions read like an indented tree instead of a flat list.
            for (int level = 0; level < depth; level++)
            {
                VisualElement guide = new();
                guide.pickingMode = PickingMode.Ignore;
                guide.style.position = Position.Absolute;
                guide.style.left = IndentGuideOffset + (level * TreeIndentWidth);
                guide.style.top = 0f;
                guide.style.bottom = 0f;
                guide.style.width = 1f;
                guide.style.backgroundColor = ResolveIndentGuideColor();
                row.Add(guide);
            }
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

            // Containers edit their owned `_steps` list, while step rows edit the step itself.
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
            string selectionCountLabel = selectionState.Kind == ActionWindowSelectionKind.Step
                ? $" ({selectionState.SelectedStepCount} selected)"
                : string.Empty;

            string dragStatus = dragState == DragRuntimeState.Dragging
                ? currentDropTarget.IsValid
                    ? $"Dragging: {dragSourceListPropertyPath}[{FormatDragSourceIndices()}] -> {currentDropTarget.ListPropertyPath}[{currentDropTarget.InsertIndex}]"
                    : "Dragging: no drop target"
                : "Dragging: inactive";

            footerLabel.text =
                $"Selection: {selectionPath}{selectionCountLabel} | Validation: {validationCount} | Missing branches: {missingBranchCount} | Dirty: {(isBoundTargetDirty ? "Yes" : "No")} | {dragStatus}";
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
            if (item == null)
                return false;

            if (item.Kind == ActionBlockTreeItemKind.Step)
                return selectionState.IsStepSelected(item);

            return item.BranchKey.Equals(selectionState.BranchKey) &&
                   MapSelectionKind(item.Kind) == selectionState.Kind;
        }

        private List<ActionBlockTreeItem> GetStepItemsInTreeOrder()
        {
            List<ActionBlockTreeItem> stepItems = new();

            for (int i = 0; i < treeViewModel.Items.Count; i++)
            {
                ActionBlockTreeItem item = treeViewModel.Items[i];

                if (item.Kind == ActionBlockTreeItemKind.Step)
                    stepItems.Add(item);
            }

            return stepItems;
        }

        private List<ActionBlockTreeItem> GetSelectedStepItemsInTreeOrder()
        {
            List<ActionBlockTreeItem> selectedItems = new();
            List<ActionBlockTreeItem> stepItems = GetStepItemsInTreeOrder();

            for (int i = 0; i < stepItems.Count; i++)
            {
                ActionBlockTreeItem stepItem = stepItems[i];

                if (selectionState.IsStepSelected(stepItem))
                    selectedItems.Add(stepItem);
            }

            return selectedItems;
        }

        private string FormatDragSourceIndices()
        {
            if (dragSourceIndices.Count <= 0)
                return string.Empty;

            List<int> sorted = new(dragSourceIndices);
            sorted.Sort();

            if (sorted.Count == 1)
                return sorted[0].ToString();

            return $"{sorted[0]}..{sorted[sorted.Count - 1]} ({sorted.Count})";
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

        private static Color ResolveIndentGuideColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.12f)
                : new Color(0f, 0f, 0f, 0.18f);
        }
    }
}
