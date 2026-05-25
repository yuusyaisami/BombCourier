using UnityEngine;

namespace BC.Base
{
    public static class ContactClassifier
    {
        private const float FootEdgeHorizontalNormalThreshold = 0.45f;
        private const float BodyWallHorizontalNormalThreshold = 0.35f;
        private const float CeilingDownDotThreshold = 0.35f;
        private const float DefaultCeilingBand = 0.08f;

        public static void Classify(
            MoveContactBuffer contactBuffer,
            in MovementBodyGeometry geometry,
            in GroundHitInfo groundHit,
            float maxGroundAngle,
            float ceilingBand = DefaultCeilingBand)
        {
            if (contactBuffer == null)
                return;

            for (int i = 0; i < contactBuffer.Count; i++)
            {
                MoveContactInfo info = contactBuffer.Get(i);
                info.Kind = ClassifySingle(in info, in geometry, in groundHit, maxGroundAngle, ceilingBand);
                contactBuffer.Set(i, in info);
            }
        }

        private static MoveContactKind ClassifySingle(
            in MoveContactInfo info,
            in MovementBodyGeometry geometry,
            in GroundHitInfo groundHit,
            float maxGroundAngle,
            float ceilingBand)
        {
            float horizontalNormal = Vector3.ProjectOnPlane(info.Normal, Vector3.up).magnitude;
            bool isFootBand = info.Point.y <= geometry.FootBandTopY;
            bool isHeadBand = info.Point.y >= geometry.HeadY - Mathf.Max(0.0f, ceilingBand);
            bool isUpFacing = info.UpDot > 0.0f;
            bool isWalkableAngle = info.Angle <= maxGroundAngle;
            bool isGroundConsistent = !groundHit.IsValid || Vector3.Dot(info.Normal, groundHit.Normal) >= 0.4f;

            if (isHeadBand && info.UpDot <= -CeilingDownDotThreshold)
                return MoveContactKind.Ceiling;

            if (isFootBand && isUpFacing && isWalkableAngle && isGroundConsistent)
            {
                if (horizontalNormal >= FootEdgeHorizontalNormalThreshold)
                    return MoveContactKind.FootEdge;

                return MoveContactKind.FootGround;
            }

            if (isUpFacing && info.Angle > maxGroundAngle)
                return MoveContactKind.SteepSlope;

            if (!isFootBand && horizontalNormal >= BodyWallHorizontalNormalThreshold)
                return MoveContactKind.BodyWall;

            return MoveContactKind.PushTarget;
        }
    }
}
