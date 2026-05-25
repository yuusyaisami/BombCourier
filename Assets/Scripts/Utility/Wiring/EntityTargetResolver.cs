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

        public string ToSummaryString()
        {
            return mode switch
            {
                EntityTargetResolveMode.Self => "Self",
                EntityTargetResolveMode.TriggerEntity => "TriggerEntity",
                EntityTargetResolveMode.TagSearch => $"TagSearch:{tag} ({selection})",
                _ => mode.ToString(),
            };
        }

        public override string ToString()
        {
            return ToSummaryString();
        }
    }

    public static class EntityTargetResolver
    {
        public static int Resolve(
            in WiringActionContext context,
            EntityTargetReference target,
            List<EntityRef> results)
        {
            return ScopedEntityResolveUtility.ResolveTargets(context, EntityResolveScope.Entity, target, results);
        }
    }
}