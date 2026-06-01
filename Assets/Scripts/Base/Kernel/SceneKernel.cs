namespace BC.Base
{
    using BC.ActionSystem;
    using BC.Camera;
    using BC.Tutorial;

    // シーン単位のサービス集約点。
    // Entity ではなく Scene 全体で共有する機能をここに載せ、GameLogic からは kernel 経由で参照する。
    public sealed class SceneKernel : BaseKernel
    {
        // stage 上の Entity を引くための登録テーブル。
        public ScopedEntityRegistry EntitiesRegistry { get; set; }

        // Scene 全体通知と Entity 単位通知を分けて扱う。
        public EventService Events { get; set; }
        public EntityLifecycleService EntityLifecycle { get; set; }

        // EntityValueStore は個別 Entity の状態を扱う。
        public ValueStoreService EntityValueStore { get; set; }
        // scene 内の生成・解決・反応値・action・camera をまとめて持つ。
        public EntitySpawnerService Spawner { get; set; }
        public EntityComponentResolverService EntityComponents { get; set; }
        public ReactiveValueResolverService ReactiveValues { get; set; }
        public ActionService Actions { get; set; }
        public CameraPathPlayerService CameraPaths { get; set; }
        public SceneCameraService Cameras { get; set; }
        public TutorialRuntimeService Tutorials { get; set; }

        // 旧コード互換の alias。新規コードでは EntityValueStore を直接使う。
        public ValueStoreService ValueStore
        {
            get => EntityValueStore;
            set => EntityValueStore = value;
        }

        public IKernelEventBus KernelEvents => Events;

        public IEntityEventService EntityEvents => Events;

        public SceneKernel()
        {
            // kernel は各 service の組み立て役で、ゲームループの Tick もここから流す。
            EntityComponents = new EntityComponentResolverService(this);
            ReactiveValues = new ReactiveValueResolverService(this);
            Actions = new ActionService(this);
            CameraPaths = new CameraPathPlayerService(this);
            Cameras = new SceneCameraService(this);
            Tutorials = new TutorialRuntimeService(this);

            Tickables = new ITickable[]
            {
                Actions,
                Cameras,
                Tutorials,
            };
        }

        // シーン終了時や再ロード時に、kernel が保持する service を順に解放する。
        public void Dispose()
        {
            Actions?.Clear();
            CameraPaths?.Cancel();
            Cameras?.Dispose();
            Tutorials?.Stop();
            ReactiveValues?.Clear();
            EntityComponents?.Clear();
            EntityValueStore?.Clear();
            Events?.Clear();
        }
    }
}
