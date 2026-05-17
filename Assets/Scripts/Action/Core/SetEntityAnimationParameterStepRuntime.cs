using System;
using System.Collections.Generic;
using BC.Animation;
using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [Serializable]
    public sealed class SetEntityAnimationParameterStepRuntime : IActionNodeDefinition
    {
        private readonly EntityTargetReference target;
        private readonly EntityAnimatorParameterWriteMode writeMode;
        private readonly string parameterName;
        private readonly bool boolValue;
        private readonly float floatValue;
        private readonly int intValue;

        public SetEntityAnimationParameterStepRuntime(
            EntityTargetReference target,
            EntityAnimatorParameterWriteMode writeMode,
            string parameterName,
            bool boolValue,
            float floatValue,
            int intValue)
        {
            this.target = target;
            this.writeMode = writeMode;
            this.parameterName = parameterName;
            this.boolValue = boolValue;
            this.floatValue = floatValue;
            this.intValue = intValue;
        }

        public IActionNodeRuntime CreateRuntime()
        {
            return new Runtime(target, writeMode, parameterName, boolValue, floatValue, intValue);
        }

        private sealed class Runtime : IActionNodeRuntime
        {
            private readonly EntityTargetReference target;
            private readonly EntityAnimatorParameterWriteMode writeMode;
            private readonly string parameterName;
            private readonly bool boolValue;
            private readonly float floatValue;
            private readonly int intValue;

            private List<EntityRef> resolvedTargets;

            public Runtime(
                EntityTargetReference target,
                EntityAnimatorParameterWriteMode writeMode,
                string parameterName,
                bool boolValue,
                float floatValue,
                int intValue)
            {
                this.target = target;
                this.writeMode = writeMode;
                this.parameterName = parameterName;
                this.boolValue = boolValue;
                this.floatValue = floatValue;
                this.intValue = intValue;
            }

            public ActionNodeStatus Tick(in ActionExecutionContext context, ref int remainingOperations)
            {
                if (context.SceneKernel == null || context.SceneKernel.EntityComponents == null)
                    return ActionNodeStatus.Failed;

                if (string.IsNullOrWhiteSpace(parameterName))
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
                        Debug.LogWarning($"{nameof(SetEntityAnimationParameterStepRuntime)}: {nameof(EntityAnimationMB)} was not found on {entity}.");
                        continue;
                    }

                    entityAnimation.TryApplyParameter(writeMode, parameterName, boolValue, floatValue, intValue);
                }

                return ActionNodeStatus.Continue;
            }
        }
    }
}