using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetActiveStepRuntime : IActionStepRuntime
    {
        [SerializeField] private EntityTargetReference target;
        [SerializeField] private bool active;

        [NonSerialized] private List<EntityRef> resolvedTargets;

        public SetActiveStepRuntime(EntityTargetReference target, bool active)
        {
            this.target = target;
            this.active = active;
        }

        public bool Execute(in ActionExecutionContext context)
        {
            if (context.SceneKernel == null || context.SceneKernel.EntityComponents == null)
                return false;

            resolvedTargets ??= new List<EntityRef>(4);
            int count = ActionTargetResolver.Resolve(context, target, resolvedTargets);
            bool handled = false;

            for (int i = 0; i < count; i++)
            {
                EntityRef entity = resolvedTargets[i];

                if (!context.SceneKernel.EntityComponents.TryGetGameObject(entity, out GameObject targetObject))
                    continue;

                targetObject.SetActive(active);
                handled = true;
            }

            return handled;
        }
    }
}
