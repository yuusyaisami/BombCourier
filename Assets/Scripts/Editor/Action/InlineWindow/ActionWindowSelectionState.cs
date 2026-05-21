namespace BC.Editor.Action
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
    }
}
