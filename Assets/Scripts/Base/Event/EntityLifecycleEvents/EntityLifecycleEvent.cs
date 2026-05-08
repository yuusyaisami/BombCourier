namespace BC.Base
{
    public struct EntityRegisterEvent : IGameEvent
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

    public readonly struct EntityUnregisteredGameEvent : IGameEvent
    {
        public readonly EntityRef Entity;

        public EntityUnregisteredGameEvent(EntityRef entity)
        {
            Entity = entity;
        }
    }
}