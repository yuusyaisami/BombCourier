using System;
using System.Collections.Generic;
using BC.Animation;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetEntityAnimationLayerWeightStepRuntime : IActionNodeDefinition
    {
        private readonly EntityTargetReference target;
        private readonly string layerName;
        private readonly float weight;

        public SetEntityAnimationLayerWeightStepRuntime(
            EntityTargetReference target,
            string layerName,
            float weight)
        {
            this.target = target;
            this.layerName = layerName;
            this.weight = NormalizeWeight(weight);
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(target, layerName, weight);
        }

        private static float NormalizeWeight(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly EntityTargetReference target;
            private readonly string layerName;
            private readonly float weight;

            private List<EntityRef> resolvedTargets;

            public Runtime(EntityTargetReference target, string layerName, float weight)
            {
                this.target = target;
                this.layerName = layerName;
                this.weight = NormalizeWeight(weight);
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel == null || context.SceneKernel.EntityComponents == null)
                    return ActionNodeStatus.Failed;

                if (string.IsNullOrWhiteSpace(layerName))
                    return ActionNodeStatus.Failed;

                resolvedTargets ??= new List<EntityRef>(4);
                int count = ActionTargetResolver.Resolve(context, target, resolvedTargets);

                if (count == 0)
                    return ActionNodeStatus.Failed;

                for (int i = 0; i < count; i++)
                {
                    EntityRef entity = resolvedTargets[i];

                    if (!context.SceneKernel.EntityComponents.TryResolve(entity, out EntityAnimationMB entityAnimation))
                    {
                        Debug.LogWarning($"{nameof(SetEntityAnimationLayerWeightStepRuntime)}: {nameof(EntityAnimationMB)} was not found on {entity}.");
                        continue;
                    }

                    entityAnimation.TrySetLayerWeight(layerName, weight);
                }

                return ActionNodeStatus.Continue;
            }
        }
    }
}