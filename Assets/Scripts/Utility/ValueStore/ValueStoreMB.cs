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
                // SceneKernel では Entity scope と Kernel scope を別 store として公開する。
                // Action/ReactiveValue の scope 名と実保存先を1対1にして、authoring ミスを追いやすくする。
                entityValueStore = new ValueStoreService();
                sceneKernel.EntityValueStore = entityValueStore;
                kernelValueStore = new KernelValueStoreService();
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
