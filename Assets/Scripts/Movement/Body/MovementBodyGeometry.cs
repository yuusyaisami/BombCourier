using UnityEngine;

namespace BC.Base
{
    public readonly struct MovementBodyGeometry
    {
        public MovementBodyGeometry(
            Vector3 bodyPosition,
            Vector3 capsuleCenter,
            Vector3 capsuleBottomSphereCenter,
            Vector3 capsuleTopSphereCenter,
            float capsuleRadius,
            float feetY,
            float headY,
            float footBandTopY)
        {
            BodyPosition = bodyPosition;
            CapsuleCenter = capsuleCenter;
            CapsuleBottomSphereCenter = capsuleBottomSphereCenter;
            CapsuleTopSphereCenter = capsuleTopSphereCenter;
            CapsuleRadius = capsuleRadius;
            FeetY = feetY;
            HeadY = headY;
            FootBandTopY = footBandTopY;
        }

        public Vector3 BodyPosition { get; }
        public Vector3 CapsuleCenter { get; }
        public Vector3 CapsuleBottomSphereCenter { get; }
        public Vector3 CapsuleTopSphereCenter { get; }
        public float CapsuleRadius { get; }
        public float FeetY { get; }
        public float HeadY { get; }
        public float FootBandTopY { get; }
    }
}