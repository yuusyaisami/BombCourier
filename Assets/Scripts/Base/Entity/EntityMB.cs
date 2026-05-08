using UnityEngine;
namespace BC.Base
{
    public interface IEntitySetupEvent
    {

    }
    public struct EntitySetupEvent : IEntitySetupEvent
    {
        public EntityRef EntityRef { get; private set; }
        public EntityData EntityData { get; private set; }

        public EntitySetupEvent(EntityRef entityRef, EntityData entityData)
        {
            EntityRef = entityRef;
            EntityData = entityData;
        }
    }
    public sealed class EntityMB : MonoBehaviour
    {

        [SerializeField] private EntityTagId tag;
        [SerializeField] private EntityFlags flags;

        public EntityRef Entity { get; private set; }
        private SceneKernel kernel;

        private void Awake()
        {
            kernel = GetComponentInParent<SceneKernelMB>()?.Kernel;
            Entity = kernel.EntityLifecycle.Register(new EntityRegistryRequest(gameObject, transform, tag, flags));
        }

        private void OnDestroy()
        {
            if (!Entity.IsValid)
                return;
        }
    }
}