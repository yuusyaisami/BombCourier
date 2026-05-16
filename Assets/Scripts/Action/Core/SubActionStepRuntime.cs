namespace BC.ActionSystem
{
    public sealed class SubActionStepRuntime : IActionNodeDefinition
    {
        private readonly ActionBlockDefinition block;

        public SubActionStepRuntime(ActionBlockDefinition block)
        {
            this.block = block;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return block != null ? block.CreateRuntime() : new EmptyRuntime();
        }

        private sealed class EmptyRuntime : IActionNodeRuntime
        {
            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                return ActionNodeStatus.Continue;
            }
        }
    }
}
