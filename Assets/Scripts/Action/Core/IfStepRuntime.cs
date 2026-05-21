using System;
using BC.Base;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class IfStepRuntime : IActionNodeDefinition
    {
        private readonly ReactiveBool condition;
        private readonly ActionBlockDefinition trueBlock;
        private readonly ActionBlockDefinition falseBlock;

        public IfStepRuntime(
            ReactiveBool condition,
            ActionBlockDefinition trueBlock,
            ActionBlockDefinition falseBlock)
        {
            this.condition = condition;
            this.trueBlock = trueBlock;
            this.falseBlock = falseBlock;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(condition, trueBlock, falseBlock);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly ReactiveBool condition;
            private readonly ActionBlockDefinition trueBlock;
            private readonly ActionBlockDefinition falseBlock;

            private IActionNodeRuntime selectedRuntime;
            private bool branchPrepared;
            private bool failed;

            public Runtime(
                ReactiveBool condition,
                ActionBlockDefinition trueBlock,
                ActionBlockDefinition falseBlock)
            {
                this.condition = condition;
                this.trueBlock = trueBlock;
                this.falseBlock = falseBlock;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (failed)
                    return ActionNodeStatus.Failed;

                if (!branchPrepared && !PrepareBranch(context))
                {
                    failed = true;
                    return ActionNodeStatus.Failed;
                }

                if (selectedRuntime == null)
                    return ActionNodeStatus.Continue;

                ActionNodeStatus status = selectedRuntime.Tick(context, ref remainingOperations);
                if (status == ActionNodeStatus.Failed)
                    failed = true;

                return status;
            }

            public void Cancel(in ActionExecutionContext context)
            {
                selectedRuntime?.Cancel(context);
            }

            private bool PrepareBranch(in ActionExecutionContext context)
            {
                branchPrepared = true;

                if (!TryEvaluateCondition(context, out bool conditionValue))
                    return false;

                // If selects a branch once per execution, then keeps ticking that branch.
                ActionBlockDefinition selectedBlock = conditionValue ? trueBlock : falseBlock;
                if (selectedBlock == null || selectedBlock.Count == 0)
                    return true;

                selectedRuntime = selectedBlock.CreateRuntime();
                return selectedRuntime != null;
            }

            private bool TryEvaluateCondition(in ActionExecutionContext context, out bool conditionValue)
            {
                conditionValue = false;

                if (context.SceneKernel == null || context.SceneKernel.ReactiveValues == null)
                    return false;

                using ReactiveBoolBinding binding = new(
                    context.SceneKernel.ReactiveValues,
                    new ReactiveEvalContext(context),
                    condition);

                ReactiveResult<bool> result = binding.Read();
                if (!result.Success)
                    return false;

                conditionValue = result.Value;
                return true;
            }
        }
    }
}