using UnityEngine;

namespace BC.Player
{
    public static class InteractionScoringUtility
    {
        public static bool TryGetPlanarFacingScore(
            InteractionQuery query,
            Vector3 targetPosition,
            float maxDistance,
            float maxAngle,
            out float score)
        {
            score = float.MaxValue;

            float allowedDistance = Mathf.Min(Mathf.Max(0.05f, maxDistance), Mathf.Max(0.05f, query.MaxDistance));
            float allowedAngle = Mathf.Min(maxAngle, query.MaxAngle);

            Vector3 toTarget = targetPosition - query.FacingPosition;
            toTarget.y = 0f;

            float sqrDistance = toTarget.sqrMagnitude;
            if (sqrDistance <= 0.0001f || sqrDistance > allowedDistance * allowedDistance)
                return false;

            Vector3 planarFacingForward = query.PlanarFacingForward;
            if (planarFacingForward.sqrMagnitude <= 0.0001f)
                return false;

            Vector3 directionToTarget = toTarget.normalized;
            float angle = Vector3.Angle(planarFacingForward, directionToTarget);
            if (angle > allowedAngle)
                return false;

            score = sqrDistance + angle * 0.05f;
            return true;
        }
    }
}