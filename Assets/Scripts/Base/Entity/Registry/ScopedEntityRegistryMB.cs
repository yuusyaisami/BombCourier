using UnityEngine;
namespace BC.Base
{
    public class ScopedEntityRegistryMB : MonoBehaviour, IKernelInstaller
    {
        // シーンとアプリケーションでIDを共有するための静的なIDアロケータ
        public static EntityIdAllocator IdAllocator;
        public ScopedEntityRegistry Registry { get; private set; }
        public int Order => -5;
        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (IdAllocator == null)
            {
                IdAllocator = new EntityIdAllocator();
            }
            if (kernel is SceneKernel sceneKernel)
            {
                Registry = new ScopedEntityRegistry(EntityLifetimeScope.Scene, IdAllocator);
                sceneKernel.EntitiesRegistry = Registry;
            }
            else if (kernel is ApplicationKernel applicationKernel)
            {
                Registry = new ScopedEntityRegistry(EntityLifetimeScope.Application, IdAllocator);
                applicationKernel.ApplicationEntityRegistry = Registry;
            }
            else
            {
                Debug.LogError("Unsupported kernel type for ScopedEntityRegistryMB");
            }
        }

    }
}