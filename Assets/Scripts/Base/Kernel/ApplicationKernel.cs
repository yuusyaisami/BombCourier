namespace BC.Base
{
    public sealed class ApplicationKernel : BaseKernel
    {
        public ScopedEntityRegistry ApplicationEntityRegistry { get; set; }

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
            // 必要ならClear
        }
    }
}