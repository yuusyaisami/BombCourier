using UnityEngine;

namespace BC.Base
{
    public sealed class EntityComponentResolverMB : MonoBehaviour, IKernelInstaller
    {
        // SceneKernel 起動時に resolver service を一度だけ差し込む。
        public int Order => 1;

        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is SceneKernel sceneKernel)
            {
                sceneKernel.EntityComponents ??= new EntityComponentResolverService(sceneKernel);
            }
        }
    }
}
