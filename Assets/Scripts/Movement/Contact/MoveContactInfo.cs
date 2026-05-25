using UnityEngine;

namespace BC.Base
{
    public struct MoveContactInfo
    {
        public Collider Collider;
        public Rigidbody AttachedRigidbody;
        public Transform Transform;

        public Vector3 Point;
        public Vector3 Normal;
        public float UpDot;
        public float Angle;

        public Vector3 RelativeVelocity;
        public float RelativeSpeed;

        public MoveContactKind Kind;
    }
}
