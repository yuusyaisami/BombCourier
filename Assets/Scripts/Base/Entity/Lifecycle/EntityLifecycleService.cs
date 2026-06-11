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
            // ApplicationKernel は通常 [DefaultExecutionOrder(-10000)] で先に起動するが、
            // GameScene を単体起動した場合 (editor 直接再生 / 一部 PlayMode test) では存在しないことがある。
            // 他の参照箇所 (ScopedEntityResolveUtility, EntityComponentResolverService) と同様に null を許容し、
            // 欠落時は明示的に診断する。ここで生 NRE を出すと SceneKernel 構築全体が壊れるため避ける。
            ApplicationRegistry = ApplicationKernelMB.Instance != null
                ? ApplicationKernelMB.Instance.Kernel?.ApplicationEntityRegistry
                : null;
            if (ApplicationRegistry == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"{nameof(EntityLifecycleService)}: ApplicationEntityRegistry is unavailable (ApplicationKernel not initialized). DontDestroyOnLoad entities will fall back to scene registration.");
            }
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

            if (request.Flags.HasFlag(EntityFlags.DontDestroyOnLoad) && ApplicationRegistry != null)
            {
                entity = ApplicationRegistry.Register(request);
            }
            else
            {
                // DDOL を要求されたが ApplicationRegistry が無い場合は honor できないため、
                // 既存の DDOL ダウングレード方針 (上の分岐) と同じく明示エラーの上で scene 登録へ落とす。
                if (request.Flags.HasFlag(EntityFlags.DontDestroyOnLoad))
                {
                    UnityEngine.Debug.LogError(
                        $"{nameof(EntityLifecycleService)}: cannot honor {nameof(EntityFlags.DontDestroyOnLoad)} for '{request.GameObject.name}' because ApplicationEntityRegistry is unavailable. Registering into scene scope instead.",
                        request.GameObject);
                }

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

            if (!removed && ApplicationRegistry != null)
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