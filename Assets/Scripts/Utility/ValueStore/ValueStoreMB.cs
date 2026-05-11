using UnityEngine;
namespace BC.Base
{
    public class ValueStoreMB : MonoBehaviour, IKernelInstaller
    {
        private ValueStoreService entityValueStore;
        private KernelValueStoreService kernelValueStore;

        public ValueStoreService ValueStore => entityValueStore;
        public ValueStoreService EntityValueStore => entityValueStore;
        public KernelValueStoreService KernelValueStore => kernelValueStore;

        // Event/Lifecycleの後に、Entity単位とKernel単位のStoreを公開する。
        public int Order => 0;


        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is SceneKernel sceneKernel)
            {
                entityValueStore = new ValueStoreService();
                kernelValueStore = new KernelValueStoreService();
                sceneKernel.EntityValueStore = entityValueStore;
                sceneKernel.KernelValueStore = kernelValueStore;
            }
            else if (kernel is ApplicationKernel applicationKernel)
            {
                kernelValueStore = new KernelValueStoreService();
                applicationKernel.KernelValueStore = kernelValueStore;
            }
            else
            {
                Debug.LogError("Unsupported kernel type for ValueStoreMB");
            }
        }
    }
}