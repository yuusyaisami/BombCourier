using System;
using System.Collections.Generic;
using BC.Base;
using BC.Gimmick.Cushion;
using BC.Item;
using BC.Stage;
using UnityEngine;

namespace BC.Bomb
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class ChainExplosiveMB : MonoBehaviour, ICarryableItem, ICarryMoveModifier, ICushionImpactSource, IExplosionImpactDetector, ICarryReleaseOwnerCollisionGuard, IStageCheckpointParticipant
    {
        public event Action<ChainExplosiveMB> Exploded;
        public event Action<ChainExplosiveMB> StartedFuse;

        [Header("Carry")]
        [SerializeField] private bool canBeCarried = true;
        [SerializeField, Range(0.0f, 1.0f)] private float carryJumpHeightMultiplier = 0.65f;

        [Header("Fuse")]
        [SerializeField, Min(0.05f)] private float fuseTime = 0.35f;
        [SerializeField] private ParticleSystem startFuseEffect;

        [Header("Explosion Trigger")]
        [SerializeField, Min(0.0f)] private float triggerExplosionThreshold = 0.01f;

        [Header("Explosion")]
        [SerializeField, Min(0.0f)] private float explosionRadius = 5f;
        [SerializeField, Min(0.0f)] private float explosionForce = 1000f;
        [SerializeField] private ParticleSystem explosionEffectPrefab;

        private Rigidbody rb;
        private Collider explosiveCollider;
        private SceneKernelMB kernelMB;
        private EntityMB entityMB;
        private EntityRef entityRef;
        private bool fuseStarted;
        private bool exploded;
        private bool isHandled;
        private float remainingFuseTime;
        private float ignoreOwnerCollisionUntilTime;

        private readonly List<Collider> ignoredHolderColliders = new(16);
        private readonly List<EntityImpactResponseMB> explosionImpactResponses = new(16);

        public Transform ItemTransform => transform;
        public bool IsHandled => isHandled;
        public bool CanBeCarried => canBeCarried && !exploded && enabled && gameObject.activeInHierarchy;
        public Transform CushionImpactRoot => transform;
        public EntityTagId CushionImpactTag => ResolveImpactTag();
        public bool FuseStarted => fuseStarted;
        public bool HasExploded => exploded;
        public float TotalFuseTime => fuseTime;
        public float RemainingFuseTime => remainingFuseTime;
        public float LastReceivedExplosionForce { get; private set; }

        public bool TryGetJumpHeightMultiplier(out float jumpHeightMultiplier)
        {
            jumpHeightMultiplier = carryJumpHeightMultiplier;
            return true;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            explosiveCollider = GetComponent<Collider>();
            kernelMB = GetComponentInParent<SceneKernelMB>();
            entityMB = GetComponentInParent<EntityMB>();

            if (entityMB != null && entityMB.HasEntity)
                entityRef = entityMB.Entity;

            remainingFuseTime = Mathf.Max(0.05f, fuseTime);
        }

        private void OnValidate()
        {
            fuseTime = Mathf.Max(0.05f, fuseTime);
            triggerExplosionThreshold = Mathf.Max(0.0f, triggerExplosionThreshold);
            explosionRadius = Mathf.Max(0.0f, explosionRadius);
            explosionForce = Mathf.Max(0.0f, explosionForce);
        }

        private void OnDisable()
        {
            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
        }

        private void Update()
        {
            TickReleaseOwnerCollisionIgnore();

            if (!fuseStarted || exploded)
                return;

            remainingFuseTime -= Time.deltaTime;

            if (remainingFuseTime <= 0f)
                ExplodeNow();
        }

        public void OnHandle(Transform handlePoint)
        {
            if (!CanBeCarried || handlePoint == null)
                return;

            isHandled = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.useGravity = false;

            ConfigureHeldHolderCollisionIgnore(handlePoint);

            transform.SetParent(handlePoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void OnRelease(Vector3 throwVelocity)
        {
            if (!isHandled)
                return;

            isHandled = false;
            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
            transform.SetParent(null, true);

            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(throwVelocity, ForceMode.VelocityChange);
        }

        public bool HandleCushionImpact(CushionImpactData impactData, CushionImpactResult impactResult)
        {
            if (!impactResult.IsHandled || rb == null)
                return false;

            isHandled = false;
            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
            return CushionRigidbodyImpactApplier.Apply(transform, rb, impactResult);
        }

        public void IgnoreOwnerCollisionAfterRelease(Transform ownerRoot, float durationSeconds)
        {
            if (ownerRoot == null || explosiveCollider == null || durationSeconds <= 0f)
                return;

            ConfigureHeldHolderCollisionIgnore(ownerRoot);
            ignoreOwnerCollisionUntilTime = Mathf.Max(ignoreOwnerCollisionUntilTime, Time.time + durationSeconds);
        }

        public object CaptureCheckpointState()
        {
            return new ChainExplosiveCheckpointState(isHandled, fuseStarted, exploded, remainingFuseTime, LastReceivedExplosionForce);
        }

        public void RestoreCheckpointState(object state)
        {
            if (state is not ChainExplosiveCheckpointState checkpoint)
                return;

            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();

            isHandled = checkpoint.IsHandled;
            fuseStarted = checkpoint.FuseStarted;
            exploded = checkpoint.Exploded;
            remainingFuseTime = checkpoint.RemainingFuseTime;
            LastReceivedExplosionForce = checkpoint.LastReceivedExplosionForce;

            if (isHandled && transform.parent != null)
                ConfigureHeldHolderCollisionIgnore(transform.parent);
        }

        private void TickReleaseOwnerCollisionIgnore()
        {
            if (ignoreOwnerCollisionUntilTime <= 0f)
                return;

            if (Time.time < ignoreOwnerCollisionUntilTime)
                return;

            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
        }

        public void BeginFuse()
        {
            if (exploded || fuseStarted)
                return;

            fuseStarted = true;
            remainingFuseTime = Mathf.Max(0.05f, fuseTime);

            if (startFuseEffect != null)
                startFuseEffect.Play();

            StartedFuse?.Invoke(this);
        }

        public void OnExplosionImpact(Vector3 direction, float impactForce)
        {
            if (exploded)
                return;

            LastReceivedExplosionForce = Mathf.Max(0.0f, impactForce);

            if (impactForce < triggerExplosionThreshold)
                return;

            BeginFuse();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isHandled || rb == null || explosiveCollider == null)
                return;

            CushionSurfaceMB surface = collision.collider.GetComponentInParent<CushionSurfaceMB>();
            if (surface == null)
                return;

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            float impactForce = collision.impulse.magnitude / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            CushionImpactData impactData = new CushionImpactData(
                gameObject,
                transform,
                entityMB,
                CushionImpactTag,
                rb,
                explosiveCollider,
                collision.contactCount > 0 ? contact.point : transform.position,
                collision.contactCount > 0 ? contact.normal : -collision.relativeVelocity.normalized,
                rb.linearVelocity,
                impactForce);

            if (surface.TryEvaluate(impactData, out CushionImpactResult result))
                HandleCushionImpact(impactData, result);
        }

        private void ExplodeNow()
        {
            if (exploded)
                return;

            exploded = true;
            ClearIgnoredHolderCollisions();

            if (explosionEffectPrefab != null)
            {
                Transform stageRoot = GetComponentInParent<MapRuntimeMB>(true)?.transform;
                SpawnTransientParticleEffect(explosionEffectPrefab, transform.position, Quaternion.identity, stageRoot);
            }

            ExplosionImpactDispatcher.ApplyExplosionImpact(
                transform,
                rb,
                explosionRadius,
                explosionForce,
                explosionImpactResponses);

            Exploded?.Invoke(this);

            if (kernelMB != null &&
                kernelMB.Kernel != null &&
                kernelMB.Kernel.Spawner != null &&
                entityRef.IsValid &&
                kernelMB.Kernel.Spawner.Despawn(entityRef))
            {
                return;
            }

            Destroy(gameObject);
        }

        private void ConfigureHeldHolderCollisionIgnore(Transform handlePoint)
        {
            ClearIgnoredHolderCollisions();

            if (handlePoint == null || explosiveCollider == null)
                return;

            EntityMB ownerEntity = handlePoint.GetComponentInParent<EntityMB>();
            Transform ownerRoot = ownerEntity != null ? ownerEntity.transform : handlePoint.root;
            if (ownerRoot == null)
                return;

            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < ownerColliders.Length; i++)
            {
                Collider ownerCollider = ownerColliders[i];
                if (!CanIgnoreHolderCollider(ownerCollider))
                    continue;

                Physics.IgnoreCollision(explosiveCollider, ownerCollider, true);
                ignoredHolderColliders.Add(ownerCollider);
            }
        }

        private bool CanIgnoreHolderCollider(Collider ownerCollider)
        {
            if (ownerCollider == null ||
                ownerCollider == explosiveCollider)
            {
                return false;
            }

            if (ownerCollider.transform.IsChildOf(transform))
                return false;

            return !ignoredHolderColliders.Contains(ownerCollider);
        }

        private void ClearIgnoredHolderCollisions()
        {
            if (explosiveCollider != null)
            {
                for (int i = 0; i < ignoredHolderColliders.Count; i++)
                {
                    Collider ignored = ignoredHolderColliders[i];
                    if (ignored != null)
                        Physics.IgnoreCollision(explosiveCollider, ignored, false);
                }
            }

            ignoredHolderColliders.Clear();
        }

        private EntityTagId ResolveImpactTag()
        {
            if (entityMB != null && entityMB.Tag.IsValid)
                return entityMB.Tag;

            return EntityTags.Item.Bomb.Id;
        }

        private static void SpawnTransientParticleEffect(ParticleSystem effectPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (effectPrefab == null)
                return;

            ParticleSystem instance = parent != null
                ? Instantiate(effectPrefab, position, rotation, parent)
                : Instantiate(effectPrefab, position, rotation);
            if (instance == null)
                return;

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            float maxLifetime = 0.5f;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                ParticleSystem.MainModule main = system.main;
                float startLifetimeMax = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                    ? main.startLifetime.constantMax
                    : main.startLifetime.constant;
                maxLifetime = Mathf.Max(maxLifetime, main.duration + startLifetimeMax + 0.5f);
            }

            instance.Play(true);
            Destroy(instance.gameObject, maxLifetime);
        }

        private sealed class ChainExplosiveCheckpointState
        {
            public ChainExplosiveCheckpointState(
                bool isHandled,
                bool fuseStarted,
                bool exploded,
                float remainingFuseTime,
                float lastReceivedExplosionForce)
            {
                IsHandled = isHandled;
                FuseStarted = fuseStarted;
                Exploded = exploded;
                RemainingFuseTime = remainingFuseTime;
                LastReceivedExplosionForce = lastReceivedExplosionForce;
            }

            public bool IsHandled { get; }
            public bool FuseStarted { get; }
            public bool Exploded { get; }
            public float RemainingFuseTime { get; }
            public float LastReceivedExplosionForce { get; }
        }
    }
}
