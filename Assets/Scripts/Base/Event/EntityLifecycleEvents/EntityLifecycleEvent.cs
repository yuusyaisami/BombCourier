namespace BC.Base
{
    public struct EntityRegisterEvent : IEntityEvent
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
}