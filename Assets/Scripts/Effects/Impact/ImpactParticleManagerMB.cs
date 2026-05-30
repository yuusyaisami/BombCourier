using BC.Audio;
using UnityEngine;
using UnityEngine.Pool;

namespace BC.Effects.Impact
{
    [DisallowMultipleComponent]
    public sealed class ImpactParticleManagerMB : MonoBehaviour
    {
        public static ImpactParticleManagerMB Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] private PooledImpactParticleMB impactParticlePrefab;

        [Header("Pool")]
        [SerializeField] private Transform poolRoot;
        [SerializeField, Min(0)] private int defaultCapacity = 16;
        [SerializeField, Min(1)] private int maxPoolSize = 96;

        [Header("Playback")]
        [SerializeField, Min(0.0f)] private float surfaceOffset = 0.015f;
        [SerializeField, Min(0.0f)] private float minSpeedMultiplier = 0.75f;
        [SerializeField, Min(0.0f)] private float maxSpeedMultiplier = 2.25f;

        [Header("Sound")]
        // 衝撃時に再生するサウンド。衝撃の強さに応じて音量が変わる。
        [SerializeField] private AudioDataSO impactSound;
        // この値以上の NormalizedStrength のときだけサウンドを再生する。
        [SerializeField, Range(0f, 1f)] private float minImpactStrengthForSound = 0.1f;

        private ObjectPool<PooledImpactParticleMB> particlePool;
        private bool isDestroying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsurePool();
        }

        private void OnDestroy()
        {
            isDestroying = true;

            if (Instance == this)
                Instance = null;

            particlePool?.Clear();
            particlePool = null;
        }

        public static ImpactParticleManagerMB EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            ImpactParticleManagerMB found = Object.FindAnyObjectByType<ImpactParticleManagerMB>();
            if (found != null)
                return found;

            GameObject managerObject = new GameObject(nameof(ImpactParticleManagerMB));
            return managerObject.AddComponent<ImpactParticleManagerMB>();
        }

        public bool TryPlay(in ImpactEffectRequest request)
        {
            if (impactParticlePrefab == null)
                return false;

            EnsurePool();

            Color baseColor = ImpactMaterialColorResolver.ResolveColor(request);
            float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, request.NormalizedStrength);
            PooledImpactParticleMB particle = particlePool.Get();
            particle.Play(request, baseColor, speedMultiplier, surfaceOffset, ReleaseParticle);

            // 衝撃強度に応じた音を、ヒット位置から 3D 再生する。
            if (impactSound != null && impactSound.Clip != null && request.NormalizedStrength >= minImpactStrengthForSound)
            {
                float volumeScale = impactSound.BaseVolume * request.NormalizedStrength;
                AudioSource.PlayClipAtPoint(impactSound.Clip, request.Point, volumeScale);
            }

            return true;
        }

        public void SetPrefab(PooledImpactParticleMB prefab)
        {
            if (impactParticlePrefab == prefab)
                return;

            impactParticlePrefab = prefab;
            particlePool?.Clear();
            particlePool = null;
            EnsurePool();
        }

        private void EnsurePool()
        {
            if (particlePool != null || impactParticlePrefab == null)
                return;

            if (poolRoot == null)
            {
                GameObject rootObject = new GameObject("Impact Particle Pool");
                rootObject.transform.SetParent(transform, false);
                poolRoot = rootObject.transform;
            }

            particlePool = new ObjectPool<PooledImpactParticleMB>(
                CreateParticle,
                particle =>
                {
                    if (particle != null)
                        particle.gameObject.SetActive(true);
                },
                particle =>
                {
                    if (particle == null)
                        return;

                    particle.StopAndReset();
                    particle.gameObject.SetActive(false);
                    particle.transform.SetParent(poolRoot, false);
                },
                particle =>
                {
                    if (particle != null)
                        Destroy(particle.gameObject);
                },
                false,
                defaultCapacity,
                Mathf.Max(1, maxPoolSize));
        }

        private PooledImpactParticleMB CreateParticle()
        {
            PooledImpactParticleMB particle = Instantiate(impactParticlePrefab, poolRoot);
            particle.gameObject.SetActive(false);
            particle.EnsureInitialized();
            return particle;
        }

        private void ReleaseParticle(PooledImpactParticleMB particle)
        {
            if (particle == null)
                return;

            if (isDestroying || particlePool == null)
            {
                Destroy(particle.gameObject);
                return;
            }

            particlePool.Release(particle);
        }
    }
}