using UnityEngine;

namespace BC.Base
{
    public readonly struct SupportMotionSnapshot
    {
        public readonly bool IsValid;
        public readonly Transform SourceTransform;
        public readonly Rigidbody SourceRigidbody;
        public readonly Vector3 SourceOrigin;
        public readonly Vector3 PassengerPoint;
        public readonly Vector3 SourcePositionDelta;
        public readonly Quaternion SourceRotationDelta;
        public readonly Vector3 PassengerDelta;
        public readonly Vector3 SourceLinearVelocity;
        public readonly Vector3 SourceAngularVelocity;
        public readonly Vector3 PassengerVelocity;

        public SupportMotionSnapshot(
            bool isValid,
            Transform sourceTransform,
            Rigidbody sourceRigidbody,
            Vector3 sourceOrigin,
            Vector3 passengerPoint,
            Vector3 sourcePositionDelta,
            Quaternion sourceRotationDelta,
            Vector3 passengerDelta,
            Vector3 sourceLinearVelocity,
            Vector3 sourceAngularVelocity,
            Vector3 passengerVelocity)
        {
            IsValid = isValid;
            SourceTransform = sourceTransform;
            SourceRigidbody = sourceRigidbody;
            SourceOrigin = sourceOrigin;
            PassengerPoint = passengerPoint;
            SourcePositionDelta = sourcePositionDelta;
            SourceRotationDelta = sourceRotationDelta;
            PassengerDelta = passengerDelta;
            SourceLinearVelocity = sourceLinearVelocity;
            SourceAngularVelocity = sourceAngularVelocity;
            PassengerVelocity = passengerVelocity;
        }

        public static SupportMotionSnapshot None => default;
    }

    public interface ISupportMotionSource
    {
        // 支持接触点の移動量と速度を返す。回転足場では中心速度ではなく接触点速度を使う。
        bool TryGetSupportMotion(Vector3 passengerWorldPosition, float deltaTime, out SupportMotionSnapshot motion);
    }

    public static class SupportMotionUtility
    {
        public static SupportMotionSnapshot FromDelta(
            Transform sourceTransform,
            Rigidbody sourceRigidbody,
            Vector3 sourceOrigin,
            Vector3 passengerWorldPosition,
            Vector3 sourcePositionDelta,
            Quaternion sourceRotationDelta,
            float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0.0001f, deltaTime);
            Vector3 offset = passengerWorldPosition - sourceOrigin;
            Vector3 passengerDelta = sourcePositionDelta + sourceRotationDelta * offset - offset;
            Vector3 sourceLinearVelocity = sourcePositionDelta / safeDeltaTime;
            Vector3 sourceAngularVelocity = CalculateAngularVelocity(sourceRotationDelta, safeDeltaTime);
            Vector3 passengerVelocity = passengerDelta / safeDeltaTime;

            return new SupportMotionSnapshot(
                true,
                sourceTransform,
                sourceRigidbody,
                sourceOrigin,
                passengerWorldPosition,
                sourcePositionDelta,
                sourceRotationDelta,
                passengerDelta,
                sourceLinearVelocity,
                sourceAngularVelocity,
                passengerVelocity);
        }

        public static SupportMotionSnapshot FromVelocity(
            Transform sourceTransform,
            Rigidbody sourceRigidbody,
            Vector3 sourceOrigin,
            Vector3 passengerWorldPosition,
            Vector3 sourceLinearVelocity,
            Vector3 sourceAngularVelocity,
            float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0.0001f, deltaTime);
            Vector3 passengerVelocity = CalculatePointVelocity(
                sourceOrigin,
                passengerWorldPosition,
                sourceLinearVelocity,
                sourceAngularVelocity);
            Vector3 sourcePositionDelta = sourceLinearVelocity * safeDeltaTime;
            Quaternion sourceRotationDelta = CalculateRotationDelta(sourceAngularVelocity, safeDeltaTime);
            Vector3 passengerDelta = passengerVelocity * safeDeltaTime;

