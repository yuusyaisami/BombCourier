namespace BC.Base
{
    public class EntityLifecycleService
    {
        private SceneKernel kernel;
        private ScopedEntityRegistry SceneRegistry;
        private ScopedEntityRegistry ApplicationRegistry;
        private IKernelEventBus kernelEvents;
        private IEntityEventService entityEvents;
        // private ValueStoreService valueStore;

        public int Order => 0;

        public EntityLifecycleService(SceneKernel kernel)
        {
            this.kernel = kernel;
            SceneRegistry = kernel.EntitiesRegistry;
            ApplicationRegistry = ApplicationKernelMB.Instance.Kernel.ApplicationEntityRegistry;
            kernelEvents = kernel.KernelEvents;
            entityEvents = kernel.EntityEvents;
        }

        public EntityRef Register(EntityRegistryRequest request)
        {
            // SceneKernel 配下の Entity で DDOL flag が立っていたら、
            // scene lifetime を壊すためエラーにして scene 登録へ強制ダウングレードする。
            if (request.Flags.HasFlag(EntityFlags.DontDestroyOnLoad) &&
                request.Transform != null &&
                request.Transform.GetComponentInParent<SceneKernelMB>() != null)
            {
                UnityEngine.Debug.LogError(
                    $"{nameof(EntityLifecycleService)}: SceneKernel scoped entity '{request.GameObject.name}' cannot use {nameof(EntityFlags.DontDestroyOnLoad)}. Downgrading to scene registration.",
                    request.GameObject);
                request.Flags &= ~EntityFlags.DontDestroyOnLoad;
            }

            EntityRef entity;

            if (request.Flags.HasFlag(EntityFlags.DontDestroyOnLoad))
            {
                entity = ApplicationRegistry.Register(request);
            }
            else
            {
                entity = SceneRegistry.Register(request);
            }

            kernel.EntityComponents?.Register(entity, request.GameObject, request.Transform);

            entityEvents.Publish(entity, new EntityRegisteredEvent(entity, request.Tag, request.Flags));
            kernelEvents.Publish(new EntityRegisteredKernelEvent(entity, request.Tag, request.Flags));

            return entity;
        }

        public bool Unregister(EntityRef entity)
        {
            if (!entity.IsValid)
                return false;

            bool removed = SceneRegistry.Unregister(entity);

            if (!removed)
            {
                removed = ApplicationRegistry.Unregister(entity);
            }

            if (!removed)
                return false;

            kernel.Actions?.ClearEntity(entity);
            kernel.EntityComponents?.Unregister(entity);
            entityEvents.Publish(entity, new EntityUnregisteredEvent(entity));
            entityEvents.ClearEntity(entity);
            kernel.EntityValueStore?.ClearEntity(entity);
            kernelEvents.Publish(new EntityUnregisteredKernelEvent(entity));

            return true;
        }
    }
}