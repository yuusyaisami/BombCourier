using UnityEngine;

namespace BC.Base
{
    public sealed class EntityMoveRuntimeState
    {
        public EntityMoveState MoveState;

        public bool IsGrounded;
        public bool WasGrounded;
        public float LastGroundedTime = -999.0f;
        public float LastJumpTime = -999.0f;
        public float LastSupportLaunchTime = -999.0f;

        public Vector3 PlanarVelocity;
        public float VerticalVelocity;
        public Vector3 ExternalVelocity;
        public Vector3 InheritedSupportVelocity;
        public Vector3 PlatformVelocity;
        public VelocityChannels Velocity;

        public GroundHitInfo Ground;
        public SupportMotionState Support;

        public bool MotionLocked;
        public bool IsDead;
        public float JumpBufferCounter;
        public float GroundSnapDisabledUntilTime = -999.0f;
        public float SupportReattachDisabledUntilTime = -999.0f;
    }
}