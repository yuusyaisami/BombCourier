using System;

namespace BC.Base
{
    [Serializable]
    public sealed class SupportInertiaSettings
    {
        public bool Enabled = true;
        public float UpwardLaunchMinPreviousVelocity = 4.0f;
        public float UpwardLaunchMinLostVelocity = 3.5f;
        public float UpwardLaunchRetainRate = 0.85f;
        public float HorizontalRetainRate = 0.35f;
        public float MaxLaunchVelocity = 22.0f;
        public float DisableGroundSnapTime = 0.16f;
        public float DisableSupportReattachTime = 0.08f;
    }
}
