using BC.Gimmick.Cushion;
using UnityEngine;

namespace BC.Base
{
    public sealed class CushionHighJumpBuffer
    {
        private float pendingExpireTime = -999.0f;
        private Vector3 pendingDirection;
        private float pendingSpeed;
        private float pendingSpeedLimit;
        private float pendingSpeedMultiplier = 1.0f;

        public void Clear()
        {
            pendingExpireTime = -999.0f;
            pendingDirection = Vector3.zero;
            pendingSpeed = 0.0f;
            pendingSpeedLimit = 0.0f;
            pendingSpeedMultiplier = 1.0f;
        }

        public void Arm(in CushionImpactResult impactResult, float currentTime, float coyoteTime)
        {
            if (impactResult.ResponseKind != CushionResponseKind.Bounce ||
                impactResult.HighJumpSpeedMultiplier <= 1.0001f ||
                impactResult.BounceVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float bounceSpeed = impactResult.BounceVelocity.magnitude;
            pendingExpireTime = currentTime + coyoteTime;
            pendingDirection = impactResult.BounceVelocity / bounceSpeed;
            pendingSpeed = bounceSpeed;
            pendingSpeedLimit = impactResult.BounceSpeedLimit;
            pendingSpeedMultiplier = impactResult.HighJumpSpeedMultiplier;
        }

        public bool TryGetPending(float currentTime, bool hasBufferedHighJumpInput, out Vector3 normalBounceVelocity, out float bounceSpeedLimit, out float highJumpSpeedMultiplier)
        {
            normalBounceVelocity = Vector3.zero;
            bounceSpeedLimit = 0.0f;
            highJumpSpeedMultiplier = 1.0f;

            if (currentTime > pendingExpireTime)
            {
                Clear();
                return false;
            }

            if (!hasBufferedHighJumpInput)
                return false;

            normalBounceVelocity = pendingDirection * pendingSpeed;
            bounceSpeedLimit = pendingSpeedLimit;
            highJumpSpeedMultiplier = pendingSpeedMultiplier;
            return true;
        }
    }
}
