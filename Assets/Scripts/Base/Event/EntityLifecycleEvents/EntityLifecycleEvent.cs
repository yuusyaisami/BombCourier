using System;

namespace BC.Base
{
    public readonly struct EntityRegisteredKernelEvent : IKernelEvent
    {
        public EntityRef Entity { get; }
        public EntityTagId Tag { get; }
        public EntityFlags Flags { get; }

        public EntityRegisteredKernelEvent(EntityRef entity, EntityTagId tag, EntityFlags flags)
        {
            Entity = entity;
            Tag = tag;
            Flags = flags;
        }
    }

    public readonly struct EntityUnregisteredKernelEvent : IKernelEvent
    {
        public readonly EntityRef Entity;

        public EntityUnregisteredKernelEvent(EntityRef entity)
        {
            Entity = entity;
        }
    }

    public readonly struct EntityRegisteredEvent : IEntityEvent
    {
        public EntityRef Entity { get; }
        public EntityTagId Tag { get; }
        public EntityFlags Flags { get; }

        public EntityRegisteredEvent(EntityRef entity, EntityTagId tag, EntityFlags flags)
        {
            Entity = entity;
            Tag = tag;
            Flags = flags;
        }
    }

    public readonly struct EntityUnregisteredEvent : IEntityEvent
    {
        public readonly EntityRef Entity;

        public EntityUnregisteredEvent(EntityRef entity)
        {
            Entity = entity;
        }
    }

    [Obsolete("Use EntityRegisteredKernelEvent instead.")]
    public readonly struct EntityRegisterEvent : IGameEvent
    {
        public EntityRef Entity { get; }
        public EntityTagId Tag { get; }
        public EntityFlags Flags { get; }

        public EntityRegisterEvent(EntityRef entity, EntityTagId tag, EntityFlags flags)
        {
            Entity = entity;
            Tag = tag;
            Flags = flags;
        }
    }

    [Obsolete("Use EntityUnregisteredKernelEvent instead.")]
    public readonly struct EntityUnregisteredGameEvent : IGameEvent
    {
        public readonly EntityRef Entity;

        public EntityUnregisteredGameEvent(EntityRef entity)
        {
            Entity = entity;
        }
    }
}