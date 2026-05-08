namespace BC.Base
{
    public sealed class SceneKernel : BaseKernel
    {
        public ScopedEntityRegistry EntitiesRegistry { get; set; }
        public EventService Events { get; set; }
        // public ValueStoreService Values { get; }
        public EntityLifecycleService EntityLifecycle { get; set; }
        public ValueStoreService ValueStore { get; set; }
        public EntitySpawnerService Spawner { get; set; }


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