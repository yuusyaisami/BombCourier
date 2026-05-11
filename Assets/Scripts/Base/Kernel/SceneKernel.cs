namespace BC.Base
{
    public sealed class SceneKernel : BaseKernel
    {
        public ScopedEntityRegistry EntitiesRegistry { get; set; }
        public EventService Events { get; set; }
        public EntityLifecycleService EntityLifecycle { get; set; }
        public ValueStoreService EntityValueStore { get; set; }
        public KernelValueStoreService KernelValueStore { get; set; }
        public EntitySpawnerService Spawner { get; set; }

        // 既存コード互換用。新規コードでは EntityValueStore / KernelValueStore を明示して使う。
        public ValueStoreService ValueStore
        {
            get => EntityValueStore;
            set => EntityValueStore = value;
        }

        public IKernelEventBus KernelEvents => Events;

        // 旧名互換。新規コードでは KernelEvents を使う。
        public IGameEventBus GameEvents => Events;
        public IEntityEventService EntityEvents => Events;

        public SceneKernel()
        {
            Tickables = new ITickable[]
            {
            };
        }


        public void Dispose()
        {
            // 必要ならClear
        }
    }
}