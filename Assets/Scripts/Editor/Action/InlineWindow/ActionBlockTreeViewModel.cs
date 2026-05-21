using System;
using System.Collections.Generic;
using BC.ActionSystem;
using UnityEditor;

namespace BC.Editor.Action
{
    public enum ActionBlockTreeItemKind
    {
        Block = 0,
        Step = 1,
        Branch = 2,
    }

    public sealed class ActionBlockTreeItem
    {
        internal ActionBlockTreeItem(
            ActionBlockTreeItemKind kind,
            int depth,
            string title,
            string badge,
            ActionBranchKey branchKey,
            int stepIndex,
            ActionStepAuthoring step,
            ActionChildSlotDescriptor childSlot,
            string propertyPath,
            bool isMissing)
        {
            Kind = kind;
            Depth = depth;
            Title = title ?? string.Empty;
            Badge = badge ?? string.Empty;
            BranchKey = branchKey;
            StepIndex = stepIndex;
            Step = step;
            ChildSlot = childSlot;
            PropertyPath = propertyPath ?? string.Empty;
            IsMissing = isMissing;
        }

        public ActionBlockTreeItemKind Kind { get; }
        public int Depth { get; }
        public string Title { get; }
        public string Badge { get; }
        public ActionBranchKey BranchKey { get; }
        public int StepIndex { get; }
        public ActionStepAuthoring Step { get; }
        public ActionChildSlotDescriptor ChildSlot { get; }
        public string PropertyPath { get; }
        public bool IsMissing { get; }
    }

    public sealed class ActionBlockTreeViewModel
    {
        private const int MaxDepth = 64;

        private readonly List<ActionBlockTreeItem> items = new();

        public IReadOnlyList<ActionBlockTreeItem> Items => items;

        public void Rebuild(SerializedProperty rootActionProperty)
        {
            if (rootActionProperty == null)
                throw new ArgumentNullException(nameof(rootActionProperty));

            Rebuild(rootActionProperty.propertyPath, rootActionProperty.boxedValue as InlineAction);
        }

        public void Rebuild(string rootPropertyPath, InlineAction rootAction)
        {
            items.Clear();

            string resolvedRootPath = string.IsNullOrWhiteSpace(rootPropertyPath)
                ? "InlineAction"
                : rootPropertyPath;

            BuildBlock(
                rootAction,
                resolvedRootPath,
                new ActionBranchKey(resolvedRootPath, "root"),
                "Root",
                0,
                new HashSet<InlineAction>());
        }

        private void BuildBlock(
            InlineAction action,
            string actionPropertyPath,
            ActionBranchKey branchKey,
            string title,
            int depth,
            HashSet<InlineAction> ancestry)
        {
            items.Add(new ActionBlockTreeItem(
                ActionBlockTreeItemKind.Block,
                depth,
                title,
                "Block",
                branchKey,
                -1,
                null,
                default,
                actionPropertyPath,
                action == null));

            if (action == null)
                return;

            if (!ancestry.Add(action))
            {
                items.Add(new ActionBlockTreeItem(
                    ActionBlockTreeItemKind.Branch,
                    depth + 1,
                    "Cyclic child action",
                    "Cycle",
                    branchKey,
                    -1,
                    null,
                    default,
                    actionPropertyPath,
                    true));
                return;
            }

            if (depth >= MaxDepth)
            {
                items.Add(new ActionBlockTreeItem(
                    ActionBlockTreeItemKind.Branch,
                    depth + 1,
                    "Nested action depth limit",
                    "Depth",
                    branchKey,
                    -1,
                    null,
                    default,
                    actionPropertyPath,
                    true));
                ancestry.Remove(action);
                return;
            }

            IReadOnlyList<ActionStepAuthoring> steps = action.Steps;

            for (int i = 0; i < steps.Count; i++)
            {
                ActionStepAuthoring step = steps[i];
                string stepPropertyPath = $"{actionPropertyPath}._steps.Array.data[{i}]";
                ActionBranchKey stepKey = branchKey.Append($"step:{i}");

                items.Add(new ActionBlockTreeItem(
                    ActionBlockTreeItemKind.Step,
                    depth + 1,
                    BuildStepTitle(step, i),
                    BuildStepBadge(step),
                    stepKey,
                    i,
                    step,
                    default,
                    stepPropertyPath,
                    step == null));

                if (step == null)
                    continue;

                AddChildSlots(step, stepPropertyPath, stepKey, depth + 2, ancestry);
            }

            ancestry.Remove(action);
        }

        private void AddChildSlots(
            ActionStepAuthoring step,
            string stepPropertyPath,
            ActionBranchKey stepKey,
            int depth,
            HashSet<InlineAction> ancestry)
        {
            IReadOnlyList<ActionChildSlotDescriptor> sourceSlots = step.GetChildActionSlots();

            if (sourceSlots == null || sourceSlots.Count == 0)
                return;

            List<ActionChildSlotDescriptor> slots = new(sourceSlots);
            slots.Sort(CompareChildSlots);

            for (int i = 0; i < slots.Count; i++)
            {
                ActionChildSlotDescriptor slot = slots[i];
                ActionBranchKey childKey = stepKey.Append(slot.SlotId);
                string branchPropertyPath = string.IsNullOrWhiteSpace(slot.SerializedPropertyPath)
                    ? string.Empty
                    : $"{stepPropertyPath}.{slot.SerializedPropertyPath}";

                items.Add(new ActionBlockTreeItem(
                    ActionBlockTreeItemKind.Branch,
                    depth,
                    slot.Label,
                    slot.MetadataBadge,
                    childKey,
                    -1,
                    step,
                    slot,
                    branchPropertyPath,
                    !slot.IsPresent));

                if (slot.IsPresent && slot.Action != null)
                    BuildBlock(slot.Action, branchPropertyPath, childKey, slot.Label, depth + 1, ancestry);
            }
        }

        private static int CompareChildSlots(ActionChildSlotDescriptor left, ActionChildSlotDescriptor right)
        {
            int orderComparison = left.Order.CompareTo(right.Order);
            return orderComparison != 0
                ? orderComparison
                : string.Compare(left.Label, right.Label, StringComparison.Ordinal);
        }

        private static string BuildStepTitle(ActionStepAuthoring step, int index)
        {
            if (step == null)
                return $"#{index + 1} Missing Step";

            if (!string.IsNullOrWhiteSpace(step.DisplayName))
                return step.DisplayName;

            string typeName = step.GetType().Name;
            return typeName.EndsWith("StepAuthoring", StringComparison.Ordinal)
                ? typeName.Substring(0, typeName.Length - "StepAuthoring".Length)
                : typeName;
        }

        private static string BuildStepBadge(ActionStepAuthoring step)
        {
            if (step == null)
                return "Missing";

            string typeName = step.GetType().Name;
            return typeName.EndsWith("StepAuthoring", StringComparison.Ordinal)
                ? typeName.Substring(0, typeName.Length - "StepAuthoring".Length)
                : typeName;
        }
    }
}
