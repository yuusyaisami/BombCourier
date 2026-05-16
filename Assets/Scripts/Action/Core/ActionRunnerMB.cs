using BC.Base;
using UnityEngine;

namespace BC.ActionSystem
{
    [DisallowMultipleComponent]
    public sealed class ActionRunnerMB : MonoBehaviour, IKernelInstaller
    {
        [SerializeField, Min(1)] private int maxOperationsPerTick = 512;

        public int Order => 2;

        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is not SceneKernel sceneKernel)
                return;

            sceneKernel.Actions ??= new ActionService(sceneKernel);
            sceneKernel.Actions.MaxOperationsPerTick = maxOperationsPerTick;
        }
    }
}
