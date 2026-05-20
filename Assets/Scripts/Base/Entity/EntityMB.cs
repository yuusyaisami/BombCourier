using System.Collections.Generic;
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
        [SerializeField, InspectorName("Tag"), EntityTagDropdown] private EntityTagReference tagReference;
        [SerializeField] private EntityFlags flags = EntityFlags.None;
        [SerializeField] private bool autoInstallRigidbodySupportRiders = true;
        private EntityRegistrationMode registrationMode = EntityRegistrationMode.Manual;
        public StateMachine<EntityState> EntityStateMachine = new StateMachine<EntityState>();


        public EntityRef Entity { get; private set; }
        public EntityTagId Tag => tagReference.IsAssigned ? tagReference.Id : default;
        public EntityTagReference TagReference => tagReference;
        public EntityRegistrationMode RegistrationMode => registrationMode;
        public EntityFlags Flags => flags;

        public bool HasEntity => Entity.IsValid;

        private void Awake()
        {
            EnsureRigidbodySupportRiders();
        }

        public void Bind(EntityRef entity)
        {
            Bind(entity, EntityRegistrationMode.Manual);
        }

        public void Bind(EntityRef entity, EntityRegistrationMode mode)
        {
            if (Entity.IsValid)
            {
                Debug.LogError($"EntityMB is already bound. Current={Entity}, New={entity}", this);
                return;
            }

            Entity = entity;
            registrationMode = mode;
        }

        public void Unbind(EntityRef entity)
        {
            if (!Entity.Equals(entity))
            {
                Debug.LogError($"EntityMB unbind mismatch. Current={Entity}, Target={entity}", this);
                return;
            }

            Entity = default;
            registrationMode = EntityRegistrationMode.Manual;
        }

        private void OnDestroy()
        {
            // 原則、Spawner / Lifecycle からDespawnされる前提。
            // ただし外部Destroy対策を入れるなら、ここでLifecycleへ通知する。
        }

        private void EnsureRigidbodySupportRiders()
        {
            if (!autoInstallRigidbodySupportRiders || !Application.isPlaying)
                return;

            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody targetRigidbody = rigidbodies[i];

                if (targetRigidbody == null || ShouldSkipSupportRider(targetRigidbody))
                    continue;

                if (targetRigidbody.GetComponent<RigidbodySupportRiderMB>() == null)
                    targetRigidbody.gameObject.AddComponent<RigidbodySupportRiderMB>();
            }
        }

        private static bool ShouldSkipSupportRider(Rigidbody targetRigidbody)
        {
            if (targetRigidbody.GetComponentInParent<EntityMoveMotorMB>() != null)
                return true;

            return targetRigidbody.GetComponentInParent<ISupportMotionSource>() != null;
        }
    }
}