using UnityEngine;

namespace BC.Base
{
    public sealed class SceneEntityBootstrapper
    {
        private readonly SceneKernel kernel;
        private readonly Transform searchRoot;

        public SceneEntityBootstrapper(SceneKernel kernel, Transform searchRoot)
        {
            this.kernel = kernel;
            this.searchRoot = searchRoot;
        }

        public void RegisterSceneEntities()
        {
            EntityMB[] entities = searchRoot.GetComponentsInChildren<EntityMB>(true);

            for (int i = 0; i < entities.Length; i++)
            {
                EntityMB entityMb = entities[i];

                if (entityMb.HasEntity)
                    continue;

                if (entityMb.RegistrationMode != EntityRegistrationMode.ScenePlaced)
                    continue;

                EntityRegistryRequest request = new EntityRegistryRequest(
                    entityMb.gameObject,
                    entityMb.transform,
                    entityMb.Tag,
                    entityMb.Flags
                );

                EntityRef entity = kernel.EntityLifecycle.Register(request);
                entityMb.Bind(entity);
            }
        }
    }
}