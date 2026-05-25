using BC.Gimmick.Cushion;
using UnityEngine;

namespace BC.Base
{
    public readonly struct CushionApplyResult
    {
        public CushionApplyResult(bool handled, bool shouldConsumeHighJumpInput, bool shouldRaiseHighJumpEvent, Vector3 highJumpEventVelocity)
        {
            Handled = handled;
            ShouldConsumeHighJumpInput = shouldConsumeHighJumpInput;
            ShouldRaiseHighJumpEvent = shouldRaiseHighJumpEvent;
            HighJumpEventVelocity = highJumpEventVelocity;
        }

        public bool Handled { get; }
        public bool ShouldConsumeHighJumpInput { get; }
        public bool ShouldRaiseHighJumpEvent { get; }
        public Vector3 HighJumpEventVelocity { get; }
    }

    public static class CushionImpactHandler
    {
        public static bool TryProcessContact(
            in MoveContactInfo contact,
            float currentTime,
            float impactCooldown,
            ref float nextImpactTime,
            GameObject sourceObject,
            Transform sourceTransform,
            EntityMB sourceEntity,
            EntityTagId sourceTag,
            Rigidbody sourceBody,
            CapsuleCollider sourceCollider,
            Vector3 incomingVelocity,
            EntityMoveRuntimeState runtimeState,
            bool canApplySystemMovement,
            bool hasBufferedHighJumpInput,
            CushionHighJumpBuffer highJumpBuffer,
            float groundedStickVelocity,
            float coyoteTime,
            out CushionApplyResult applyResult)
        {
            applyResult = default;

            if (currentTime < nextImpactTime)
                return false;

            if (contact.Collider == null)
                return false;

            CushionSurfaceMB surface = contact.Collider.GetComponentInParent<CushionSurfaceMB>();
            if (surface == null)
                return false;

            CushionImpactData impactData = new CushionImpactData(
                sourceObject,
                sourceTransform,
                sourceEntity,
                sourceTag,
                sourceBody,
                sourceCollider,
                contact.Point,
                contact.Normal,
                incomingVelocity,
                contact.RelativeSpeed);

            if (!surface.TryEvaluate(impactData, out CushionImpactResult impactResult))
                return false;

            applyResult = ApplyImpact(
                runtimeState,
                impactResult,
                canApplySystemMovement,
                hasBufferedHighJumpInput,
                highJumpBuffer,
                groundedStickVelocity,
                currentTime,
                coyoteTime);

            if (!applyResult.Handled)
                return false;

            nextImpactTime = currentTime + impactCooldown;
            return true;
        }

        public static CushionApplyResult ApplyImpact(
            EntityMoveRuntimeState runtimeState,
            in CushionImpactResult impactResult,
            bool canApplySystemMovement,
            bool hasBufferedHighJumpInput,
            CushionHighJumpBuffer highJumpBuffer,
            float groundedStickVelocity,
            float currentTime,
            float coyoteTime)
        {
            if (runtimeState == null || !impactResult.IsHandled || runtimeState.IsDead || !canApplySystemMovement)
                return default;

            switch (impactResult.ResponseKind)
            {
                case CushionResponseKind.Bounce:
                    return ApplyBounce(runtimeState, impactResult, hasBufferedHighJumpInput, highJumpBuffer, groundedStickVelocity, currentTime, coyoteTime);

                case CushionResponseKind.Stop:
                case CushionResponseKind.StopAndAttach:
                case CushionResponseKind.Dampen:
                    ApplyStop(runtimeState, groundedStickVelocity);
                    return new CushionApplyResult(true, false, false, Vector3.zero);

                default:
                    return default;
            }
        }

