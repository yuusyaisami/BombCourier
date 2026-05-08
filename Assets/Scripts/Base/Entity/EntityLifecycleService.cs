namespace BC.Base
{

    // エンティティの登録はこれが担当します。
    public class EntityLifecycleService
    {
        private ScopedEntityRegistry SceneRegistry;
        private ScopedEntityRegistry ApplicationRegistry;
        private EventService events;
        //private readonly ValueStoreService valueStore; 

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


            return entity;
        }

    }
}