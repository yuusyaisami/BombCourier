namespace BC.Base
{
    using BC.ActionSystem;
    using BC.Camera;

    public sealed class SceneKernel : BaseKernel
    {
        public ScopedEntityRegistry EntitiesRegistry { get; set; }

        // KernelEventsはScene全体通知、EntityEventsはEntity単位通知として使い分ける。
        public EventService Events { get; set; }
        public EntityLifecycleService EntityLifecycle { get; set; }

        // EntityValueStoreはEntityRefごとの状態、KernelValueStoreはScene全体の共有状態を扱う。
        public ValueStoreService EntityValueStore { get; set; }
        public KernelValueStoreService KernelValueStore { get; set; }
        public EntitySpawnerService Spawner { get; set; }
        public EntityComponentResolverService EntityComponents { get; set; }
        public ReactiveValueResolverService ReactiveValues { get; set; }
        public ActionService Actions { get; set; }
        public CameraPathPlayerService CameraPaths { get; set; }
        public SceneCameraService Cameras { get; set; }

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
            EntityComponents = new EntityComponentResolverService(this);
            ReactiveValues = new ReactiveValueResolverService(this);
            Actions = new ActionService(this);
            CameraPaths = new CameraPathPlayerService(this);
            Cameras = new SceneCameraService(this);

            Tickables = new ITickable[]
            {
                Actions,
                Cameras,
            };
        }


        public void Dispose()
        {
            Actions?.Clear();
            CameraPaths?.Cancel();
            Cameras?.Dispose();
            ReactiveValues?.Clear();
            EntityComponents?.Clear();
            EntityValueStore?.Clear();
            KernelValueStore?.Clear();
            Events?.Clear();
        }
    }
}