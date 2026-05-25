using UnityEngine;

namespace BC.Base
{
    public readonly struct GroundHitInfo
    {
        public GroundHitInfo(
            bool isValid,
            Collider collider,
            Transform transform,
            Vector3 point,
            Vector3 normal,
            float distance,
            float angle,
            GroundSurfaceKind surfaceKind,
            bool isWalkable)
        {
            IsValid = isValid;
            Collider = collider;
            Transform = transform;
            Point = point;
            Normal = normal;
            Distance = distance;
            Angle = angle;
            SurfaceKind = surfaceKind;
            IsWalkable = isWalkable;
        }

        public bool IsValid { get; }
        public Collider Collider { get; }
        public Transform Transform { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
        public float Angle { get; }
        public GroundSurfaceKind SurfaceKind { get; }
        public bool IsWalkable { get; }
    }
}
