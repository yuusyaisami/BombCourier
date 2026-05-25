using System;
using System.Collections.Generic;
using BC.ActionSystem;

namespace BC.Base
{
    public enum EntityResolveScope
    {
        Entity = 0,
        SceneKernel = 1,
        ApplicationKernel = 2,
    }

    public readonly struct EntityResolveContext
    {
        public readonly SceneKernel SceneKernel;
        public readonly EntityRef SelfEntity;
        public readonly EntityRef TriggerEntity;

        public EntityResolveContext(SceneKernel sceneKernel, EntityRef selfEntity, EntityRef triggerEntity)
        {
            SceneKernel = sceneKernel;
            SelfEntity = selfEntity;
            TriggerEntity = triggerEntity;
        }

        public EntityResolveContext(in WiringActionContext context)
            : this(context.SceneKernel, context.SelfEntity, context.TriggerEntity)
        {
        }

        public EntityResolveContext(in ActionExecutionContext context)
            : this(context.SceneKernel, context.SelfEntity, context.TriggerEntity)
        {
        }
    }

    public static class ScopedEntityResolveUtility
    {
        public static int ResolveTargets(
            in WiringActionContext context,
            EntityResolveScope scope,
            EntityTargetReference entityTarget,
            List<EntityRef> results)
        {
            return ResolveTargets(new EntityResolveContext(context), scope, entityTarget, results);
        }

        public static int ResolveTargets(
            in ActionExecutionContext context,
            EntityResolveScope scope,
            EntityTargetReference entityTarget,
            List<EntityRef> results)
        {
            return ResolveTargets(new EntityResolveContext(context), scope, entityTarget, results);
        }

        public static int ResolveTargets(
            in EntityResolveContext context,
            EntityResolveScope scope,
            EntityTargetReference entityTarget,
            List<EntityRef> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();

            switch (scope)
            {
                case EntityResolveScope.Entity:
                    return ResolveEntityScope(context, entityTarget, results);

                case EntityResolveScope.SceneKernel:
                    return AddFirstFromRegistry(context.SceneKernel?.EntitiesRegistry, EntityTags.System.SceneKernel.Id, results);

                case EntityResolveScope.ApplicationKernel:
                    return AddFirstFromRegistry(
                        ApplicationKernelMB.Instance?.Kernel?.ApplicationEntityRegistry,
                        EntityTags.System.ApplicationKernel.Id,
                        results);

                default:
                    return 0;
            }
        }

        public static bool TryResolveSingle(
            in EntityResolveContext context,
            EntityResolveScope scope,
            EntityTargetReference entityTarget,
            out EntityRef entity,
            out int resolvedCount)
        {
            var buffer = new List<EntityRef>(2);
            resolvedCount = ResolveTargets(context, scope, entityTarget, buffer);
            if (resolvedCount <= 0)
            {
                entity = default;
                return false;
            }

            entity = buffer[0];
            return true;
        }

        private static int ResolveEntityScope(
            in EntityResolveContext context,
            EntityTargetReference target,
            List<EntityRef> results)
        {
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

        private static int AddFirstFromRegistry(ScopedEntityRegistry registry, EntityTagId tag, List<EntityRef> results)
        {
            if (registry == null || !tag.IsValid)
                return 0;

            IReadOnlyList<EntityRef> entities = registry.GetEntitiesByTag(tag);

            for (int i = 0; i < entities.Count; i++)
            {
                if (!entities[i].IsValid)
                    continue;

                results.Add(entities[i]);
                return 1;
            }

            return 0;
        }
    }
}
