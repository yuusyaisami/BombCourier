using UnityEngine;
namespace BC.Base
{
    public class ValueStoreMB : MonoBehaviour, IKernelInstaller
    {
        private ValueStoreService valueStore;
        public ValueStoreService ValueStore => valueStore;
        public int Order => 0;


        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is SceneKernel sceneKernel)
            {
                valueStore = new ValueStoreService(sceneKernel.Events);
                sceneKernel.ValueStore = valueStore;
            }
            else
            {
                Debug.LogError("Unsupported kernel type for ValueStoreMB");
            }
        }
    }
}