        public static CushionApplyResult TryConsumeBufferedHighJump(
            EntityMoveRuntimeState runtimeState,
            CushionHighJumpBuffer highJumpBuffer,
            bool hasBufferedHighJumpInput,
            float groundedStickVelocity,
            float currentTime)
        {
            if (runtimeState == null || highJumpBuffer == null)
                return default;

            if (!highJumpBuffer.TryGetPending(
                    currentTime,
                    hasBufferedHighJumpInput,
                    out Vector3 normalBounceVelocity,
                    out float bounceSpeedLimit,
                    out float highJumpSpeedMultiplier))
            {
                return default;
            }

            if (!TryBuildHighJumpBounceVelocity(normalBounceVelocity, bounceSpeedLimit, highJumpSpeedMultiplier, hasBufferedHighJumpInput, out Vector3 highJumpBounceVelocity))
                return default;

            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = Vector3.zero;
            channels.Vertical = groundedStickVelocity;
            channels.External = highJumpBounceVelocity;
            channels.InheritedSupport = Vector3.zero;
            runtimeState.Velocity = channels;

            highJumpBuffer.Clear();
            return new CushionApplyResult(true, true, true, highJumpBounceVelocity);
        }

        public static bool TryBuildHighJumpBounceVelocity(
            Vector3 normalBounceVelocity,
            float bounceSpeedLimit,
            float highJumpSpeedMultiplier,
            bool hasBufferedHighJumpInput,
            out Vector3 highJumpBounceVelocity)
        {
            highJumpBounceVelocity = normalBounceVelocity;

            if (highJumpSpeedMultiplier <= 1.0001f || !hasBufferedHighJumpInput)
                return false;

            float normalBounceSpeed = normalBounceVelocity.magnitude;
            if (normalBounceSpeed <= 0.0001f)
                return false;

            Vector3 bounceDirection = normalBounceVelocity / normalBounceSpeed;
            float cappedBoostedSpeed = normalBounceSpeed * highJumpSpeedMultiplier;
            if (bounceSpeedLimit > 0.0f)
                cappedBoostedSpeed = Mathf.Min(cappedBoostedSpeed, bounceSpeedLimit * highJumpSpeedMultiplier);

            if (cappedBoostedSpeed <= normalBounceSpeed + 0.0001f)
                return false;

            highJumpBounceVelocity = bounceDirection * cappedBoostedSpeed;
            return true;
        }

        private static CushionApplyResult ApplyBounce(
            EntityMoveRuntimeState runtimeState,
            in CushionImpactResult impactResult,
            bool hasBufferedHighJumpInput,
            CushionHighJumpBuffer highJumpBuffer,
            float groundedStickVelocity,
            float currentTime,
            float coyoteTime)
        {
            Vector3 bounceVelocity = impactResult.BounceVelocity;
            bool consumedHighJumpInput = false;
            bool raiseHighJumpEvent = false;

            if (TryBuildHighJumpBounceVelocity(
                    impactResult.BounceVelocity,
                    impactResult.BounceSpeedLimit,
                    impactResult.HighJumpSpeedMultiplier,
                    hasBufferedHighJumpInput,
                    out Vector3 highJumpBounceVelocity))
            {
                bounceVelocity = highJumpBounceVelocity;
                consumedHighJumpInput = true;
                raiseHighJumpEvent = true;
                highJumpBuffer?.Clear();
            }
            else
            {
                highJumpBuffer?.Arm(impactResult, currentTime, coyoteTime);
            }

            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = Vector3.zero;
            channels.Vertical = groundedStickVelocity;
            channels.External = bounceVelocity;
            channels.InheritedSupport = Vector3.zero;
            runtimeState.Velocity = channels;

            return new CushionApplyResult(true, consumedHighJumpInput, raiseHighJumpEvent, bounceVelocity);
        }

        private static void ApplyStop(EntityMoveRuntimeState runtimeState, float groundedStickVelocity)
        {
            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = Vector3.zero;
            channels.External = Vector3.zero;
            channels.InheritedSupport = Vector3.zero;
            channels.Vertical = groundedStickVelocity;
            runtimeState.Velocity = channels;
        }
    }
}
