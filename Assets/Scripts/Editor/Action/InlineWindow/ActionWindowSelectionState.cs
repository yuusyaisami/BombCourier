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

        public ActionWindowSelectionKind Kind { get; private set; }
        public ActionBranchKey BranchKey { get; private set; }
        public int StepIndex { get; private set; } = -1;
        public string PropertyPath { get; private set; }

        public void Clear()
        {
            Kind = ActionWindowSelectionKind.None;
            BranchKey = default;
            StepIndex = -1;
            PropertyPath = string.Empty;
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
        }

        private static string GetExpansionKey(ActionBlockTreeItem item)
        {
            return $"{item.Kind}:{item.BranchKey}";
        }
    }
}
