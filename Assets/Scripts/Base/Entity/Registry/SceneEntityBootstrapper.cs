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

                EntityRegistryRequest request = new EntityRegistryRequest(
                    entityMb.gameObject,
                    entityMb.transform,
                    entityMb.Tag,
                    entityMb.Flags
                );

                EntityRef entity = kernel.EntityLifecycle.Register(request);
                // SceneKernel 配下に最初からある Entity は実際の登録経路で ScenePlaced とみなす。
                entityMb.Bind(entity, EntityRegistrationMode.ScenePlaced);
            }
        }

        public void UnregisterSceneEntities()
        {
            EntityMB[] entities = searchRoot.GetComponentsInChildren<EntityMB>(true);

            for (int i = entities.Length - 1; i >= 0; i--)
            {
                EntityMB entityMb = entities[i];

                if (!entityMb.HasEntity)
                    continue;

                EntityRef entity = entityMb.Entity;
                entityMb.Unbind(entity);
                kernel.EntityLifecycle.Unregister(entity);
            }
        }
    }
}