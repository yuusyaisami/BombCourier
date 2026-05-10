using System;
using UnityEngine;
using BC.Base;
using BC.Manager;

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

    public interface IItemObject
    {
        Transform ItemTransform { get; }
        bool IsHandled { get; }

        void OnHandle(Transform handlePoint);
        void OnRelease(Vector3 throwVelocity);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class BombMB : MonoBehaviour, IItemObject
    {
        public event Action<BombMB> Exploded;
        public event Action<BombMB> StartedFuse;

        [Header("Fuse")]
        [SerializeField] private float fuseTime = 8.0f;
        [SerializeField] private bool startFuseOnHandle = true;

        [Header("Explosion")]
        [SerializeField] private float explosionThreshold = 10f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float explosionForce = 1000f;
        [SerializeField] private ParticleSystem explosionEffectPrefab;
        [SerializeField] private ParticleSystem startFuseEffect;
        [SerializeField] private BombExplosionThresholdDataset thresholdDataset;
        [Header("Safety")] // 拾った瞬間に爆発するのを防ぐため
        [SerializeField] private float impactExplosionGraceTime = 0.2f;

        private Rigidbody rb;
        private Collider bombCollider;
        private SceneKernelMB kernelMB;
        private EntityRef entityRef;

        private bool fuseStarted;
        private bool exploded;
        private bool isHandled;
        private float remainingFuseTime;
        private float ignoreImpactExplosionUntilTime;

        public Transform ItemTransform => transform;
        public bool IsHandled => isHandled;
        public bool FuseStarted => fuseStarted;
        public float TotalFuseTime => fuseTime;
        public float RemainingFuseTime => remainingFuseTime;
        public float LastImpactForce { get; private set; }
        public float ImpactExplosionRatio => Mathf.Clamp01(LastImpactForce / explosionThreshold);

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            bombCollider = GetComponent<Collider>();
            kernelMB = GetComponentInParent<SceneKernelMB>();

            EntityMB entityMB = GetComponentInParent<EntityMB>();
            if (entityMB != null && entityMB.HasEntity)
            {
                entityRef = entityMB.Entity;
            }

            remainingFuseTime = fuseTime;
        }

        private void Update()
        {
            if (!fuseStarted || exploded)
                return;

            remainingFuseTime -= Time.deltaTime;
            // lastImpactForce は爆発のトリガーにはならないが、爆発エフェクトの演出などに利用できるようにする。
            LastImpactForce = Mathf.Lerp(LastImpactForce, 0f, Time.deltaTime * 5f);
            if (LastImpactForce < 0.01f)
                LastImpactForce = 0f;

            if (remainingFuseTime <= 0f)
            {
                Explode();
            }
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
            rb.detectCollisions = false;

            // 落下しないようにする
            rb.useGravity = false;

            transform.SetParent(handlePoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

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

            if (Time.time < ignoreImpactExplosionUntilTime)
                return;

            float threshold = explosionThreshold;

            if (thresholdDataset.thresholds != null)
            {
                for (int i = 0; i < thresholdDataset.thresholds.Length; i++)
                {
                    var data = thresholdDataset.thresholds[i];

                    if (!string.IsNullOrEmpty(data.unityTag) &&
                        collision.gameObject.CompareTag(data.unityTag))
                    {
                        threshold *= Mathf.Max(0.01f, data.explosionThresholdMultiplier);
                        break;
                    }
                }
            }

            float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;

            if (impactForce >= threshold)
            {
                LastImpactForce = impactForce;
                Explode();
            }
        }

        private void Explode()
        {
            if (exploded)
                return;

            exploded = true;

            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity).Play();
            }

            ApplyExplosionImpact();

            Exploded?.Invoke(this);

            if (kernelMB != null &&
                kernelMB.Kernel != null &&
                kernelMB.Kernel.Spawner != null &&
                entityRef.IsValid &&
                kernelMB.Kernel.Spawner.Despawn(entityRef))
            {
                return;
            }

            GameLogicManagerMB.Instance.SetCurrentBomb(null); // 爆弾が爆発したらGameLogicManagerに通知する

            Destroy(gameObject);
        }

        private void ApplyExplosionImpact()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                if (hit == null)
                    continue;

                Vector3 direction = hit.transform.position - transform.position;
                float distance = Mathf.Max(0.1f, direction.magnitude);
                direction /= distance;

                float forceMagnitude = Mathf.Clamp(
                    explosionForce / (distance * distance),
                    0f,
                    explosionForce
                );

                if (hit.TryGetComponent(out Rigidbody hitRb) && hitRb != rb)
                {
                    if (hitRb.TryGetComponent(out IBombImpactDetector detector))
                    {
                        detector.OnBombImpact(direction, forceMagnitude);
                    }

                    hitRb.AddForce(direction * forceMagnitude, ForceMode.Impulse);
                }

                if (hit.TryGetComponent(out IBombImpactReceiver receiver))
                {
                    receiver.OnBombImpactReceived(direction, forceMagnitude);
                }
            }
        }
    }
}