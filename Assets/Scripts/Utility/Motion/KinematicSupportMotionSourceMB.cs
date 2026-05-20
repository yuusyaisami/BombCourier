using UnityEngine;

namespace BC.Base
{
    [DefaultExecutionOrder(-80)]
    [DisallowMultipleComponent]
    public sealed class KinematicSupportMotionSourceMB : MonoBehaviour, ISupportMotionSource
    {
        [Header("Source")]
        [SerializeField] private Transform sourceTransform;
        [SerializeField] private Rigidbody sourceRigidbody;

        private Vector3 lastPosition;
        private Quaternion lastRotation = Quaternion.identity;
        private Vector3 positionDelta;
        private Quaternion rotationDelta = Quaternion.identity;
        private bool hasLastPose;

        private void Reset()
        {
            sourceTransform = transform;
            sourceRigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureCurrentPose();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureCurrentPose();
        }

        private void FixedUpdate()
        {
            ResolveReferences();

            Transform resolvedTransform = ResolveTransform();
            if (resolvedTransform == null)
                return;

            if (!hasLastPose)
            {
                CaptureCurrentPose();
                return;
            }

            positionDelta = resolvedTransform.position - lastPosition;
            rotationDelta = resolvedTransform.rotation * Quaternion.Inverse(lastRotation);
            lastPosition = resolvedTransform.position;
            lastRotation = resolvedTransform.rotation;
        }

        public bool TryGetSupportMotion(Vector3 passengerWorldPosition, float deltaTime, out SupportMotionSnapshot motion)
        {
            motion = SupportMotionSnapshot.None;

            if (deltaTime <= 0f)
                return false;

            ResolveReferences();

            if (sourceRigidbody != null && !sourceRigidbody.isKinematic)
            {
                motion = SupportMotionUtility.FromRigidbody(sourceRigidbody, passengerWorldPosition, deltaTime);
                return motion.IsValid;
            }

            Transform resolvedTransform = ResolveTransform();
            if (resolvedTransform == null || !hasLastPose)
                return false;

            if (positionDelta.sqrMagnitude <= 0.0000001f && rotationDelta == Quaternion.identity)
                return false;

            motion = SupportMotionUtility.FromDelta(
                resolvedTransform,
                sourceRigidbody,
                resolvedTransform.position,
                passengerWorldPosition,
                positionDelta,
                rotationDelta,
                deltaTime);
            return motion.IsValid;
        }

        private void ResolveReferences()
        {
            if (sourceTransform == null)
                sourceTransform = transform;

            if (sourceRigidbody == null)
                sourceRigidbody = GetComponent<Rigidbody>();
        }

        private Transform ResolveTransform()
        {
            if (sourceRigidbody != null)
                return sourceRigidbody.transform;

            return sourceTransform != null ? sourceTransform : transform;
        }

        private void CaptureCurrentPose()
        {
            Transform resolvedTransform = ResolveTransform();
            if (resolvedTransform == null)
                return;

            lastPosition = resolvedTransform.position;
            lastRotation = resolvedTransform.rotation;
            positionDelta = Vector3.zero;
            rotationDelta = Quaternion.identity;
            hasLastPose = true;
        }
    }
}