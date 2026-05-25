using UnityEngine;

namespace BC.Base
{
    public static class GroundSnapSolver
    {
        public static PositionCorrection Resolve(
            GroundSnapSettings settings,
            EntityMoveRuntimeState runtimeState,
            GroundHitInfo groundHit,
            bool isGrounded,
            float verticalVelocity,
            float currentTime,
            float dt)
        {
            if (settings == null || runtimeState == null || !settings.Enabled)
                return PositionCorrection.None;

            if (!isGrounded && !runtimeState.WasGrounded)
                return PositionCorrection.None;

            if (currentTime < runtimeState.GroundSnapDisabledUntilTime)
                return PositionCorrection.None;

            if (currentTime - runtimeState.LastJumpTime <= settings.DisableAfterJumpTime)
                return PositionCorrection.None;

            if (currentTime - runtimeState.LastSupportLaunchTime <= settings.DisableAfterSupportLaunchTime)
                return PositionCorrection.None;

            if (verticalVelocity > 0.1f)
                return PositionCorrection.None;

            if (!groundHit.IsValid || !groundHit.IsWalkable)
                return PositionCorrection.None;

            if (groundHit.Distance <= 0.0001f || groundHit.Distance > settings.MaxSnapDistance)
                return PositionCorrection.None;

            float speedCapDelta = Mathf.Max(0.0f, settings.SnapSpeed) * Mathf.Max(0.0001f, dt);
            float distanceCapDelta = Mathf.Max(0.0f, settings.MaxSnapDistancePerTick);
            float maxDelta = Mathf.Min(speedCapDelta, distanceCapDelta);

            if (maxDelta <= 0.0001f)
                return PositionCorrection.None;

            float deltaY = Mathf.Min(groundHit.Distance, maxDelta);
            if (deltaY <= 0.0001f)
                return PositionCorrection.None;

            return new PositionCorrection(Vector3.down * deltaY);
        }
    }
}
