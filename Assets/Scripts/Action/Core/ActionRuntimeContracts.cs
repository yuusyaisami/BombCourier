using System;
using System.Collections.Generic;
using BC.Base;

namespace BC.ActionSystem
{
    public interface IActionStepRuntime
    {
        bool Execute(in ActionExecutionContext context);
    }

    public readonly struct ActionExecutionContext
    {
        public readonly SceneKernel SceneKernel;
        public readonly EntityRef SelfEntity;
        public readonly EntityRef TriggerEntity;

        public ActionExecutionContext(SceneKernel sceneKernel, EntityRef selfEntity, EntityRef triggerEntity = default)
        {
            SceneKernel = sceneKernel;
            SelfEntity = selfEntity;
            TriggerEntity = triggerEntity;
        }
    }

    public sealed class ActionValidationContext
    {
        private readonly List<string> errors = new();

        public IReadOnlyList<string> Errors => errors;
        public bool IsValid => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void ValidateEntityTarget(EntityTargetReference target)
        {
            if (target.Mode == EntityTargetResolveMode.TagSearch && !target.Tag.IsAssigned)
                AddError("Entity target tag is not assigned.");
        }
    }

    public sealed class ActionCompileContext
    {
        private readonly List<IActionStepRuntime> steps = new();

        public IReadOnlyList<IActionStepRuntime> Steps => steps;

        public void AddStep(IActionStepRuntime step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            steps.Add(step);
        }

        public CompiledAction Build()
        {
            return new CompiledAction(steps);
        }
    }

    public sealed class CompiledAction
    {
        private readonly IActionStepRuntime[] steps;

        public CompiledAction(IReadOnlyList<IActionStepRuntime> steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));

            this.steps = new IActionStepRuntime[steps.Count];

            for (int i = 0; i < steps.Count; i++)
            {
                this.steps[i] = steps[i];
            }
        }

        public bool Execute(in ActionExecutionContext context)
        {
            bool handled = false;

            for (int i = 0; i < steps.Length; i++)
            {
                IActionStepRuntime step = steps[i];

                if (step == null)
                    continue;

                handled |= step.Execute(context);
            }

            return handled;
        }
    }

    public static class ActionTargetResolver
    {
        public static int Resolve(
            in ActionExecutionContext context,
            EntityTargetReference target,
            List<EntityRef> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();

            switch (target.Mode)
            {
                case EntityTargetResolveMode.Self:
                    AddIfValid(context.SelfEntity, target.Selection, results);
                    break;

                case EntityTargetResolveMode.TriggerEntity:
                    AddIfValid(context.TriggerEntity, target.Selection, results);
                    break;

                case EntityTargetResolveMode.TagSearch:
                    ResolveByTag(context.SceneKernel, target.Tag.Id, target.Selection, results);
                    break;
            }

            return results.Count;
        }

        private static bool AddIfValid(EntityRef entity, EntityTargetSelection selection, List<EntityRef> results)
        {
            if (!entity.IsValid)
                return false;

            results.Add(entity);
            return selection == EntityTargetSelection.First;
        }

        private static void ResolveByTag(
            SceneKernel sceneKernel,
            EntityTagId tag,
            EntityTargetSelection selection,
            List<EntityRef> results)
        {
            if (!tag.IsValid)
                return;

            if (sceneKernel?.EntitiesRegistry != null &&
                AddFromRegistry(sceneKernel.EntitiesRegistry, tag, selection, results))
            {
                return;
            }

            ApplicationKernel applicationKernel = ApplicationKernelMB.Instance != null
                ? ApplicationKernelMB.Instance.Kernel
                : null;

            if (applicationKernel?.ApplicationEntityRegistry != null)
                AddFromRegistry(applicationKernel.ApplicationEntityRegistry, tag, selection, results);
        }

        private static bool AddFromRegistry(
            ScopedEntityRegistry registry,
            EntityTagId tag,
            EntityTargetSelection selection,
            List<EntityRef> results)
        {
            IReadOnlyList<EntityRef> entities = registry.GetEntitiesByTag(tag);

            for (int i = 0; i < entities.Count; i++)
            {
                EntityRef entity = entities[i];

                if (!entity.IsValid)
                    continue;

                results.Add(entity);

                if (selection == EntityTargetSelection.First)
                    return true;
            }

            return false;
        }
    }
}
