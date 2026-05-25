using UnityEngine;

namespace BC.Base
{
    public static class GroundProbeSolver
    {
        public static GroundHitInfo Probe(
            Transform ownerTransform,
            CapsuleCollider bodyCollider,
            LayerMask groundMask,
            float groundProbeExtraDistance,
            float groundProbeRadiusShrink,
            float maxGroundAngle,
            RaycastHit[] groundHits)
        {
            if (ownerTransform == null || bodyCollider == null || groundHits == null || groundHits.Length == 0)
                return default;

            GetGroundProbeParameters(
                ownerTransform,
                bodyCollider,
                groundProbeExtraDistance,
                groundProbeRadiusShrink,
                out Vector3 center,
                out float radius,
                out float distance);

            int hitCount = Physics.SphereCastNonAlloc(
                center,
                radius,
                Vector3.down,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            GroundHitInfo bestHit = default;
            float minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null || hit.collider.transform.IsChildOf(ownerTransform))
                    continue;

                float angle = Vector3.Angle(hit.normal, Vector3.up);
                float upDot = Vector3.Dot(hit.normal, Vector3.up);
                GroundSurfaceKind surfaceKind = ClassifySurfaceKind(upDot, minGroundDot);
                bool isWalkable = surfaceKind == GroundSurfaceKind.Walkable;

                if (!isWalkable || hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestHit = new GroundHitInfo(
                    true,
                    hit.collider,
                    hit.collider.transform,
                    hit.point,
                    hit.normal,
                    hit.distance,
                    angle,
                    surfaceKind,
                    true);
            }

            return bestHit;
        }

        private static GroundSurfaceKind ClassifySurfaceKind(float upDot, float minGroundDot)
        {
            if (upDot >= minGroundDot)
                return GroundSurfaceKind.Walkable;

            if (upDot > 0.01f)
                return GroundSurfaceKind.SteepSlope;

            if (upDot <= -0.35f)
                return GroundSurfaceKind.Ceiling;

            return GroundSurfaceKind.Wall;
        }

        public static void GetGroundProbeParameters(
            Transform ownerTransform,
            CapsuleCollider bodyCollider,
            float groundProbeExtraDistance,
            float groundProbeRadiusShrink,
            out Vector3 center,
            out float radius,
            out float distance)
        {
            Vector3 lossyScale = ownerTransform.lossyScale;
            float scaleX = Mathf.Abs(lossyScale.x);
            float scaleY = Mathf.Abs(lossyScale.y);
            float scaleZ = Mathf.Abs(lossyScale.z);

            float colliderRadius = bodyCollider.radius * Mathf.Max(scaleX, scaleZ);
            float colliderHalfHeight = bodyCollider.height * 0.5f * scaleY;

            center = ownerTransform.TransformPoint(bodyCollider.center);
            radius = Mathf.Max(0.01f, colliderRadius - groundProbeRadiusShrink);
            distance = Mathf.Max(0.01f, colliderHalfHeight - colliderRadius + groundProbeExtraDistance);
        }
    }
}
