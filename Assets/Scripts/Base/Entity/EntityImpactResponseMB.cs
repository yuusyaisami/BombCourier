using UnityEngine;
using UnityEngine.Events;

namespace BC.Base
{
    public enum EntityImpactKind
    {
        Explosion,
        Contact
    }

    public readonly struct EntityImpactData
    {
        public readonly EntityImpactKind Kind;
        public readonly GameObject SourceObject;
        public readonly Transform SourceRoot;
        public readonly Collider TargetCollider;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly float ForceMagnitude;

        public EntityImpactData(
            EntityImpactKind kind,
            GameObject sourceObject,
            Transform sourceRoot,
            Collider targetCollider,
            Vector3 point,
            Vector3 direction,
            float forceMagnitude)
        {
            Kind = kind;
            SourceObject = sourceObject;
            SourceRoot = sourceRoot;
            TargetCollider = targetCollider;
            Point = point;
            Direction = direction;
            ForceMagnitude = forceMagnitude;
        }
    }

    public interface IEntityImpactReceiver
    {
        bool TryApplyImpact(in EntityImpactData impactData);
    }

    [DisallowMultipleComponent]
    public sealed class EntityImpactResponseMB : MonoBehaviour, IEntityImpactReceiver
    {
        [Header("Target")]
        [SerializeField] private Rigidbody targetRigidbody;

        [Header("Explosion")]
        [SerializeField] private bool receiveExplosionImpact = true;
        [SerializeField, Min(0.0f)] private float explosionForceMultiplier = 1.0f;
        [SerializeField, Min(0.0f)] private float minimumExplosionForce = 0.0f;
        [SerializeField] private bool makeRigidbodyDynamicAfterExplosion;
        [SerializeField] private bool useGravityAfterExplosion = true;
        [SerializeField] private bool enableContactImpactAfterExplosion;

        [Header("Contact")]
        [SerializeField] private bool receiveContactImpact = true;
        [SerializeField] private bool contactImpactEnabledOnStart = true;
        [SerializeField, Min(0.0f)] private float contactForceMultiplier = 1.0f;
        [SerializeField, Min(0.0f)] private float minimumContactForce = 0.0f;

        [Header("Events")]
        [SerializeField] private UnityEvent explosionImpactReceived;
        [SerializeField] private UnityEvent contactImpactEnabled;

        private bool canReceiveContactImpact;
        private bool hasReceivedExplosionImpact;

        public bool HasReceivedExplosionImpact => hasReceivedExplosionImpact;
        public bool CanReceiveContactImpact => receiveContactImpact && canReceiveContactImpact;

        private void Reset()
        {
            targetRigidbody = GetComponentInParent<Rigidbody>();
        }

        private void Awake()
        {
            if (targetRigidbody == null)
                targetRigidbody = GetComponentInParent<Rigidbody>();

            canReceiveContactImpact = contactImpactEnabledOnStart;
        }

        private void OnValidate()
        {
            explosionForceMultiplier = Mathf.Max(0.0f, explosionForceMultiplier);
            minimumExplosionForce = Mathf.Max(0.0f, minimumExplosionForce);
            contactForceMultiplier = Mathf.Max(0.0f, contactForceMultiplier);
            minimumContactForce = Mathf.Max(0.0f, minimumContactForce);
        }

        public bool TryApplyImpact(in EntityImpactData impactData)
        {
            switch (impactData.Kind)
            {
                case EntityImpactKind.Explosion:
                    return TryApplyExplosionImpact(impactData);

                case EntityImpactKind.Contact:
                    return TryApplyContactImpact(impactData);

                default:
                    return false;
            }
        }

        public void EnableContactImpact()
        {
            SetContactImpactEnabled(true);
        }

        public void DisableContactImpact()
        {
            SetContactImpactEnabled(false);
        }

        public void SetContactImpactEnabled(bool isEnabled)
        {
            if (canReceiveContactImpact == isEnabled)
                return;

            canReceiveContactImpact = isEnabled;

            if (canReceiveContactImpact)
                contactImpactEnabled?.Invoke();
        }

        public void MakeTargetRigidbodyDynamic()
        {
            MakeRigidbodyDynamic();
        }

        private bool TryApplyExplosionImpact(in EntityImpactData impactData)
        {
            if (!receiveExplosionImpact || impactData.ForceMagnitude < minimumExplosionForce)
                return false;

            hasReceivedExplosionImpact = true;

            if (makeRigidbodyDynamicAfterExplosion)
                MakeRigidbodyDynamic();

            if (enableContactImpactAfterExplosion)
                SetContactImpactEnabled(true);

            float effectiveForce = impactData.ForceMagnitude * explosionForceMultiplier;
            bool appliedImpulse = ApplyImpulse(impactData.Direction, impactData.Point, effectiveForce);

            explosionImpactReceived?.Invoke();
            return appliedImpulse || explosionForceMultiplier > 0.0f;
        }

        private bool TryApplyContactImpact(in EntityImpactData impactData)
        {
            if (!CanReceiveContactImpact || impactData.ForceMagnitude < minimumContactForce)
                return false;

            float effectiveForce = impactData.ForceMagnitude * contactForceMultiplier;
            return ApplyImpulse(impactData.Direction, impactData.Point, effectiveForce);
        }

        private void MakeRigidbodyDynamic()
        {
            if (targetRigidbody == null)
                return;

            targetRigidbody.isKinematic = false;
            targetRigidbody.detectCollisions = true;
            targetRigidbody.useGravity = useGravityAfterExplosion;
        }

        private bool ApplyImpulse(Vector3 direction, Vector3 point, float forceMagnitude)
        {
            if (targetRigidbody == null || targetRigidbody.isKinematic || forceMagnitude <= 0.0f)
                return false;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = transform.position - point;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = transform.forward;

            Vector3 impulse = direction.normalized * forceMagnitude;

            if (point == Vector3.zero)
                targetRigidbody.AddForce(impulse, ForceMode.Impulse);
            else
                targetRigidbody.AddForceAtPosition(impulse, point, ForceMode.Impulse);

            return true;
        }
    }
}