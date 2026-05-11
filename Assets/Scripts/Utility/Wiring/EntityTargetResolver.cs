using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Base
{
    public enum EntityTargetResolveMode
    {
        Self = 0,
        TriggerEntity = 1,
        TagSearch = 2,
    }

    public enum EntityTargetSelection
    {
        First = 0,
        All = 1,
    }

    [Serializable]
    public struct EntityTargetReference
    {
        [SerializeField] private EntityTargetResolveMode mode;
        [ShowIf(nameof(UsesSelection))]
        [SerializeField]
        private EntityTargetSelection selection;

        [ShowIf(nameof(UsesTag))]
        [SerializeField, EntityTagDropdown]
        private EntityTagReference tag;

        public EntityTargetResolveMode Mode => mode;
        public EntityTargetSelection Selection => selection;
        public EntityTagReference Tag => tag;

        private bool UsesSelection => mode == EntityTargetResolveMode.TagSearch;
        private bool UsesTag => mode == EntityTargetResolveMode.TagSearch;

        public static EntityTargetReference Self()
        {
            return new EntityTargetReference
            {
                mode = EntityTargetResolveMode.Self,
                selection = EntityTargetSelection.First,
            };
        }

        public static EntityTargetReference Trigger()
        {
            return new EntityTargetReference
            {
                mode = EntityTargetResolveMode.TriggerEntity,
                selection = EntityTargetSelection.First,
            };
        }
    }

    public static class EntityTargetResolver
    {
        public static int Resolve(
            in WiringActionContext context,
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