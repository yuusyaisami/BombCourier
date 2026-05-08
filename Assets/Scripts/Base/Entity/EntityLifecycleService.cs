namespace BC.Base
{
    public class EntityLifecycleService
    {
        private ScopedEntityRegistry SceneRegistry;
        private ScopedEntityRegistry ApplicationRegistry;
        private EventService events;
        // private ValueStoreService valueStore;

        public int Order => 0;

        public void Setup(SceneKernel kernel)
        {
            SceneRegistry = kernel.EntitiesRegistry;
            ApplicationRegistry = ApplicationKernelMB.Instance.Kernel.ApplicationEntityRegistry;
            events = kernel.Events;
        }

        public EntityRef Register(EntityRegistryRequest request)
        {
            EntityRef entity;

            if (request.Flags.HasFlag(EntityFlags.DontDestroyOnLoad))
            {
                entity = ApplicationRegistry.Register(request);
            }
            else
            {
                entity = SceneRegistry.Register(request);
            }

            events.Publish(new EntityRegisterEvent(entity, request.Tag, request.Flags));

            return entity;
        }

        public bool Unregister(EntityRef entity)
        {
            if (!entity.IsValid)
                return false;

            bool removed = SceneRegistry.Unregister(entity);

            if (!removed)
            {
                removed = ApplicationRegistry.Unregister(entity);
            }

            if (!removed)
                return false;

            events.ClearEntity(entity);
            events.Publish(new EntityUnregisteredGameEvent(entity));

            return true;
        }
    }
}