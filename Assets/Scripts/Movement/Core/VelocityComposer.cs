using UnityEngine;

namespace BC.Base
{
    public static class VelocityComposer
    {
        public static Vector3 ComposeBodyVelocity(in VelocityChannels channels)
        {
            return channels.InputPlanar
                 + Vector3.up * channels.Vertical
                 + channels.External
                 + channels.InheritedSupport
                 + channels.ConstraintCorrectionVelocity;
        }

        public static Vector3 ComposeFinalVelocity(in VelocityChannels channels)
        {
            return ComposeBodyVelocity(channels) + channels.SupportCarry;
        }

        public static Vector3 ControlledPlanarVelocity(in VelocityChannels channels)
        {
            Vector3 planar = channels.InputPlanar;
            planar.y = 0.0f;
            return planar;
        }

        public static float CurrentPlanarSpeed(in VelocityChannels channels)
        {
            return ControlledPlanarVelocity(channels).magnitude;
        }
    }
}
