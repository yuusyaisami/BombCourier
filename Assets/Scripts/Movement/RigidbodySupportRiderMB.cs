using UnityEngine;

namespace BC.Base
{
    [DefaultExecutionOrder(95)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodySupportRiderMB : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody targetRigidbody;

        [Header("Support Contact")]
        [SerializeField, Range(0f, 89f)] private float maxSupportAngle = 55f;
        [SerializeField] private bool ignoreWhenKinematic = true;
        [SerializeField] private bool ignoreWhenParented = true;

        [Header("Carry")]
        [SerializeField, Range(0f, 1f)] private float carryPositionRate = 1f;
        [SerializeField, Range(0f, 1f)] private float carryVelocityRate = 1f;
        [SerializeField, Min(0f)] private float maxCarryCorrectionSpeed = 30f;
        [SerializeField, Min(0f)] private float maxSupportVelocity = 30f;

        private SupportContactCandidate pendingSupport;
        private Vector3 appliedSupportVelocity;
        private bool hasPendingSupport;
        private bool hasAppliedSupport;

        private void Reset()
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ClearSupportState();
        }

        private void OnDisable()
        {
            ClearSupportState();
        }

        private void FixedUpdate()
        {
            ResolveReferences();

            if (!CanApplySupport())
            {
                ClearSupportState();
                return;
            }

            float dt = Time.fixedDeltaTime;
            if (dt <= 0f)
            {
                hasPendingSupport = false;
                return;
            }

            if (!hasPendingSupport || pendingSupport.Collider == null)
            {
                hasAppliedSupport = false;
                appliedSupportVelocity = Vector3.zero;
                return;
            }

            ApplySupportMotion(dt);
            hasPendingSupport = false;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!CanApplySupport() || collision == null)
                return;

            float minSupportDot = Mathf.Cos(maxSupportAngle * Mathf.Deg2Rad);

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                Collider supportCollider = contact.otherCollider != null && contact.otherCollider.transform.IsChildOf(transform)
                    ? contact.thisCollider
                    : contact.otherCollider;

                if (!CanUseSupportCollider(supportCollider))
                    continue;

                float upDot = Vector3.Dot(contact.normal, Vector3.up);
                if (upDot < minSupportDot)
                    continue;

                if (hasPendingSupport && upDot <= pendingSupport.UpDot)
                    continue;

                pendingSupport = new SupportContactCandidate(supportCollider, contact.point, upDot);
                hasPendingSupport = true;
            }
        }

        private void ApplySupportMotion(float dt)
        {
            Vector3 passengerPoint = pendingSupport.Point;

            if (!SupportMotionUtility.TryGetSupportMotion(
                    pendingSupport.Collider,
                    passengerPoint,
                    dt,
                    transform,
                    out SupportMotionSnapshot supportMotion))
            {
                hasAppliedSupport = false;
                appliedSupportVelocity = Vector3.zero;
                return;
            }

            Vector3 supportVelocity = ClampMagnitude(supportMotion.PassengerVelocity, maxSupportVelocity);

            if (carryPositionRate > 0f && supportMotion.PassengerDelta.sqrMagnitude > 0.0000001f)
            {
                Vector3 positionDelta = supportMotion.PassengerDelta * carryPositionRate;
                float maxDelta = maxCarryCorrectionSpeed > 0f ? maxCarryCorrectionSpeed * dt : positionDelta.magnitude;

                if (maxDelta > 0f)
                    positionDelta = Vector3.ClampMagnitude(positionDelta, maxDelta);

                targetRigidbody.position += positionDelta;
            }

            if (carryVelocityRate > 0f)
            {
                Vector3 previousSupportVelocity = hasAppliedSupport ? appliedSupportVelocity : Vector3.zero;
                Vector3 velocityDelta = (supportVelocity - previousSupportVelocity) * carryVelocityRate;
                targetRigidbody.linearVelocity += velocityDelta;
                appliedSupportVelocity = supportVelocity;
                hasAppliedSupport = true;
            }
        }

        private bool CanApplySupport()
        {
            if (targetRigidbody == null || !isActiveAndEnabled)
                return false;

            if (ignoreWhenKinematic && targetRigidbody.isKinematic)
                return false;

            if (ignoreWhenParented && transform.parent != null)
                return false;

            return true;
        }

        private bool CanUseSupportCollider(Collider supportCollider)
        {
            if (supportCollider == null || !supportCollider.enabled)
                return false;

            if (supportCollider.attachedRigidbody == targetRigidbody)
                return false;

            if (supportCollider.transform == transform || supportCollider.transform.IsChildOf(transform))
                return false;

            return true;
        }

        private void ResolveReferences()
        {
            if (targetRigidbody == null)
                targetRigidbody = GetComponent<Rigidbody>();
        }

        private void ClearSupportState()
        {
            pendingSupport = default;
            appliedSupportVelocity = Vector3.zero;
            hasPendingSupport = false;
            hasAppliedSupport = false;
        }

        private static Vector3 ClampMagnitude(Vector3 value, float maxMagnitude)
        {
            if (maxMagnitude <= 0f)
                return value;

            return value.sqrMagnitude > maxMagnitude * maxMagnitude
                ? value.normalized * maxMagnitude
                : value;
        }

        private readonly struct SupportContactCandidate
        {
            public readonly Collider Collider;
            public readonly Vector3 Point;
            public readonly float UpDot;

            public SupportContactCandidate(Collider collider, Vector3 point, float upDot)
            {
                Collider = collider;
                Point = point;
                UpDot = upDot;
            }
        }
    }
}