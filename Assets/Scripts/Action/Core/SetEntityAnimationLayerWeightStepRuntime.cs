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
        private readonly float duration;

        public SetEntityAnimationLayerWeightStepRuntime(
            EntityTargetReference target,
            string layerName,
            float weight,
            float duration = 0f)
        {
            this.target = target;
            this.layerName = layerName;
            this.weight = NormalizeWeight(weight);
            this.duration = NormalizeDuration(duration);
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(target, layerName, weight, duration);
        }

        private static float NormalizeWeight(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
        }

        private static float NormalizeDuration(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Max(0f, value);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly EntityTargetReference target;
            private readonly string layerName;
            private readonly float weight;
            private readonly float duration;

            private List<EntityRef> resolvedTargets;
            private List<LayerWeightAnimationTarget> animationTargets;
            private bool initialized;
            private float elapsedTime;

            public Runtime(EntityTargetReference target, string layerName, float weight, float duration)
            {
                this.target = target;
                this.layerName = layerName;
                this.weight = NormalizeWeight(weight);
                this.duration = NormalizeDuration(duration);
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

                if (duration <= 0f)
                {
                    ApplyImmediate(context, count);
                    return ActionNodeStatus.Continue;
                }

                if (!initialized)
                {
                    InitializeAnimationTargets(context, count);
                    initialized = true;

                    if (animationTargets == null || animationTargets.Count == 0)
                        return ActionNodeStatus.Continue;
                }

                elapsedTime = Mathf.Min(duration, elapsedTime + GetDeltaTime());
                float normalizedTime = duration > 0f ? Mathf.Clamp01(elapsedTime / duration) : 1f;

                for (int i = 0; i < animationTargets.Count; i++)
                {
                    LayerWeightAnimationTarget animationTarget = animationTargets[i];
                    if (animationTarget.Animation == null)
                        continue;

                    float currentWeight = Mathf.Lerp(animationTarget.StartWeight, weight, normalizedTime);
                    animationTarget.Animation.TrySetLayerWeight(layerName, currentWeight);
                }

                if (elapsedTime < duration)
                    return ActionNodeStatus.Running;

                for (int i = 0; i < animationTargets.Count; i++)
                {
                    EntityAnimationMB entityAnimation = animationTargets[i].Animation;
                    if (entityAnimation != null)
                        entityAnimation.TrySetLayerWeight(layerName, weight);
                }

                return ActionNodeStatus.Continue;
            }

            private void ApplyImmediate(in ActionExecutionContext context, int count)
            {
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
            }

            private void InitializeAnimationTargets(in ActionExecutionContext context, int count)
            {
                animationTargets ??= new List<LayerWeightAnimationTarget>(count);
                animationTargets.Clear();

                for (int i = 0; i < count; i++)
                {
                    EntityRef entity = resolvedTargets[i];

                    if (!context.SceneKernel.EntityComponents.TryResolve(entity, out EntityAnimationMB entityAnimation))
                    {
                        Debug.LogWarning($"{nameof(SetEntityAnimationLayerWeightStepRuntime)}: {nameof(EntityAnimationMB)} was not found on {entity}.");
                        continue;
                    }

                    if (!entityAnimation.TryGetLayerWeight(layerName, out float startWeight))
                        continue;

                    animationTargets.Add(new LayerWeightAnimationTarget(entityAnimation, startWeight));
                }
            }

            private static float GetDeltaTime()
            {
                if (Time.deltaTime > 0f)
                    return Time.deltaTime;

                return Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            }

            private readonly struct LayerWeightAnimationTarget
            {
                public readonly EntityAnimationMB Animation;
                public readonly float StartWeight;

                public LayerWeightAnimationTarget(EntityAnimationMB animation, float startWeight)
                {
                    Animation = animation;
                    StartWeight = startWeight;
                }
            }
        }
    }
}