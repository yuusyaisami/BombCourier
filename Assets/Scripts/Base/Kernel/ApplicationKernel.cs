namespace BC.Base
{
    public sealed class ApplicationKernel : BaseKernel
    {
        public ScopedEntityRegistry ApplicationEntityRegistry { get; set; }

        public EventService EntityEvents { get; }

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