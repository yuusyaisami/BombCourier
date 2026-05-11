namespace BC.Base
{
    public sealed class ApplicationKernel : BaseKernel
    {
        public ScopedEntityRegistry ApplicationEntityRegistry { get; set; }

        // Application単位で必要な共有通知と共有状態だけを置く。
        public EventService Events { get; set; }
        public KernelValueStoreService KernelValueStore { get; set; }
        public IKernelEventBus KernelEvents => Events;

        // 旧名互換。新規コードでは KernelEvents を使う。
        public IGameEventBus GameEvents => Events;

        public ApplicationKernel()
        {
            Tickables = new ITickable[]
            {
                // ここにTickableなサービスを追加していく
            };
        }

        public void Dispose()
        {
            Events?.Clear();
            KernelValueStore?.Clear();
        }
    }
}