using UnityEngine;

namespace BC.Base
{
    public struct SupportMotionState
    {
        public bool HasSupport;
        public bool HadSupport;
        public Collider Collider;
        public Transform Transform;

        public Vector3 PassengerDelta;
        public Vector3 PassengerVelocity;
        public Vector3 PreviousPassengerVelocity;
        public Vector3 PassengerAcceleration;

        public Vector3 SupportPoint;

        public Transform SupportTransform;
        public Vector3 PlatformDelta;
        public Vector3 PlatformVelocity;
        public bool HasPlatformPose;
        public Vector3 LastPlatformPosition;
        public Quaternion LastPlatformRotation;
    }
}