            return new SupportMotionSnapshot(
                true,
                sourceTransform,
                sourceRigidbody,
                sourceOrigin,
                passengerWorldPosition,
                sourcePositionDelta,
                sourceRotationDelta,
                passengerDelta,
                sourceLinearVelocity,
                sourceAngularVelocity,
                passengerVelocity);
        }

        public static SupportMotionSnapshot FromRigidbody(Rigidbody sourceRigidbody, Vector3 passengerWorldPosition, float deltaTime)
        {
            if (sourceRigidbody == null)
                return SupportMotionSnapshot.None;

            Vector3 origin = sourceRigidbody.worldCenterOfMass;
            Vector3 pointVelocity = sourceRigidbody.GetPointVelocity(passengerWorldPosition);
            Vector3 linearVelocity = sourceRigidbody.linearVelocity;
            Vector3 angularVelocity = sourceRigidbody.angularVelocity;
            float safeDeltaTime = Mathf.Max(0.0001f, deltaTime);

            return new SupportMotionSnapshot(
                true,
                sourceRigidbody.transform,
                sourceRigidbody,
                origin,
                passengerWorldPosition,
                linearVelocity * safeDeltaTime,
                CalculateRotationDelta(angularVelocity, safeDeltaTime),
                pointVelocity * safeDeltaTime,
                linearVelocity,
                angularVelocity,
                pointVelocity);
        }

        public static bool TryGetSupportMotion(
            Collider supportCollider,
            Vector3 passengerWorldPosition,
            float deltaTime,
            Transform selfRoot,
            out SupportMotionSnapshot motion)
        {
            motion = SupportMotionSnapshot.None;

            if (supportCollider == null || deltaTime <= 0f)
                return false;

            Rigidbody attachedRigidbody = supportCollider.attachedRigidbody;
            Transform searchRoot = attachedRigidbody != null ? attachedRigidbody.transform : supportCollider.transform;
            ISupportMotionSource source = searchRoot.GetComponentInParent<ISupportMotionSource>();

            if (source != null && !IsSelfSupport(source, selfRoot) && source.TryGetSupportMotion(passengerWorldPosition, deltaTime, out motion))
                return motion.IsValid;

            if (attachedRigidbody != null && (selfRoot == null || !attachedRigidbody.transform.IsChildOf(selfRoot)))
            {
                motion = FromRigidbody(attachedRigidbody, passengerWorldPosition, deltaTime);
                return motion.IsValid;
            }

            return false;
        }

        public static Vector3 CalculatePointVelocity(
            Vector3 sourceOrigin,
            Vector3 point,
            Vector3 sourceLinearVelocity,
            Vector3 sourceAngularVelocity)
        {
            return sourceLinearVelocity + Vector3.Cross(sourceAngularVelocity, point - sourceOrigin);
        }

        public static Vector3 CalculateAngularVelocity(Quaternion rotationDelta, float deltaTime)
        {
            if (deltaTime <= 0f || rotationDelta == Quaternion.identity)
                return Vector3.zero;

            rotationDelta.ToAngleAxis(out float angleDegrees, out Vector3 axis);

            if (float.IsNaN(axis.x) || axis.sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            if (angleDegrees > 180f)
                angleDegrees -= 360f;

            return axis.normalized * (angleDegrees * Mathf.Deg2Rad / deltaTime);
        }

        public static Quaternion CalculateRotationDelta(Vector3 angularVelocity, float deltaTime)
        {
            if (deltaTime <= 0f || angularVelocity.sqrMagnitude <= 0.000001f)
                return Quaternion.identity;

            float angleRadians = angularVelocity.magnitude * deltaTime;
            return Quaternion.AngleAxis(angleRadians * Mathf.Rad2Deg, angularVelocity.normalized);
        }

        private static bool IsSelfSupport(ISupportMotionSource source, Transform selfRoot)
        {
            if (selfRoot == null || source is not Component component)
                return false;

            Transform sourceTransform = component.transform;
            return sourceTransform == selfRoot || sourceTransform.IsChildOf(selfRoot);
        }
    }

    public readonly struct MovingPlatformPassengerMotion
    {
        public readonly Vector3 Delta;
        public readonly Vector3 Velocity;

        public MovingPlatformPassengerMotion(Vector3 delta, Vector3 velocity)
        {
            Delta = delta;
            Velocity = velocity;
        }
    }

    public interface IMovingPlatformMotionSource
    {
        // CharacterControllerはRigidbodyの接触解決だけでは床移動を継承しないため、足場側が明示的に乗客用デルタを渡す。
        bool TryGetPassengerMotion(Vector3 passengerWorldPosition, float deltaTime, out MovingPlatformPassengerMotion motion);
    }
}