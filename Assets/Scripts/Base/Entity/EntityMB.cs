using BC.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace BC.Base
{
    public enum EntityState
    {
        None,
        Dead
        // 他のタグを追加
    }
    public enum EntityRegistrationMode
    {
        ScenePlaced,
        Spawned,
        Manual,
    }
    public sealed class EntityMB : MonoBehaviour
    {
        [FormerlySerializedAs("tag")]
        [SerializeField, HideInInspector] private EntityTagId legacyTag;
        [SerializeField, InspectorName("Tag"), EntityTagDropdown] private EntityTagReference tagReference;
        [SerializeField] private EntityFlags flags = EntityFlags.None;
        [SerializeField] private EntityRegistrationMode registrationMode = EntityRegistrationMode.ScenePlaced;
        public StateMachine<EntityState> EntityStateMachine = new StateMachine<EntityState>();


        public EntityRef Entity { get; private set; }
        public EntityTagId Tag => tagReference.IsAssigned ? tagReference.Id : legacyTag;
        public EntityTagReference TagReference => tagReference;
        public EntityRegistrationMode RegistrationMode => registrationMode;
        public EntityFlags Flags => flags;

        public bool HasEntity => Entity.IsValid;

        public void Bind(EntityRef entity)
        {
            if (Entity.IsValid)
            {
                Debug.LogError($"EntityMB is already bound. Current={Entity}, New={entity}", this);
                return;
            }

            Entity = entity;
        }

        public void Unbind(EntityRef entity)
        {
            if (!Entity.Equals(entity))
            {
                Debug.LogError($"EntityMB unbind mismatch. Current={Entity}, Target={entity}", this);
                return;
            }

            Entity = default;
        }

        private void OnDestroy()
        {
            // 原則、Spawner / Lifecycle からDespawnされる前提。
            // ただし外部Destroy対策を入れるなら、ここでLifecycleへ通知する。
        }
    }
}