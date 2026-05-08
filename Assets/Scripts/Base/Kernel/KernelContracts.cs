using System.Collections.Generic;
namespace BC.Base
{
    // ライフタイムインターフェース
    public interface ITickable
    {
        void Tick(float deltaTime);
    }

    public interface IKernelInstaller
    {
        int Order { get; }
        void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel;
    }

    public sealed class KernelBuilder
    {
        private readonly List<IKernelInstaller> installers = new();

        public KernelBuilder AddInstaller(IKernelInstaller installer)
        {
            installers.Add(installer);
            return this;
        }

        public TKernel Build<TKernel>() where TKernel : BaseKernel, new()
        {
            var kernel = new TKernel();
            // orderでソート
            installers.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var installer in installers)
            {
                installer.Setup<TKernel>(kernel);
            }


            return kernel;
        }
    }
}