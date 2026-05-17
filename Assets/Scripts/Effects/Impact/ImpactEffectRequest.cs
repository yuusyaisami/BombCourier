using UnityEngine;

namespace BC.Effects.Impact
{
    public readonly struct ImpactEffectRequest
    {
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly Collider SurfaceCollider;
        public readonly float ImpactStrength;
        public readonly float ReferenceStrength;
        public readonly Color DefaultColor;

        public ImpactEffectRequest(
            Vector3 point,
            Vector3 normal,
            Collider surfaceCollider,
            float impactStrength,
            float referenceStrength,
            Color defaultColor)
        {
            Point = point;
            Normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            SurfaceCollider = surfaceCollider;
            ImpactStrength = Mathf.Max(0.0f, impactStrength);
            ReferenceStrength = Mathf.Max(0.01f, referenceStrength);
            DefaultColor = defaultColor;
        }

        public float NormalizedStrength => Mathf.Clamp01(ImpactStrength / ReferenceStrength);
    }
}