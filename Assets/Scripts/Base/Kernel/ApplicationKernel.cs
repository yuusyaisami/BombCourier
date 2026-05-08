namespace BC.Base
{
    public sealed class ApplicationKernel : BaseKernel
    {
        public ScopedEntityRegistry ApplicationEntityRegistry { get; set; }

        public EventService Events { get; set; }

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