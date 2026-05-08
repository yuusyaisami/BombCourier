namespace BC.Base
{
    // ライフタイムインターフェース
    public class BaseKernel
    {
        // 共通の機能やサービスをここに配置する予定
        public ITickable[] Tickables { get; protected set; }

        public void Tick(float deltaTime)
        {
            foreach (var tickable in Tickables)
            {
                tickable.Tick(deltaTime);
            }
        }
    }
}