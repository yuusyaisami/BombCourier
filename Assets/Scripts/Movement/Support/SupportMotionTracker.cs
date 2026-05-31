using UnityEngine;

namespace BC.Base
{
    public sealed class SupportMotionTracker
    {
        private const float MinSupportDeltaSqr = 0.00000025f;

        private Transform currentPlatform;
        private Vector3 lastPlatformPosition;
        private Quaternion lastPlatformRotation;
        private bool hasPlatformPose;

        private Transform supportSampleSource;
        private Vector3 supportSampleLocalPoint;
        private bool hasSupportSamplePoint;

        public Transform CurrentPlatform => currentPlatform;
        public Vector3 PlatformDelta { get; private set; }
        public bool HasPlatformPose => hasPlatformPose;
        public Vector3 LastPlatformPosition => lastPlatformPosition;
        public Quaternion LastPlatformRotation => lastPlatformRotation;

        // support追従の計算と、runtimeState.Support の更新を1箇所に集約する。
        public void Update(
            Transform actorTransform,
            Rigidbody actorBody,
            in GroundHitInfo ground,
            bool isGrounded,
            EntityMoveRuntimeState runtimeState,
            float dt,
            float currentTime,
            float supportReattachDisabledUntilTime,
            float platformSupportSampleSmoothing,
            float platformCarryRotationDeadZoneDegrees)
        {
            if (runtimeState == null)
                return;

            PlatformDelta = Vector3.zero;
            runtimeState.PlatformVelocity = Vector3.zero;

            bool isSupportReattachSuppressed = currentTime < supportReattachDisabledUntilTime;
            if (isSupportReattachSuppressed || !isGrounded || !ground.IsValid || ground.Transform == null)
            {
                currentPlatform = null;
                hasPlatformPose = false;
                supportSampleSource = null;
                hasSupportSamplePoint = false;
                SyncRuntimeSupportState(actorTransform, in ground, runtimeState, dt);
                return;
            }

            Transform platform = ground.Transform;
            Vector3 supportSamplePoint = ResolveSupportSamplePoint(actorTransform, actorBody, platform, dt, platformSupportSampleSmoothing);

            if (SupportMotionUtility.TryGetSupportMotion(
                    ground.Collider,
                    supportSamplePoint,
                    dt,
                    actorTransform,
                    out SupportMotionSnapshot supportMotion))
            {
                Transform supportSource = supportMotion.SourceTransform != null ? supportMotion.SourceTransform : platform;
                UpdateSupportSamplePoint(supportSource, supportSamplePoint, dt, platformSupportSampleSmoothing);

                Vector3 stabilizedPoint = hasSupportSamplePoint && supportSource != null
                    ? supportSource.TransformPoint(supportSampleLocalPoint)
                    : supportSamplePoint;

                Vector3 sourceOffset = stabilizedPoint - supportMotion.SourceOrigin;
                Vector3 stabilizedDelta = supportMotion.SourcePositionDelta +
                                         supportMotion.SourceRotationDelta * sourceOffset -
                                         sourceOffset;

                if (Quaternion.Angle(supportMotion.SourceRotationDelta, Quaternion.identity) <= platformCarryRotationDeadZoneDegrees)
                    stabilizedDelta = supportMotion.SourcePositionDelta;

                stabilizedDelta = SanitizeSupportDelta(stabilizedDelta);

                PlatformDelta = stabilizedDelta;
                if (dt > 0.0f)
                    runtimeState.PlatformVelocity = stabilizedDelta / dt;

                currentPlatform = supportSource;
                SyncRuntimeSupportState(actorTransform, in ground, runtimeState, dt);
                return;
            }

            if (currentPlatform == platform && hasPlatformPose)
            {
                Vector3 positionDelta = platform.position - lastPlatformPosition;
                Quaternion rotationDelta = platform.rotation * Quaternion.Inverse(lastPlatformRotation);

                Vector3 playerOffsetFromPlatform = actorTransform.position - platform.position;
                Vector3 rotatedOffset = rotationDelta * playerOffsetFromPlatform;
                Vector3 rotationMovementDelta = rotatedOffset - playerOffsetFromPlatform;

                PlatformDelta = SanitizeSupportDelta(positionDelta + rotationMovementDelta);

                if (dt > 0.0f)
                    runtimeState.PlatformVelocity = PlatformDelta / dt;
            }

            currentPlatform = platform;
            SyncRuntimeSupportState(actorTransform, in ground, runtimeState, dt);
        }

