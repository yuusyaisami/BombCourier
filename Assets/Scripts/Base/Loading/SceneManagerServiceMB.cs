using UnityEngine;

namespace BC.Base
{
    // ApplicationKernel に scene 遷移サービスを登録する installer。
    public sealed class SceneManagerServiceMB : MonoBehaviour, IKernelInstaller
    {
        public int Order => 10;

        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is not ApplicationKernel applicationKernel)
            {
                Debug.LogError("SceneManagerServiceMB supports only ApplicationKernel.", this);
                return;
            }

            applicationKernel.SceneManager = new SceneManagerService(applicationKernel);
        }
    }
}