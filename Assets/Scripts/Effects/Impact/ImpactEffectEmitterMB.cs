using UnityEngine;

namespace BC.Effects.Impact
{
    [DisallowMultipleComponent]
    public sealed class ImpactEffectEmitterMB : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private ImpactParticleManagerMB impactManager;
        [SerializeField] private PooledImpactParticleMB fallbackParticlePrefab;
        [SerializeField] private Color defaultColor = new(0.78f, 0.72f, 0.62f, 0.75f);

        [Header("Auto Impact")]
        [SerializeField] private bool playOnStrongCollision = true;
        [SerializeField, Min(0.0f)] private float minAutoImpactStrength = 6.0f;
        [SerializeField, Min(0.01f)] private float referenceImpactStrength = 12.0f;
        [SerializeField, Min(0.0f)] private float autoCooldownSeconds = 0.12f;

        [Header("External Playback")]
        [SerializeField, Min(0.0f)] private float defaultExternalImpactStrength = 8.0f;
        [SerializeField, Min(0.0f)] private float contactForgetSeconds = 0.25f;

        [Header("Probe")]
        [SerializeField] private LayerMask probeLayerMask = ~0;
        [SerializeField, Min(0.0f)] private float probeRadius = 0.2f;
        [SerializeField, Min(0.01f)] private float probeDistance = 2.0f;
        [SerializeField] private QueryTriggerInteraction probeTriggerInteraction = QueryTriggerInteraction.Ignore;

        private const int MaxProbeHits = 8;

        private readonly RaycastHit[] probeHits = new RaycastHit[MaxProbeHits];
        private ContactSnapshot latestContact;
        private float nextAutoPlayTime;

        private void OnValidate()
        {
            minAutoImpactStrength = Mathf.Max(0.0f, minAutoImpactStrength);
            referenceImpactStrength = Mathf.Max(0.01f, referenceImpactStrength);
            autoCooldownSeconds = Mathf.Max(0.0f, autoCooldownSeconds);
            defaultExternalImpactStrength = Mathf.Max(0.0f, defaultExternalImpactStrength);
            contactForgetSeconds = Mathf.Max(0.0f, contactForgetSeconds);
            probeRadius = Mathf.Max(0.0f, probeRadius);
            probeDistance = Mathf.Max(0.01f, probeDistance);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessCollision(collision, true);
        }

        private void OnCollisionStay(Collision collision)
        {
            ProcessCollision(collision, false);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision == null || collision.collider == null)
                return;

            if (latestContact.IsValid && latestContact.Collider == collision.collider)
                latestContact = default;
        }

        public bool PlayFromCurrentContact()
        {
            return PlayFromCurrentContact(defaultExternalImpactStrength);
        }

        public bool PlayFromCurrentContact(float impactStrength)
        {
            if (!TryGetCurrentContact(out ContactSnapshot contact))
                return false;

            float resolvedStrength = impactStrength > 0.0f ? impactStrength : contact.ImpactStrength;
            return PlayAt(contact.Point, contact.Normal, contact.Collider, resolvedStrength);
        }

        public bool PlayAt(Vector3 point, Vector3 normal, Collider surfaceCollider, float impactStrength)
        {
            ImpactEffectRequest request = new ImpactEffectRequest(
                point,
                normal,
                surfaceCollider,
                impactStrength,
                referenceImpactStrength,
                defaultColor);

            return TryPlay(request);
        }

        public bool TryPlayFromProbe(Vector3 origin, Vector3 direction)
        {
            return TryPlayFromProbe(origin, direction, probeDistance, defaultExternalImpactStrength);
        }

        public bool TryPlayFromProbe(Vector3 origin, Vector3 direction, float distance, float impactStrength)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            Vector3 normalizedDirection = direction.normalized;
            float resolvedDistance = Mathf.Max(0.01f, distance);
            int hitCount = probeRadius > 0.0f
                ? Physics.SphereCastNonAlloc(origin, probeRadius, normalizedDirection, probeHits, resolvedDistance, probeLayerMask, probeTriggerInteraction)
                : Physics.RaycastNonAlloc(origin, normalizedDirection, probeHits, resolvedDistance, probeLayerMask, probeTriggerInteraction);

            if (!TrySelectProbeHit(hitCount, out RaycastHit hit))
                return false;

            ImpactEffectRequest request = new ImpactEffectRequest(
                hit.point,
                hit.normal,
                hit.collider,
                impactStrength,
                referenceImpactStrength,
                defaultColor);

            return TryPlay(request);
        }

        private void ProcessCollision(Collision collision, bool canAutoPlay)
        {
            if (collision == null || collision.collider == null)
                return;

            float impactStrength = CalculateImpactStrength(collision);
            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Vector3 point = collision.contactCount > 0 ? contact.point : collision.collider.ClosestPoint(transform.position);
            Vector3 normal = collision.contactCount > 0 && contact.normal.sqrMagnitude > 0.0001f
                ? contact.normal
                : ResolveFallbackNormal(collision.collider, point);

            latestContact = new ContactSnapshot(collision.collider, point, normal, impactStrength, Time.time);

            if (!canAutoPlay || !playOnStrongCollision || Time.time < nextAutoPlayTime)
                return;

            if (impactStrength < minAutoImpactStrength)
                return;

            nextAutoPlayTime = Time.time + autoCooldownSeconds;
            PlayAt(point, normal, collision.collider, impactStrength);
        }

        private bool TryGetCurrentContact(out ContactSnapshot contact)
        {
            contact = latestContact;

            if (!contact.IsValid)
                return false;

            if (Time.time - contact.Time > contactForgetSeconds)
                return false;

            return true;
        }

        private float CalculateImpactStrength(Collision collision)
        {
            float relativeSpeed = collision.relativeVelocity.magnitude;
            float impulseStrength = collision.impulse.magnitude / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            return Mathf.Max(relativeSpeed, impulseStrength);
        }

        private Vector3 ResolveFallbackNormal(Collider surfaceCollider, Vector3 point)
        {
            if (surfaceCollider == null)
                return Vector3.up;

            Vector3 normal = transform.position - point;
            return normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        }

        private bool TrySelectProbeHit(int hitCount, out RaycastHit selectedHit)
        {
            selectedHit = default;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = probeHits[i];

                if (hit.collider == null || hit.transform.IsChildOf(transform))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                selectedHit = hit;
                bestDistance = hit.distance;
            }

            return selectedHit.collider != null;
        }

        private bool TryPlay(in ImpactEffectRequest request)
        {
            ImpactParticleManagerMB manager = ResolveImpactManager();
            return manager != null && manager.TryPlay(request);
        }

        private ImpactParticleManagerMB ResolveImpactManager()
        {
            if (impactManager == null)
                impactManager = ImpactParticleManagerMB.Instance;

            if (impactManager == null && fallbackParticlePrefab != null)
                impactManager = ImpactParticleManagerMB.EnsureInstance();

            if (impactManager != null && fallbackParticlePrefab != null)
                impactManager.SetPrefab(fallbackParticlePrefab);

            return impactManager;
        }

        private readonly struct ContactSnapshot
        {
            public readonly Collider Collider;
            public readonly Vector3 Point;
            public readonly Vector3 Normal;
            public readonly float ImpactStrength;
            public readonly float Time;

            public ContactSnapshot(Collider collider, Vector3 point, Vector3 normal, float impactStrength, float time)
            {
                Collider = collider;
                Point = point;
                Normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
                ImpactStrength = Mathf.Max(0.0f, impactStrength);
                Time = time;
            }

            public bool IsValid => Collider != null;
        }
    }
}