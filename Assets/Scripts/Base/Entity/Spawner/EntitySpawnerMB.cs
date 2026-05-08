using UnityEngine;

namespace BC.Base
{
    public sealed class EntitySpawnerMB : MonoBehaviour, IKernelInstaller
    {
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Transform poolRoot;

        public int Order => 10;

        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is not SceneKernel sceneKernel)
                return;

            if (spawnRoot == null)
            {
                var root = new GameObject("[SpawnRoot]");
                root.transform.SetParent(transform, false);
                spawnRoot = root.transform;
            }

            if (poolRoot == null)
            {
                var root = new GameObject("[PoolRoot]");
                root.transform.SetParent(transform, false);
                poolRoot = root.transform;
            }

            sceneKernel.Spawner = new EntitySpawnerService(
                sceneKernel,
                spawnRoot,
                poolRoot
            );
        }
    }
}