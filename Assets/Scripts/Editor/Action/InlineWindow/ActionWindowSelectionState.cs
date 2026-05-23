using System;
using System.Collections.Generic;

namespace BC.Editor.ActionSystem
{
    public enum ActionWindowSelectionKind
    {
        None = 0,
        Block = 1,
        Step = 2,
        Branch = 3,
    }

    public sealed class ActionWindowSelectionState
    {
        // Collapse state is keyed by the stable branch key so reordering steps does not reopen unrelated blocks.
        private readonly HashSet<string> collapsedItemKeys = new(StringComparer.Ordinal);
        private readonly HashSet<string> selectedStepPropertyPaths = new(StringComparer.Ordinal);

        public ActionWindowSelectionKind Kind { get; private set; }
        public ActionBranchKey BranchKey { get; private set; }
        public int StepIndex { get; private set; } = -1;
        public string PropertyPath { get; private set; }
        public string AnchorListPropertyPath { get; private set; }
        public int AnchorStepIndex { get; private set; } = -1;
        public int SelectedStepCount => selectedStepPropertyPaths.Count;

        public void Clear()
        {
            Kind = ActionWindowSelectionKind.None;
            BranchKey = default;
            StepIndex = -1;
            PropertyPath = string.Empty;
            AnchorListPropertyPath = string.Empty;
            AnchorStepIndex = -1;
            selectedStepPropertyPaths.Clear();
        }

        public bool IsExpanded(ActionBlockTreeItem item)
        {
            if (item == null || !item.CanExpand)
                return false;

            return !collapsedItemKeys.Contains(GetExpansionKey(item));
        }

        public void SetExpanded(ActionBlockTreeItem item, bool expanded)
        {
            if (item == null || !item.CanExpand)
                return;

            string key = GetExpansionKey(item);

            if (expanded)
                collapsedItemKeys.Remove(key);
            else
                collapsedItemKeys.Add(key);
        }

        public void ToggleExpanded(ActionBlockTreeItem item)
        {
            if (item == null || !item.CanExpand)
                return;

            SetExpanded(item, !IsExpanded(item));
        }

        public void Select(ActionBlockTreeItem item)
        {
            if (item == null)
            {
                Clear();
                return;
            }

            Kind = item.Kind switch
            {
                ActionBlockTreeItemKind.Block => ActionWindowSelectionKind.Block,
                ActionBlockTreeItemKind.Step => ActionWindowSelectionKind.Step,
                ActionBlockTreeItemKind.Branch => ActionWindowSelectionKind.Branch,
                _ => ActionWindowSelectionKind.None,
            };

            BranchKey = item.BranchKey;
            StepIndex = item.StepIndex;
            PropertyPath = item.PropertyPath;

            if (Kind == ActionWindowSelectionKind.Step)
            {
                selectedStepPropertyPaths.Clear();

                if (!string.IsNullOrWhiteSpace(item.PropertyPath))
                    selectedStepPropertyPaths.Add(item.PropertyPath);

                AnchorListPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(item.PropertyPath);
                AnchorStepIndex = item.StepIndex;
            }
            else
            {
                selectedStepPropertyPaths.Clear();
                AnchorListPropertyPath = string.Empty;
                AnchorStepIndex = -1;
            }
        }

        public void SetStepSelection(
            IReadOnlyList<ActionBlockTreeItem> selectedItems,
            ActionBlockTreeItem focusedItem,
            ActionBlockTreeItem anchorItem)
        {
            selectedStepPropertyPaths.Clear();

            if (selectedItems != null)
            {
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    ActionBlockTreeItem item = selectedItems[i];

                    if (item == null || item.Kind != ActionBlockTreeItemKind.Step || string.IsNullOrWhiteSpace(item.PropertyPath))
                        continue;

                    selectedStepPropertyPaths.Add(item.PropertyPath);
                }
            }

            if (focusedItem == null || focusedItem.Kind != ActionBlockTreeItemKind.Step ||
                string.IsNullOrWhiteSpace(focusedItem.PropertyPath) ||
                !selectedStepPropertyPaths.Contains(focusedItem.PropertyPath))
            {
                Kind = ActionWindowSelectionKind.None;
                BranchKey = default;
                StepIndex = -1;
                PropertyPath = string.Empty;
            }
            else
            {
                Kind = ActionWindowSelectionKind.Step;
                BranchKey = focusedItem.BranchKey;
                StepIndex = focusedItem.StepIndex;
                PropertyPath = focusedItem.PropertyPath;
            }

            if (anchorItem != null && anchorItem.Kind == ActionBlockTreeItemKind.Step)
            {
                AnchorListPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(anchorItem.PropertyPath);
                AnchorStepIndex = anchorItem.StepIndex;
            }
            else if (focusedItem != null && focusedItem.Kind == ActionBlockTreeItemKind.Step)
            {
                AnchorListPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(focusedItem.PropertyPath);
                AnchorStepIndex = focusedItem.StepIndex;
            }
            else
            {
                AnchorListPropertyPath = string.Empty;
                AnchorStepIndex = -1;
            }
        }

        public void SyncStepSelection(IReadOnlyList<ActionBlockTreeItem> stepItemsInTreeOrder)
        {
            if (selectedStepPropertyPaths.Count == 0)
                return;

            HashSet<string> availableStepPaths = new(StringComparer.Ordinal);

            if (stepItemsInTreeOrder != null)
            {
                for (int i = 0; i < stepItemsInTreeOrder.Count; i++)
                {
                    ActionBlockTreeItem item = stepItemsInTreeOrder[i];

                    if (item == null || item.Kind != ActionBlockTreeItemKind.Step || string.IsNullOrWhiteSpace(item.PropertyPath))
                        continue;

                    availableStepPaths.Add(item.PropertyPath);
                }
            }

            selectedStepPropertyPaths.RemoveWhere(path => !availableStepPaths.Contains(path));

            if (selectedStepPropertyPaths.Count == 0)
            {
                Kind = ActionWindowSelectionKind.None;
                BranchKey = default;
                StepIndex = -1;
                PropertyPath = string.Empty;
                AnchorListPropertyPath = string.Empty;
                AnchorStepIndex = -1;
                return;
            }

            if (Kind != ActionWindowSelectionKind.Step || string.IsNullOrWhiteSpace(PropertyPath) ||
                !selectedStepPropertyPaths.Contains(PropertyPath))
            {
                for (int i = 0; i < stepItemsInTreeOrder.Count; i++)
                {
                    ActionBlockTreeItem item = stepItemsInTreeOrder[i];

                    if (item == null || !selectedStepPropertyPaths.Contains(item.PropertyPath))
                        continue;

                    Kind = ActionWindowSelectionKind.Step;
                    BranchKey = item.BranchKey;
                    StepIndex = item.StepIndex;
                    PropertyPath = item.PropertyPath;
                    break;
                }
            }
        }

        public bool IsStepSelected(ActionBlockTreeItem item)
        {
            return item != null &&
                   item.Kind == ActionBlockTreeItemKind.Step &&
                   !string.IsNullOrWhiteSpace(item.PropertyPath) &&
                   selectedStepPropertyPaths.Contains(item.PropertyPath);
        }

        public bool TryGetStepAnchor(out string listPropertyPath, out int stepIndex)
        {
            listPropertyPath = AnchorListPropertyPath;
            stepIndex = AnchorStepIndex;
            return !string.IsNullOrWhiteSpace(AnchorListPropertyPath) && AnchorStepIndex >= 0;
        }

        private static string GetExpansionKey(ActionBlockTreeItem item)
        {
            return $"{item.Kind}:{item.BranchKey}";
        }
    }
}
