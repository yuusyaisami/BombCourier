using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetActiveStepRuntime : IActionNodeDefinition
    {
        private readonly EntityTargetReference target;
        private readonly bool active;

        public SetActiveStepRuntime(EntityTargetReference target, bool active)
        {
            this.target = target;
            this.active = active;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(target, active);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly EntityTargetReference target;
            private readonly bool active;

            private List<EntityRef> resolvedTargets;

            public Runtime(EntityTargetReference target, bool active)
            {
                this.target = target;
                this.active = active;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel == null || context.SceneKernel.EntityComponents == null)
                    return ActionNodeStatus.Failed;

                resolvedTargets ??= new List<EntityRef>(4);
                int count = ActionTargetResolver.Resolve(context, target, resolvedTargets);

                for (int i = 0; i < count; i++)
                {
                    EntityRef entity = resolvedTargets[i];

                    if (!context.SceneKernel.EntityComponents.TryGetGameObject(entity, out GameObject targetObject))
                        continue;

                    targetObject.SetActive(active);
                }

                return ActionNodeStatus.Continue;
            }
        }
    }
}
