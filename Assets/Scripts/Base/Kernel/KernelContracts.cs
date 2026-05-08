using System.Collections.Generic;
using UnityEngine;
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
        public TKernel Build<TKernel>(GameObject[] obj) where TKernel : BaseKernel, new()
        {
            var kernel = new TKernel();
            foreach (var go in obj)
            {
                installers.AddRange(go.GetComponents<IKernelInstaller>());
            }
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