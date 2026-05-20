using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class ClearEntityFacingStepRuntime : IActionNodeDefinition
    {
        private readonly EntityTargetReference target;
        private readonly string channel;

        public ClearEntityFacingStepRuntime(EntityTargetReference target, string channel)
        {
            this.target = target;
            this.channel = string.IsNullOrWhiteSpace(channel) ? EntityFacingChannels.Action : channel;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(target, channel);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly EntityTargetReference target;
            private readonly string channel;

            private List<EntityRef> resolvedTargets;

            public Runtime(EntityTargetReference target, string channel)
            {
                this.target = target;
                this.channel = string.IsNullOrWhiteSpace(channel) ? EntityFacingChannels.Action : channel;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel == null || context.EntityComponents == null)
                    return ActionNodeStatus.Failed;

                resolvedTargets ??= new List<EntityRef>(4);
                int targetCount = ActionTargetResolver.Resolve(context, target, resolvedTargets);

                if (targetCount == 0)
                    return ActionNodeStatus.Failed;

                bool cleared = false;

                for (int i = 0; i < targetCount; i++)
                {
                    EntityRef entity = resolvedTargets[i];

                    if (!context.EntityComponents.TryResolve(entity, out EntityFacingControllerMB facingController))
                    {
                        Debug.LogWarning($"{nameof(ClearEntityFacingStepRuntime)}: {nameof(EntityFacingControllerMB)} was not found on {entity}.");
                        continue;
                    }

                    facingController.ClearFacing(channel);
                    cleared = true;
                }

                return cleared ? ActionNodeStatus.Continue : ActionNodeStatus.Failed;
            }
        }
    }
}