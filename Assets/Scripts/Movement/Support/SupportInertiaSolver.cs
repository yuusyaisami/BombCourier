using UnityEngine;

namespace BC.Base
{
    public static class SupportInertiaSolver
    {
        public static bool TryResolveLaunch(
            SupportInertiaSettings settings,
            EntityMoveRuntimeState runtimeState,
            bool hasConflictWithJumpOrCushion,
            float currentTime,
            out float launchVelocityY)
        {
            launchVelocityY = 0.0f;

            if (settings == null || runtimeState == null || !settings.Enabled)
                return false;

            if (runtimeState.IsDead || runtimeState.MotionLocked || hasConflictWithJumpOrCushion)
                return false;

            SupportMotionState support = runtimeState.Support;
            if (!support.HadSupport)
                return false;

            float previousY = support.PreviousPassengerVelocity.y;
            float currentY = support.PassengerVelocity.y;
            float lostY = previousY - currentY;

            if (previousY < settings.UpwardLaunchMinPreviousVelocity)
                return false;

            if (lostY < settings.UpwardLaunchMinLostVelocity)
                return false;

            float resolved = Mathf.Clamp(lostY * settings.UpwardLaunchRetainRate, 0.0f, settings.MaxLaunchVelocity);
            if (resolved <= 0.0001f)
                return false;

            Vector3 retainedPlanar = Vector3.ProjectOnPlane(support.PreviousPassengerVelocity, Vector3.up) * Mathf.Clamp01(settings.HorizontalRetainRate);
            if (retainedPlanar.sqrMagnitude > 0.0001f)
            {
                // Launch時に足場由来の水平慣性を保持し、急停止での不自然な失速を抑える。
                runtimeState.InheritedSupportVelocity += retainedPlanar;
            }

            runtimeState.VerticalVelocity = Mathf.Max(runtimeState.VerticalVelocity, resolved);
            runtimeState.LastSupportLaunchTime = currentTime;
            runtimeState.GroundSnapDisabledUntilTime = currentTime + settings.DisableGroundSnapTime;
            runtimeState.SupportReattachDisabledUntilTime = currentTime + settings.DisableSupportReattachTime;

            launchVelocityY = resolved;
            return true;
        }
    }
}
