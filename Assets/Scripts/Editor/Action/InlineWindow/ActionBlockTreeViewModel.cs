using System;
using System.Collections.Generic;
using BC.ActionSystem;
using UnityEditor;

namespace BC.Editor.ActionSystem
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
            bool isMissing,
            bool canExpand)
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
            CanExpand = canExpand;
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
        public bool CanExpand { get; }
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

            Rebuild(rootActionProperty.serializedObject, rootActionProperty.propertyPath, rootActionProperty.boxedValue as InlineAction);
        }

        public void Rebuild(string rootPropertyPath, InlineAction rootAction)
        {
            Rebuild(null, rootPropertyPath, rootAction);
        }

        private void Rebuild(SerializedObject rootSerializedObject, string rootPropertyPath, InlineAction rootAction)
        {
            items.Clear();

            string resolvedRootPath = string.IsNullOrWhiteSpace(rootPropertyPath)
                ? "InlineAction"
                : rootPropertyPath;

            BuildBlock(
                rootSerializedObject,
                rootAction,
                resolvedRootPath,
                new ActionBranchKey(resolvedRootPath, "root"),
                "Root",
                0,
                new HashSet<InlineAction>());
        }

        private void BuildBlock(
            SerializedObject rootSerializedObject,
            InlineAction action,
            string actionPropertyPath,
            ActionBranchKey branchKey,
            string title,
            int depth,
            HashSet<InlineAction> ancestry)
        {
            // Only the root inline action gets a dedicated block row. Child inline actions are represented by
            // their owning branch row so nested blocks do not appear twice in the tree.
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
                action == null,
                action != null));

            if (action == null)
                return;

            if (!ancestry.Add(action))
            {
                AddIssueItem(depth + 1, "Cyclic child action", "Cycle", branchKey, actionPropertyPath);
                return;
            }

            if (depth >= MaxDepth)
            {
                AddIssueItem(depth + 1, "Nested action depth limit", "Depth", branchKey, actionPropertyPath);
                ancestry.Remove(action);
                return;
            }

            AddSteps(rootSerializedObject, action, actionPropertyPath, branchKey, depth + 1, ancestry);

            ancestry.Remove(action);
        }

        private void AddSteps(
            SerializedObject rootSerializedObject,
            InlineAction action,
            string actionPropertyPath,
            ActionBranchKey branchKey,
            int depth,
            HashSet<InlineAction> ancestry)
        {
            IReadOnlyList<ActionStepAuthoring> steps = action.Steps;

            // Emit each step directly under the current block/branch so indentation stays aligned with execution order.
            for (int i = 0; i < steps.Count; i++)
            {
                ActionStepAuthoring step = steps[i];
                string stepPropertyPath = $"{actionPropertyPath}._steps.Array.data[{i}]";
                ActionBranchKey stepKey = branchKey.Append(BuildStepKeySegment(rootSerializedObject, stepPropertyPath, i));
                IReadOnlyList<ActionChildSlotDescriptor> childSlots = step != null
                    ? step.GetChildActionSlots()
                    : null;
                bool hasChildBranches = childSlots != null && childSlots.Count > 0;

                items.Add(new ActionBlockTreeItem(
                    ActionBlockTreeItemKind.Step,
                    depth,
                    BuildStepTitle(step, i),
                    BuildStepBadge(step),
                    stepKey,
                    i,
                    step,
                    default,
                    stepPropertyPath,
                    step == null,
                    hasChildBranches));

                if (step == null)
                    continue;

                AddChildSlots(rootSerializedObject, step, childSlots, stepPropertyPath, stepKey, depth + 1, ancestry);
            }
        }

        private void AddChildSlots(
            SerializedObject rootSerializedObject,
            ActionStepAuthoring step,
            IReadOnlyList<ActionChildSlotDescriptor> sourceSlots,
            string stepPropertyPath,
            ActionBranchKey stepKey,
            int depth,
            HashSet<InlineAction> ancestry)
        {
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
                    !slot.IsPresent,
                    slot.Action != null));

                if (slot.IsPresent && slot.Action != null)
                    BuildChildAction(rootSerializedObject, slot.Action, branchPropertyPath, childKey, depth + 1, ancestry);
            }
        }

        private void BuildChildAction(
            SerializedObject rootSerializedObject,
            InlineAction action,
            string actionPropertyPath,
            ActionBranchKey branchKey,
            int depth,
            HashSet<InlineAction> ancestry)
        {
            if (action == null)
                return;

            // Nested actions inline their steps beneath the branch header instead of creating another block wrapper.
            if (!ancestry.Add(action))
            {
                AddIssueItem(depth, "Cyclic child action", "Cycle", branchKey, actionPropertyPath);
                return;
            }

            if (depth >= MaxDepth)
            {
                AddIssueItem(depth, "Nested action depth limit", "Depth", branchKey, actionPropertyPath);
                ancestry.Remove(action);
                return;
            }

            AddSteps(rootSerializedObject, action, actionPropertyPath, branchKey, depth, ancestry);
            ancestry.Remove(action);
        }

        private void AddIssueItem(int depth, string title, string badge, ActionBranchKey branchKey, string propertyPath)
        {
            items.Add(new ActionBlockTreeItem(
                ActionBlockTreeItemKind.Branch,
                depth,
                title,
                badge,
                branchKey,
                -1,
                null,
                default,
                propertyPath,
                true,
                false));
        }

        private static string BuildStepKeySegment(SerializedObject rootSerializedObject, string stepPropertyPath, int stepIndex)
        {
            SerializedProperty stepProperty = rootSerializedObject?.FindProperty(stepPropertyPath);

            // Prefer the managed reference id when available so selection survives step reordering.
            if (stepProperty != null && stepProperty.managedReferenceId != 0)
                return $"step:{stepProperty.managedReferenceId}";

            return $"step:{stepIndex}";
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
