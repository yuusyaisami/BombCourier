using System;
using System.Collections.Generic;
using UnityEngine;
using BC.Base;
using BC.Gimmick.Cushion;
using BC.Item;
using BC.Manager;
using BC.Player;
using BC.Stage;

namespace BC.Bomb
{
    [Serializable]
    public struct BombExplosionThresholdData
    {
        public string unityTag;
        public float explosionThresholdMultiplier;
    }

    [Serializable]
    public struct BombExplosionThresholdDataset
    {
        public BombExplosionThresholdData[] thresholds;
    }

    public interface IBombImpactDetector
    {
        void OnBombImpact(Vector3 direction, float impactForce);
    }

    public interface IBombImpactReceiver
    {
        void OnBombImpactReceived(Vector3 direction, float impactForce);
    }

    public interface IExplosionImpactDetector
    {
        void OnExplosionImpact(Vector3 direction, float impactForce);
    }

    public interface IExplosionImpactReceiver
    {
        void OnExplosionImpactReceived(Vector3 direction, float impactForce);
    }

    public static class ExplosionImpactDispatcher
    {
        public static void ApplyExplosionImpact(
            Transform sourceTransform,
            Rigidbody sourceRigidbody,
            float explosionRadius,
            float explosionForce,
            List<EntityImpactResponseMB> impactResponseBuffer)
        {
            if (sourceTransform == null || impactResponseBuffer == null)
                return;

            Collider[] hits = Physics.OverlapSphere(sourceTransform.position, explosionRadius);
            impactResponseBuffer.Clear();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                if (hit == null)
                    continue;

                Vector3 hitPoint = hit.ClosestPoint(sourceTransform.position);
                Vector3 direction = hitPoint - sourceTransform.position;

                if (direction.sqrMagnitude <= 0.0001f)
                    direction = hit.transform.position - sourceTransform.position;

                float distance = Mathf.Max(0.1f, direction.magnitude);
                direction /= distance;

                float forceMagnitude = Mathf.Clamp(
                    explosionForce / (distance * distance),
                    0f,
                    explosionForce);

                bool handledByEntityImpactResponse = TryHandleEntityExplosionImpact(
                    sourceTransform,
                    sourceRigidbody,
                    hit,
                    hitPoint,
                    direction,
                    forceMagnitude,
                    impactResponseBuffer);

                Rigidbody hitRigidbody = hit.attachedRigidbody;
                if (hitRigidbody != null && hitRigidbody != sourceRigidbody)
                {
                    NotifyImpactDetector(hitRigidbody, direction, forceMagnitude);

                    if (!handledByEntityImpactResponse)
                        hitRigidbody.AddForce(direction * forceMagnitude, ForceMode.Impulse);
                }

                NotifyImpactReceiver(hit, direction, forceMagnitude);
            }

            impactResponseBuffer.Clear();
        }

        private static void NotifyImpactDetector(Rigidbody hitRigidbody, Vector3 direction, float forceMagnitude)
        {
            if (hitRigidbody.TryGetComponent(out IExplosionImpactDetector explosionDetector))
            {
                explosionDetector.OnExplosionImpact(direction, forceMagnitude);
                return;
            }

            if (hitRigidbody.TryGetComponent(out IBombImpactDetector legacyDetector))
                legacyDetector.OnBombImpact(direction, forceMagnitude);
        }

        private static void NotifyImpactReceiver(Collider hitCollider, Vector3 direction, float forceMagnitude)
        {
            if (hitCollider.TryGetComponent(out IExplosionImpactReceiver explosionReceiver))
            {
                explosionReceiver.OnExplosionImpactReceived(direction, forceMagnitude);
                return;
            }

            if (hitCollider.TryGetComponent(out IBombImpactReceiver legacyReceiver))
                legacyReceiver.OnBombImpactReceived(direction, forceMagnitude);
        }