        public void StorePlatformPose()
        {
            if (currentPlatform == null)
            {
                hasPlatformPose = false;
                return;
            }

            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
            hasPlatformPose = true;
        }

        private Vector3 ResolveSupportSamplePoint(
            Transform actorTransform,
            Rigidbody actorBody,
            Transform platform,
            float dt,
            float platformSupportSampleSmoothing)
        {
            Vector3 desiredWorldPoint = actorBody != null
                ? actorBody.worldCenterOfMass
                : actorTransform.position;

            if (!float.IsFinite(desiredWorldPoint.x) ||
                !float.IsFinite(desiredWorldPoint.y) ||
                !float.IsFinite(desiredWorldPoint.z))
            {
                desiredWorldPoint = actorTransform.position;
            }

            if (platform == null)
                return desiredWorldPoint;

            UpdateSupportSamplePoint(platform, desiredWorldPoint, dt, platformSupportSampleSmoothing);
            return platform.TransformPoint(supportSampleLocalPoint);
        }

        private void UpdateSupportSamplePoint(Transform sourceTransform, Vector3 desiredWorldPoint, float dt, float platformSupportSampleSmoothing)
        {
            if (sourceTransform == null)
            {
                supportSampleSource = null;
                hasSupportSamplePoint = false;
                return;
            }

            Vector3 desiredLocalPoint = sourceTransform.InverseTransformPoint(desiredWorldPoint);

            if (!hasSupportSamplePoint || supportSampleSource != sourceTransform)
            {
                supportSampleSource = sourceTransform;
                supportSampleLocalPoint = desiredLocalPoint;
                hasSupportSamplePoint = true;
                return;
            }

            float smoothing = Mathf.Max(0.0f, platformSupportSampleSmoothing);
            if (smoothing <= 0.0f)
            {
                supportSampleLocalPoint = desiredLocalPoint;
                return;
            }

            float blend = 1.0f - Mathf.Exp(-smoothing * Mathf.Max(0.0001f, dt));
            supportSampleLocalPoint = Vector3.Lerp(supportSampleLocalPoint, desiredLocalPoint, Mathf.Clamp01(blend));
        }

        private void SyncRuntimeSupportState(Transform actorTransform, in GroundHitInfo ground, EntityMoveRuntimeState runtimeState, float dt)
        {
            SupportMotionState support = runtimeState.Support;
            support.HadSupport = support.HasSupport;
            support.HasSupport = currentPlatform != null && ground.IsValid;
            support.Collider = ground.Collider;
            support.Transform = currentPlatform;
            support.PreviousPassengerVelocity = support.PassengerVelocity;
            support.PassengerDelta = PlatformDelta;
            support.PassengerVelocity = runtimeState.PlatformVelocity;
            support.PassengerAcceleration = dt > 0.0f
                ? (support.PassengerVelocity - support.PreviousPassengerVelocity) / dt
                : Vector3.zero;
            support.SupportPoint = hasSupportSamplePoint && supportSampleSource != null
                ? supportSampleSource.TransformPoint(supportSampleLocalPoint)
                : actorTransform.position;

            support.SupportTransform = currentPlatform;
            support.PlatformDelta = PlatformDelta;
            support.PlatformVelocity = runtimeState.PlatformVelocity;
            support.HasPlatformPose = hasPlatformPose;
            support.LastPlatformPosition = lastPlatformPosition;
            support.LastPlatformRotation = lastPlatformRotation;

            runtimeState.Support = support;
        }

        private static Vector3 SanitizeSupportDelta(Vector3 delta)
        {
            if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z))
                return Vector3.zero;

            return delta.sqrMagnitude >= MinSupportDeltaSqr ? delta : Vector3.zero;
        }
    }
}
