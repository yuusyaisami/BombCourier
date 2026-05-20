using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetEntityFacingTargetStepRuntime : IActionNodeDefinition
    {
        private readonly EntityTargetReference target;
        private readonly EntityTargetReference faceTarget;
        private readonly string channel;

        public SetEntityFacingTargetStepRuntime(
            EntityTargetReference target,
            EntityTargetReference faceTarget,
            string channel)
        {
            this.target = target;
            this.faceTarget = faceTarget;
            this.channel = string.IsNullOrWhiteSpace(channel) ? EntityFacingChannels.Action : channel;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(target, faceTarget, channel);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly EntityTargetReference target;
            private readonly EntityTargetReference faceTarget;
            private readonly string channel;

            private List<EntityRef> resolvedTargets;
            private List<EntityRef> resolvedFaceTargets;

            public Runtime(
                EntityTargetReference target,
                EntityTargetReference faceTarget,
                string channel)
            {
                this.target = target;
                this.faceTarget = faceTarget;
                this.channel = string.IsNullOrWhiteSpace(channel) ? EntityFacingChannels.Action : channel;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel == null || context.EntityComponents == null)
                    return ActionNodeStatus.Failed;

                resolvedTargets ??= new List<EntityRef>(4);
                resolvedFaceTargets ??= new List<EntityRef>(4);

                int targetCount = ActionTargetResolver.Resolve(context, target, resolvedTargets);
                int faceTargetCount = ActionTargetResolver.Resolve(context, faceTarget, resolvedFaceTargets);

                if (targetCount == 0 || faceTargetCount == 0)
                    return ActionNodeStatus.Failed;

                if (!context.EntityComponents.TryGetTransform(resolvedFaceTargets[0], out Transform faceTargetTransform) || faceTargetTransform == null)
                {
                    Debug.LogWarning($"{nameof(SetEntityFacingTargetStepRuntime)}: Face target transform could not be resolved.");
                    return ActionNodeStatus.Failed;
                }

                bool applied = false;

                for (int i = 0; i < targetCount; i++)
                {
                    EntityRef entity = resolvedTargets[i];

                    if (!context.EntityComponents.TryResolve(entity, out EntityFacingControllerMB facingController))
                    {
                        Debug.LogWarning($"{nameof(SetEntityFacingTargetStepRuntime)}: {nameof(EntityFacingControllerMB)} was not found on {entity}.");
                        continue;
                    }

                    facingController.SetFacingTargetTransform(channel, faceTargetTransform, EntityFacingPriorities.Action);
                    applied = true;
                }

                return applied ? ActionNodeStatus.Continue : ActionNodeStatus.Failed;
            }
        }
    }
}