        private static bool TryHandleEntityExplosionImpact(
            Transform sourceTransform,
            Rigidbody sourceRigidbody,
            Collider hit,
            Vector3 hitPoint,
            Vector3 direction,
            float forceMagnitude,
            List<EntityImpactResponseMB> impactResponseBuffer)
        {
            if (hit == null || hit.attachedRigidbody == sourceRigidbody || hit.transform.IsChildOf(sourceTransform))
                return false;

            EntityImpactResponseMB impactResponse = hit.GetComponentInParent<EntityImpactResponseMB>();
            if (impactResponse == null)
                return false;

            if (impactResponseBuffer.Contains(impactResponse))
                return true;

            impactResponseBuffer.Add(impactResponse);

            EntityImpactData impactData = new EntityImpactData(
                EntityImpactKind.Explosion,
                sourceTransform.gameObject,
                sourceTransform,
                hit,
                hitPoint,
                direction,
                forceMagnitude);

            impactResponse.TryApplyImpact(impactData);
            return true;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class BombMB : MonoBehaviour, ICarryableItem, ICarryMoveModifier, ICushionImpactSource, IBombImpactDetector, IExplosionImpactDetector, IInteractionPromptDetailTextProvider, IStageCheckpointParticipant
    {
        public event Action<BombMB> Exploded;
        public event Action<BombMB> StartedFuse;

        [Header("Fuse")]
        [SerializeField] private float fuseTime = 8.0f;
        [SerializeField] private bool startFuseOnHandle = true;

        [Header("Carry")]
        [SerializeField, Range(0.0f, 1.0f)] private float carryJumpHeightMultiplier = 0.65f;

        [Header("Explosion")]
        [SerializeField] private float explosionThreshold = 10f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float explosionForce = 1000f;
        [SerializeField] private ParticleSystem explosionEffectPrefab;
        [SerializeField] private ParticleSystem startFuseEffect;
        [SerializeField] private BombExplosionThresholdDataset thresholdDataset;
        [Header("Safety")] // 拾った瞬間に爆発するのを防ぐため
        [SerializeField] private float impactExplosionGraceTime = 0.2f;
        [SerializeField] private float heldImpactExplosionSpeed = 2.0f;
        [SerializeField] private float heldCollisionProbePadding = 0.02f;

        private Rigidbody rb;
        private Collider bombCollider;
        private SceneKernelMB kernelMB;
        private EntityMB entityMB;
        private EntityRef entityRef;

        private bool fuseStarted;
        private bool exploded;
        private bool isHandled;
        private float remainingFuseTime;
        private float ignoreImpactExplosionUntilTime;
        private bool hasPreviousHeldPosition;
        private Vector3 previousHeldPosition;
        private float lastImpactThreshold;

        private const int MaxHeldCollisionHits = 16;
        private readonly Collider[] heldCollisionHits = new Collider[MaxHeldCollisionHits];
        private readonly List<Collider> ignoredPlayerColliders = new(16);
        private readonly List<EntityImpactResponseMB> explosionImpactResponses = new(16);

        public Transform ItemTransform => transform;
        public bool IsHandled => isHandled;
        public bool CanBeCarried => !exploded;
        public Transform CushionImpactRoot => transform;
        public EntityTagId CushionImpactTag => ResolveImpactTag();
        public bool FuseStarted => fuseStarted;
        public bool HasExploded => exploded;
        public float TotalFuseTime => fuseTime;
        public float RemainingFuseTime => remainingFuseTime;
        public string PromptDetailText => exploded ? string.Empty : $"{ResolvePromptDetailSeconds()}s";
        public float LastImpactForce { get; private set; }
        public float ImpactExplosionRatio => Mathf.Clamp01(LastImpactForce / Mathf.Max(0.01f, lastImpactThreshold));

        public bool TryGetJumpHeightMultiplier(out float jumpHeightMultiplier)
        {
            jumpHeightMultiplier = carryJumpHeightMultiplier;
            return true;
        }

        private int ResolvePromptDetailSeconds()
        {
            float seconds = fuseStarted ? remainingFuseTime : fuseTime;
            return Mathf.Max(0, Mathf.CeilToInt(seconds));
        }

        public object CaptureCheckpointState()
        {
            return new BombCheckpointState(
                transform.parent,
                fuseStarted,
                exploded,
                isHandled,
                remainingFuseTime,
                LastImpactForce,
                lastImpactThreshold,
                rb != null && rb.isKinematic,
                rb != null && rb.useGravity,
                rb == null || rb.detectCollisions,
                rb != null ? rb.linearVelocity : Vector3.zero,
                rb != null ? rb.angularVelocity : Vector3.zero);
        }

            // Retry checkpoint では「起爆開始時接触」を基準に、未起爆状態へ戻せるスナップショットを使う。
            public object CaptureRetryCheckpointState()
            {
                return new BombCheckpointState(
                transform.parent,
                false,
                false,
                isHandled,
                fuseTime,
                0f,
                lastImpactThreshold,
                rb != null && rb.isKinematic,
                rb != null && rb.useGravity,
                rb == null || rb.detectCollisions,
                Vector3.zero,
                Vector3.zero);
            }

        public void RestoreCheckpointState(object state)
        {
            if (state is not BombCheckpointState checkpointState)
                return;

            ClearIgnoredPlayerCollisions();
            transform.SetParent(checkpointState.Parent, true);

            fuseStarted = checkpointState.FuseStarted;
            exploded = checkpointState.Exploded;
            isHandled = checkpointState.IsHandled;
            remainingFuseTime = checkpointState.RemainingFuseTime;
            LastImpactForce = checkpointState.LastImpactForce;
            lastImpactThreshold = Mathf.Max(0.01f, checkpointState.LastImpactThreshold);
            ignoreImpactExplosionUntilTime = 0f;
            hasPreviousHeldPosition = false;
            previousHeldPosition = transform.position;

            if (rb != null)
            {
                rb.isKinematic = checkpointState.IsKinematic;
                rb.useGravity = checkpointState.UseGravity;
                rb.detectCollisions = checkpointState.DetectCollisions;
                rb.linearVelocity = checkpointState.LinearVelocity;
                rb.angularVelocity = checkpointState.AngularVelocity;
            }

            if (startFuseEffect != null)
            {
                if (fuseStarted && gameObject.activeInHierarchy)
                    startFuseEffect.Play();
                else
                    startFuseEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            bombCollider = GetComponent<Collider>();
            kernelMB = GetComponentInParent<SceneKernelMB>();

            entityMB = GetComponentInParent<EntityMB>();
            if (entityMB != null && entityMB.HasEntity)
            {
                entityRef = entityMB.Entity;
            }

            remainingFuseTime = fuseTime;
            lastImpactThreshold = Mathf.Max(0.01f, explosionThreshold);
        }

        private void OnDisable()
        {
            ClearIgnoredPlayerCollisions();
            hasPreviousHeldPosition = false;
        }

        private void Update()
        {
            if (exploded)
                return;

            TickImpactForce(Time.deltaTime);

            if (!fuseStarted)
                return;

            TickFuse(Time.deltaTime);
        }

        private void TickFuse(float dt)
        {
            // 将来ヒューズ停止ギミックを入れる時は、この入口で停止条件を差し込める。
            remainingFuseTime -= dt;

            if (remainingFuseTime <= 0f)
            {
                Explode();
            }
        }

        private void TickImpactForce(float dt)
        {
            // lastImpactForce は爆発のトリガーにはならないが、爆発エフェクトの演出などに利用できるようにする。
            LastImpactForce = Mathf.Lerp(LastImpactForce, 0f, dt * 5f);
            if (LastImpactForce < 0.01f)
                LastImpactForce = 0f;
        }

        private void FixedUpdate()
        {
            if (!isHandled || exploded || rb == null || bombCollider == null)
            {
                hasPreviousHeldPosition = false;
                return;
            }

            Vector3 currentPosition = transform.position;

            if (!hasPreviousHeldPosition)
            {
                previousHeldPosition = currentPosition;
                hasPreviousHeldPosition = true;
                return;
            }

            float heldSpeed = (currentPosition - previousHeldPosition).magnitude /
                              Mathf.Max(Time.fixedDeltaTime, 0.0001f);

            previousHeldPosition = currentPosition;

            if (Time.time < ignoreImpactExplosionUntilTime)
                return;

            if (heldSpeed < heldImpactExplosionSpeed)
                return;

            if (!IsTouchingNonPlayerCollider())
                return;

            RecordImpactForce(heldSpeed, explosionThreshold);
            Explode();
        }

        public void OnHandle(Transform handlePoint)
        {
            if (handlePoint == null)
            {
                Debug.LogError($"{nameof(BombMB)}: Handle point is null.", this);
                return;
            }

            if (exploded)
                return;

            isHandled = true;
            ignoreImpactExplosionUntilTime = Time.time + impactExplosionGraceTime;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = true;

            // 落下しないようにする
            rb.useGravity = false;
            ConfigureHeldPlayerCollisionIgnore(handlePoint);

            transform.SetParent(handlePoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            previousHeldPosition = transform.position;
            hasPreviousHeldPosition = true;

            if (startFuseOnHandle)
            {
                BeginFuse();
            }
        }

        public void OnRelease(Vector3 throwVelocity)
        {
            if (exploded)
                return;

            isHandled = false;
            ignoreImpactExplosionUntilTime = Time.time + impactExplosionGraceTime;
            hasPreviousHeldPosition = false;
            ClearIgnoredPlayerCollisions();

            transform.SetParent(null, true);

            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(throwVelocity, ForceMode.VelocityChange);
        }

        public void BeginFuse()
        {
            if (exploded || fuseStarted)
                return;

            fuseStarted = true;
            remainingFuseTime = Mathf.Max(0.1f, fuseTime);

            if (startFuseEffect != null)
            {
                startFuseEffect.Play();
            }

            StartedFuse?.Invoke(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (exploded || rb == null || bombCollider == null || isHandled)
                return;

            float rawImpactForce = collision.relativeVelocity.magnitude;

            if (TryHandleCushionCollision(collision, rawImpactForce))
                return;

            if (Time.time < ignoreImpactExplosionUntilTime)
                return;

            float impactForce = ResolveEffectiveImpactForce(rawImpactForce, collision.gameObject);
            RecordImpactForce(impactForce, explosionThreshold);

            if (impactForce >= explosionThreshold)
                Explode();
        }

        public bool HandleCushionImpact(CushionImpactData impactData, CushionImpactResult impactResult)
        {
            if (!impactResult.IsHandled || exploded || rb == null)
                return false;

            isHandled = false;
            ClearIgnoredPlayerCollisions();
            hasPreviousHeldPosition = false;
            ignoreImpactExplosionUntilTime = Time.time + impactExplosionGraceTime;

            CushionImpactResult appliedResult = impactResult.ResponseKind == CushionResponseKind.StopAndAttach
                ? CushionImpactResult.Stop(impactResult.SuppressExplosion)
                : impactResult;

            if (appliedResult.ResponseKind == CushionResponseKind.Stop)
            {
                transform.SetParent(null, true);

                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                return true;
            }

            return CushionRigidbodyImpactApplier.Apply(transform, rb, appliedResult);
        }

        public void OnBombImpact(Vector3 direction, float impactForce)
        {
            if (exploded)
                return;

            float threshold = Mathf.Max(0.01f, explosionThreshold);
            RecordImpactForce(impactForce, threshold);

            if (impactForce >= threshold)
                Explode();
        }

        public void OnExplosionImpact(Vector3 direction, float impactForce)
        {
            OnBombImpact(direction, impactForce);
        }

        private bool TryHandleCushionCollision(Collision collision, float impactForce)
        {
            CushionSurfaceMB surface = collision.collider.GetComponentInParent<CushionSurfaceMB>();

            if (surface == null)
                return false;

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Vector3 normal = collision.contactCount > 0
                ? contact.normal
                : -collision.relativeVelocity.normalized;

            CushionImpactData impactData = new CushionImpactData(
                gameObject,
                transform,
                entityMB,
                CushionImpactTag,
                rb,
                bombCollider,
                collision.contactCount > 0 ? contact.point : transform.position,
                normal,
                rb.linearVelocity,
                impactForce);

            if (!surface.TryEvaluate(impactData, out CushionImpactResult result))
                return false;

            if (!result.SuppressExplosion)
                RecordImpactForce(
                    ResolveEffectiveImpactForce(impactForce, collision.collider != null ? collision.collider.gameObject : null),
                    explosionThreshold);

            return HandleCushionImpact(impactData, result);
        }

        private float ResolveEffectiveImpactForce(float rawImpactForce, GameObject otherObject)
        {
            float multiplier = 1.0f;

            if (otherObject == null || thresholdDataset.thresholds == null)
                return Mathf.Max(0.0f, rawImpactForce);

            for (int i = 0; i < thresholdDataset.thresholds.Length; i++)
            {
                BombExplosionThresholdData data = thresholdDataset.thresholds[i];

                if (!string.IsNullOrEmpty(data.unityTag) && otherObject.CompareTag(data.unityTag))
                {
                    multiplier = Mathf.Max(0.0f, data.explosionThresholdMultiplier);
                    break;
                }
            }

            return Mathf.Max(0.0f, rawImpactForce) * multiplier;
        }

        private void RecordImpactForce(float impactForce, float threshold)
        {
            LastImpactForce = Mathf.Max(0.0f, impactForce);
            lastImpactThreshold = Mathf.Max(0.01f, threshold);
        }

        private EntityTagId ResolveImpactTag()
        {
            if (entityMB != null && entityMB.Tag.IsValid)
                return entityMB.Tag;

            return EntityTags.Item.Bomb.Id;
        }

        private void Explode()
        {
            if (exploded)
                return;

            exploded = true;
            ClearIgnoredPlayerCollisions();
            hasPreviousHeldPosition = false;

            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity).Play();
            }

            ApplyExplosionImpact();

            Exploded?.Invoke(this);

            if (TryHandleCheckpointableExplosion())
                return;

            if (kernelMB != null &&
                kernelMB.Kernel != null &&
                kernelMB.Kernel.Spawner != null &&
                entityRef.IsValid &&
                kernelMB.Kernel.Spawner.Despawn(entityRef))
            {
                return;
            }

            if (GameLogicManagerMB.Instance != null)
                GameLogicManagerMB.Instance.SetCurrentBomb(null); // 爆弾が爆発したらGameLogicManagerに通知する

            Destroy(gameObject);
        }

        private bool TryHandleCheckpointableExplosion()
        {
            bool hasStageSaveMark = TryGetComponent(out StageSaveMarkMB _);
            bool hasRetryCheckpoint = GameLogicManagerMB.Instance != null && GameLogicManagerMB.Instance.HasRetryCheckpoint;
            if (!hasStageSaveMark && !hasRetryCheckpoint)
                return false;

            if (GameLogicManagerMB.Instance != null)
                GameLogicManagerMB.Instance.SetCurrentBomb(null);

            gameObject.SetActive(false);
            return true;
        }

        private void ConfigureHeldPlayerCollisionIgnore(Transform handlePoint)
        {
            ClearIgnoredPlayerCollisions();

            if (handlePoint == null || bombCollider == null)
                return;

            CharacterController ownerController = handlePoint.GetComponentInParent<CharacterController>();
            Transform ownerRoot = ownerController != null
                ? ownerController.transform
                : handlePoint.root;

            if (ownerRoot == null)
                return;

            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < ownerColliders.Length; i++)
            {
                Collider ownerCollider = ownerColliders[i];

                if (!CanIgnorePlayerCollider(ownerCollider))
                    continue;

                Physics.IgnoreCollision(bombCollider, ownerCollider, true);
                ignoredPlayerColliders.Add(ownerCollider);
            }
        }

        private bool CanIgnorePlayerCollider(Collider ownerCollider)
        {
            if (ownerCollider == null ||
                ownerCollider == bombCollider ||
                !ownerCollider.enabled ||
                !ownerCollider.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (ownerCollider.transform.IsChildOf(transform))
                return false;

            return !ignoredPlayerColliders.Contains(ownerCollider);
        }

        private void ClearIgnoredPlayerCollisions()
        {
            if (bombCollider != null)
            {
                for (int i = 0; i < ignoredPlayerColliders.Count; i++)
                {
                    Collider ignored = ignoredPlayerColliders[i];

                    if (ignored != null)
                        Physics.IgnoreCollision(bombCollider, ignored, false);
                }
            }

            ignoredPlayerColliders.Clear();
        }

        private sealed class BombCheckpointState
        {
            public BombCheckpointState(
                Transform parent,
                bool fuseStarted,
                bool exploded,
                bool isHandled,
                float remainingFuseTime,
                float lastImpactForce,
                float lastImpactThreshold,
                bool isKinematic,
                bool useGravity,
                bool detectCollisions,
                Vector3 linearVelocity,
                Vector3 angularVelocity)
            {
                Parent = parent;
                FuseStarted = fuseStarted;
                Exploded = exploded;
                IsHandled = isHandled;
                RemainingFuseTime = remainingFuseTime;
                LastImpactForce = lastImpactForce;
                LastImpactThreshold = lastImpactThreshold;
                IsKinematic = isKinematic;
                UseGravity = useGravity;
                DetectCollisions = detectCollisions;
                LinearVelocity = linearVelocity;
                AngularVelocity = angularVelocity;
            }

            public Transform Parent { get; }
            public bool FuseStarted { get; }
            public bool Exploded { get; }
            public bool IsHandled { get; }
            public float RemainingFuseTime { get; }
            public float LastImpactForce { get; }
            public float LastImpactThreshold { get; }
            public bool IsKinematic { get; }
            public bool UseGravity { get; }
            public bool DetectCollisions { get; }
            public Vector3 LinearVelocity { get; }
            public Vector3 AngularVelocity { get; }
        }

        private bool IsTouchingNonPlayerCollider()
        {
            Bounds bounds = bombCollider.bounds;
            float probeRadius = bounds.extents.magnitude + Mathf.Max(0.0f, heldCollisionProbePadding);

            int hitCount = Physics.OverlapSphereNonAlloc(
                bounds.center,
                probeRadius,
                heldCollisionHits,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = heldCollisionHits[i];

                if (ShouldIgnoreHeldCollision(hit))
                    continue;

                if (Physics.ComputePenetration(
                        bombCollider,
                        transform.position,
                        transform.rotation,
                        hit,
                        hit.transform.position,
                        hit.transform.rotation,
                        out _,
                        out float distance) &&
                    distance > 0.0001f)
                {
                    return true;
                }

                if (bombCollider.bounds.Intersects(hit.bounds))
                    return true;
            }

            return false;
        }

        private bool ShouldIgnoreHeldCollision(Collider hit)
        {
            if (hit == null ||
                hit == bombCollider ||
                hit.attachedRigidbody == rb ||
                hit.transform.IsChildOf(transform))
            {
                return true;
            }

            if (hit.GetComponentInParent<CushionSurfaceMB>() != null)
                return true;

            return ignoredPlayerColliders.Contains(hit);
        }

        private void ApplyExplosionImpact()
        {
            ExplosionImpactDispatcher.ApplyExplosionImpact(
                transform,
                rb,
                explosionRadius,
                explosionForce,
                explosionImpactResponses);
        }
    }
}
