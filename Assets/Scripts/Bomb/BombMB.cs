using System;
using UnityEngine;
using BC.Base;

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
        void OnHandle();
    }

    public sealed class BombMB : MonoBehaviour, IItemObject
    {
        public event Action<BombMB> Exploded;

        [Header("Fuse")]
        [SerializeField] private float fuseTime = 8.0f;

        [Header("Explosion")]
        [SerializeField] private float explosionThreshold = 10f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float explosionForce = 1000f;
        [SerializeField] private ParticleSystem explosionEffectPrefab;
        [SerializeField] private ParticleSystem startFuseEffect;
        [SerializeField] private BombExplosionThresholdDataset thresholdDataset;

        private Rigidbody rb;
        private Collider bombCollider;
        private SceneKernelMB kernelMB;
        private EntityRef entityRef;

        private bool fuseStarted;
        private bool exploded;
        private float remainingFuseTime;

        public bool FuseStarted => fuseStarted;
        public float RemainingFuseTime => remainingFuseTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            bombCollider = GetComponent<Collider>();
            kernelMB = GetComponentInParent<SceneKernelMB>();

            EntityMB entityMB = GetComponentInParent<EntityMB>();
            if (entityMB != null)
                entityRef = entityMB.Entity;

            remainingFuseTime = fuseTime;
        }

        private void Update()
        {
            if (!fuseStarted || exploded)
                return;

            remainingFuseTime -= Time.deltaTime;

            if (remainingFuseTime <= 0f)
            {
                Explode();
            }
        }

        public void BeginFuse()
        {
            if (exploded)
                return;

            if (fuseStarted)
                return;

            fuseStarted = true;
            remainingFuseTime = fuseTime;
            if (startFuseEffect != null)
                startFuseEffect.Play();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (exploded || rb == null || bombCollider == null)
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
                Explode();
            }
        }

        private void Explode()
        {
            if (exploded)
                return;

            exploded = true;

            if (explosionEffectPrefab != null)
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity).Play();

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

            Destroy(gameObject);
        }

        // OnHandle
        public void OnHandle()
        {
            // ここでは特に何もしない。アイテムを扱う処理は、アイテムを扱う側（PlayerItemHandleStateMBなど）で行う想定。
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
                        detector.OnBombImpact(direction, forceMagnitude);

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