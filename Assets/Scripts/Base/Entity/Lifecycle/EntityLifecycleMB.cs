using UnityEngine;
namespace BC.Base
{
    public class EntityLifecycleMB : MonoBehaviour, IKernelInstaller
    {
        public int Order => -1;

        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is SceneKernel sceneKernel)
            {
                sceneKernel.EntityLifecycle = new EntityLifecycleService(sceneKernel);
            }
            else
            {
                Debug.LogError("Unsupported kernel type for EntityLifecycleMB");
            }
        }
    }
}