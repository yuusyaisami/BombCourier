using System;

namespace BC.Base
{
    [Serializable]
    public sealed class GroundSnapSettings
    {
        public bool Enabled = true;
        public float MaxSnapDistance = 0.12f;
        public float SnapSpeed = 8.0f;
        public float MaxSnapDistancePerTick = 0.12f;
        public float DisableAfterJumpTime = 0.12f;
        public float DisableAfterSupportLaunchTime = 0.16f;
    }
}
